from fastapi import FastAPI, UploadFile, File
from fastapi.middleware.cors import CORSMiddleware
from fastapi.responses import JSONResponse
import cv2
import mediapipe as mp
import numpy as np
import os
import uuid
import traceback

app = FastAPI()

app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)


def build_skin_comment(skin_type, redness, texture, brightness):
    if skin_type == "oily":
        return {
            "skinType": "Yağlı Cilt",
            "summary": "Cildinizde fazla sebum üretimine bağlı parlama ve doku yoğunluğu gözlemlendi.",
            "routine": "Sabah jel temizleyici, dengeleyici tonik, hafif nemlendirici ve güneş kremi. Akşam temizleyici, niacinamide içerikli serum ve su bazlı nemlendirici önerilir.",
            "recommendedProduct": "The Edit Co. Gentle Cleanser",
            "recommendedCategory": "Yüz Temizleme Jeli"
        }
    elif skin_type == "dry":
        return {
            "skinType": "Kuru Cilt",
            "summary": "Ciltte düşük parlaklık ve nem eksikliğine işaret eden bir görünüm tespit edildi.",
            "routine": "Sabah nazik temizleyici, hyaluronik asit serumu, yoğun nemlendirici ve güneş kremi. Akşam temizleyici, besleyici serum ve bariyer destekleyici krem önerilir.",
            "recommendedProduct": "The Edit Co. Moisture Cream",
            "recommendedCategory": "Nemlendiriciler"
        }
    elif skin_type == "combination":
        return {
            "skinType": "Karma Cilt",
            "summary": "Ciltte bazı bölgelerde denge, bazı bölgelerde ise yağlanma eğilimi gözlemlendi.",
            "routine": "Sabah nazik temizleyici, dengeleyici tonik, hafif serum ve güneş kremi. Akşam temizleyici, gözenek görünümünü dengeleyen serum ve hafif nemlendirici önerilir.",
            "recommendedProduct": "The Edit Co. Balancing Toner",
            "recommendedCategory": "Tonikler"
        }
    else:
        hassas_note = ""
        if redness in ["noticeable", "mild"]:
            hassas_note = " Ciltte hafif hassasiyet ve kızarıklık eğilimi de gözlemlendi."

        return {
            "skinType": "Normal Cilt",
            "summary": f"Cildiniz genel olarak dengeli görünüyor.{hassas_note}",
            "routine": "Sabah nazik temizleyici, hafif nem serumu ve güneş kremi. Akşam temizleyici ve nemlendirici ile rutininizi koruyabilirsiniz.",
            "recommendedProduct": "The Edit Co. Daily UV Defense SPF 50",
            "recommendedCategory": "Güneş Kremleri"
        }


def analyze_skin_image(image_path):
    image = cv2.imread(image_path)

    if image is None:
        return {"error": "Fotoğraf okunamadı."}

    rgb = cv2.cvtColor(image, cv2.COLOR_BGR2RGB)
    h, w, _ = image.shape

    with mp.solutions.face_mesh.FaceMesh(
        static_image_mode=True,
        max_num_faces=1,
        refine_landmarks=True
    ) as mesh:

        results = mesh.process(rgb)

        if not results.multi_face_landmarks:
            return {"error": "Yüz bulunamadı."}

        face_landmarks = results.multi_face_landmarks[0].landmark

        pts = []
        for lm in face_landmarks:
            x = int(lm.x * w)
            y = int(lm.y * h)
            pts.append((x, y))

        pts = np.array(pts)

        left_cheek_idx = [50, 101, 118, 205]
        left_cheek = np.array([pts[i] for i in left_cheek_idx])

        right_cheek_idx = [280, 330, 347, 425]
        right_cheek = np.array([pts[i] for i in right_cheek_idx])

        forehead_idx = [70, 63, 105, 66, 107]
        forehead = []
        for i in forehead_idx:
            x, y = pts[i]
            forehead.append((x, y - 40))
        forehead = np.array(forehead)

        mask = np.zeros((h, w), dtype=np.uint8)
        cv2.fillPoly(mask, [left_cheek], 255)
        cv2.fillPoly(mask, [right_cheek], 255)
        cv2.fillPoly(mask, [forehead], 255)

        lab = cv2.cvtColor(image, cv2.COLOR_BGR2LAB)
        L, A, B = cv2.split(lab)

        mean_L = cv2.mean(L, mask=mask)[0]
        mean_A = cv2.mean(A, mask=mask)[0]

        gray = cv2.cvtColor(image, cv2.COLOR_BGR2GRAY)
        masked_gray = cv2.bitwise_and(gray, gray, mask=mask)
        laplacian = cv2.Laplacian(masked_gray, cv2.CV_64F)
        valid_pixels = laplacian[mask > 0]
        texture = np.var(valid_pixels) if len(valid_pixels) > 0 else 0

        if mean_L > 170:
            brightness = "high"
        elif mean_L > 145:
            brightness = "balanced"
        else:
            brightness = "low"

        if mean_A > 142:
            redness = "noticeable"
        elif mean_A > 135:
            redness = "mild"
        else:
            redness = "low"

        if texture > 450:
            texture_level = "rough"
        elif texture > 250:
            texture_level = "medium"
        else:
            texture_level = "smooth"

        if brightness == "high" and texture_level == "rough":
            skin_type = "oily"
        elif brightness == "low":
            skin_type = "dry"
        elif brightness == "balanced" and texture_level == "medium":
            skin_type = "combination"
        else:
            skin_type = "normal"

        comment = build_skin_comment(skin_type, redness, texture_level, brightness)

        return {
            "brightnessScore": round(mean_L, 2),
            "rednessScore": round(mean_A, 2),
            "textureScore": round(float(texture), 2),
            "brightness": brightness,
            "redness": redness,
            "texture": texture_level,
            "skinType": comment["skinType"],
            "summary": comment["summary"],
            "routine": comment["routine"],
            "recommendedProduct": comment["recommendedProduct"],
            "recommendedCategory": comment["recommendedCategory"]
        }


@app.get("/")
def root():
    return {"message": "Skin AI API is running"}


@app.post("/analyze")
async def analyze(file: UploadFile = File(...)):
    ext = os.path.splitext(file.filename)[1]
    temp_filename = f"temp_{uuid.uuid4().hex}{ext}"
    temp_path = os.path.join(".", temp_filename)

    try:
        with open(temp_path, "wb") as buffer:
            buffer.write(await file.read())

        result = analyze_skin_image(temp_path)

        if "error" in result:
            return JSONResponse(status_code=400, content=result)

        return result

    except Exception as e:
        return JSONResponse(
            status_code=500,
            content={
                "error": str(e),
                "trace": traceback.format_exc()
            }
        )

    finally:
        if os.path.exists(temp_path):
            os.remove(temp_path)