using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient; // SQL bağlantısı için gerekli
using EDITCOWEB.Models;
using System.IO;

namespace EDITCOWEB.Controllers
{
    public class AdminController : Controller
    {
        // Veritabanı bağlantı yolumuzu almak için bunu ekliyoruz
        private readonly IConfiguration _configuration;

        public AdminController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public IActionResult Index()
        {
            // 1. GÜVENLİK DUVARI
            var adminEmail = HttpContext.Session.GetString("AdminEmail");

            if (string.IsNullOrEmpty(adminEmail))
            {
                return RedirectToAction("AdminLogin", "Account");
            }

            ViewBag.AdminEmail = adminEmail;

            // 2. GERÇEK VERİLERİ ÇEKME
            int toplamMusteri = 0;

            using (SqlConnection con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                // Users tablosundaki tüm kayıtları sayıyoruz
                string query = "SELECT COUNT(*) FROM Users";
                SqlCommand cmd = new SqlCommand(query, con);

                con.Open();
                // ExecuteScalar, veritabanından dönen tek bir değeri (sayıyı) almak için kullanılır
                toplamMusteri = (int)cmd.ExecuteScalar();
            }

            // Sayıyı tasarıma göndermek için çantaya (ViewBag) koyuyoruz
            ViewBag.ToplamMusteri = toplamMusteri;

            return View();
        }


        // ================= KAMPANYA YÖNETİMİ =================
        public IActionResult Campaigns()
        {
            // 1. GÜVENLİK DUVARI: Sadece adminler girebilir
            var adminEmail = HttpContext.Session.GetString("AdminEmail");
            if (string.IsNullOrEmpty(adminEmail)) return RedirectToAction("AdminLogin", "Account");

            // 2. KAMPANYALARI VERİTABANINDAN ÇEKME (Senin ADO.NET yönteminle)
            List<Campaign> kampanyaListesi = new List<Campaign>();

            using (SqlConnection con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                string query = "SELECT * FROM Campaigns ORDER BY Id DESC"; // En son eklenen en üstte görünsün
                SqlCommand cmd = new SqlCommand(query, con);

                con.Open();
                SqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    kampanyaListesi.Add(new Campaign
                    {
                        Id = Convert.ToInt32(reader["Id"]),
                        Baslik = reader["Baslik"].ToString(),
                        Aciklama = reader["Aciklama"] != DBNull.Value ? reader["Aciklama"].ToString() : "",
                        ResimYolu = reader["ResimYolu"].ToString(),
                        AktifMi = Convert.ToBoolean(reader["AktifMi"])
                    });
                }
            }

            // 3. Bulunan kampanyaları tasarıma (View) gönder
            return View(kampanyaListesi);
        }

        // ================= YENİ KAMPANYA EKLEME =================

        // 1. Ekleme Sayfasını (Tasarımı) Ekrana Getiren Metot
        [HttpGet]
        public IActionResult AddCampaign()
        {
            var adminEmail = HttpContext.Session.GetString("AdminEmail");
            if (string.IsNullOrEmpty(adminEmail)) return RedirectToAction("AdminLogin", "Account");

            return View();
        }

        // 2. Formdan Gelen Resmi ve Bilgileri Kaydeden Metot
        [HttpPost]
        public IActionResult AddCampaign(string baslik, string aciklama, IFormFile resimDosyasi)
        {
            var adminEmail = HttpContext.Session.GetString("AdminEmail");
            if (string.IsNullOrEmpty(adminEmail)) return RedirectToAction("AdminLogin", "Account");

            // Eğer gerçekten bir resim seçildiyse işlemleri yap
            if (resimDosyasi != null && resimDosyasi.Length > 0)
            {
                // A) Sitenin içindeki wwwroot klasörüne "images/campaigns" diye bir yol belirliyoruz
                string klasorYolu = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "campaigns");

                // B) Eğer böyle bir klasör yoksa, otomatik olarak oluştur!
                if (!Directory.Exists(klasorYolu))
                {
                    Directory.CreateDirectory(klasorYolu);
                }

                // C) Resim isimleri çakışmasın diye (Örn: iki tane resim1.jpg olursa) ismin sonuna benzersiz bir kod ekliyoruz
                string dosyaAdi = Guid.NewGuid().ToString() + Path.GetExtension(resimDosyasi.FileName);
                string tamYol = Path.Combine(klasorYolu, dosyaAdi);

                // D) Resmi seçtiğimiz bu klasöre fiziksel olarak kopyalıyoruz
                using (var stream = new FileStream(tamYol, FileMode.Create))
                {
                    resimDosyasi.CopyTo(stream);
                }

                // E) Veritabanına kaydetmek için resmin yolunu hazırlıyoruz (Örn: /images/campaigns/1234abcd.jpg)
                string dbResimYolu = "/images/campaigns/" + dosyaAdi;

                // F) Veritabanına (SQL) Kayıt İşlemi
                using (SqlConnection con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    string query = "INSERT INTO Campaigns (Baslik, Aciklama, ResimYolu, AktifMi) VALUES (@Baslik, @Aciklama, @ResimYolu, 1)";
                    SqlCommand cmd = new SqlCommand(query, con);

                    cmd.Parameters.AddWithValue("@Baslik", baslik);
                    cmd.Parameters.AddWithValue("@Aciklama", aciklama ?? "");
                    cmd.Parameters.AddWithValue("@ResimYolu", dbResimYolu);

                    con.Open();
                    cmd.ExecuteNonQuery();
                }
            }

            // İşlem başarıyla bitince bizi tekrar Kampanyalar listesine göndersin
            return RedirectToAction("Campaigns");
        }

        // ================= LOGOUT =================
        public IActionResult Logout()
        {
            HttpContext.Session.Remove("AdminEmail");
            return RedirectToAction("AdminLogin", "Account");
        }
    }
}