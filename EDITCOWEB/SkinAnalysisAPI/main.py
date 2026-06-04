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

# -----------------------------
# SKIN TYPE COMMENT SYSTEM
# -----------------------------
def build_skin_comment(skin_type, redness, texture, brightness):

    if skin_type == "oily":
        return {
            "skinType": "Yağlı Cilt",
            "summary": "Cildinizde fazla sebum üretimi ve parlama tespit edildi.",
            "routine": "Sabah jel temizleyici, tonik, hafif nemlendirici ve SPF. Akşam temizleyici + niacinamide serum.",
            "recommendedProduct": "The Edit Co. Gentle Cleanser",
            "recommendedCategory": "Yüz Temizleme Jeli"
        }

    elif skin_type == "dry":
        return {
            "skinType": "Kuru Cilt",
            "summary": "Ciltte nem eksikliği ve düşük parlaklık gözlemlendi.",
            "routine": "Sabah nazik temizleyici + hyaluronik asit + yoğun nemlendirici + SPF. Akşam besleyici bakım.",
            "recommendedProduct": "The Edit Co. Moisture Cream",
            "recommendedCategory": "Nemlendiriciler"
        }

    elif skin_type == "combination":
        return {
            "skinType": "Karma Cilt",
            "summary": "T bölgesi yağlı, diğer bölgeler dengeli görünümde.",
            "routine": "Sabah dengeleyici bakım + hafif nemlendirici + SPF. Akşam gözenek dengeleyici serum.",
            "recommendedProduct": "The Edit Co. Balancing Toner",
            "recommendedCategory": "Tonikler"
        }

    else:
        hassas_note = ""
        if redness in ["noticeable", "mild"]:
            hassas_note = " Hafif hassasiyet ve kızarıklık eğilimi mevcut."

        return {
            "skinType": "Normal Cilt",
            "summary": f"Cilt dengeli görünüyor.{hassas_note}",
            "routine": "Sabah temizleyici + nemlendirici + SPF. Akşam temel bakım rutini.",
            "recommendedProduct": "The Edit Co. Daily UV Defense SPF 50",
            "recommendedCategory": "Güneş Kremleri"
        }


# -----------------------------
# TRANSLATE FUNCTIONS
# -----------------------------
def translate_brightness(x):
    return {
        "high": "Yüksek",
        "balanced": "Dengeli",
        "low": "Düşük"
    }.get(x, x)


def translate_redness(x):
    return {
        "noticeable": "Belirgin",
        "mild": "Hafif",
        "low": "Düşük"
    }.get(x, x)


def translate_texture(x):
    return {
        "rough": "Pürüzlü",
        "medium": "Orta",
        "smooth": "Pürüzsüz"
    }.get(x, x)


# -----------------------------
# MAIN ANALYSIS FUNCTION
# -----------------------------
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
        right_cheek_idx = [280, 330, 347, 425]
        forehead_idx = [70, 63, 105, 66, 107]

        left_cheek = np.array([pts[i] for i in left_cheek_idx])
        right_cheek = np.array([pts[i] for i in right_cheek_idx])

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

        # -----------------------------
        # CLASSIFICATION
        # -----------------------------
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

        # -----------------------------
        # FINAL RESPONSE
        # -----------------------------
        return {
            "brightnessScore": round(mean_L, 2),
            "rednessScore": round(mean_A, 2),
            "textureScore": round(float(texture), 2),

            "brightness": translate_brightness(brightness),
            "redness": translate_redness(redness),
            "texture": translate_texture(texture_level),

            "skinType": comment["skinType"],
            "summary": comment["summary"],
            "routine": comment["routine"],
            "recommendedProduct": comment["recommendedProduct"],
            "recommendedCategory": comment["recommendedCategory"]
        }


# -----------------------------
# API ENDPOINTS
# -----------------------------
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
            content={"error": str(e), "trace": traceback.format_exc()}
        )

    finally:
        if os.path.exists(temp_path):
            os.remove(temp_path)


# -----------------------------
# RUN
# -----------------------------
if __name__ == "__main__":
    import uvicorn
    uvicorn.run(app, host="127.0.0.1", port=8001)