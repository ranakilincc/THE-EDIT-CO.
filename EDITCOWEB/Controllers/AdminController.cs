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


        // ================= KAMPANYA YÖNETİMİ (LİSTELEME) =================
        public IActionResult Campaigns()
        {
            // 1. Güvenlik Duvarı
            var adminEmail = HttpContext.Session.GetString("AdminEmail");
            if (string.IsNullOrEmpty(adminEmail)) return RedirectToAction("AdminLogin", "Account");

            // 2. Kampanyaları Çekme
            List<Campaign> kampanyaListesi = new List<Campaign>();

            using (SqlConnection con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                string query = "SELECT * FROM Campaigns ORDER BY Id DESC";
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
                        AktifMi = Convert.ToBoolean(reader["AktifMi"]),
                        // DİKKAT: Bitiş Tarihini Veritabanından Okuyoruz
                        BitisTarihi = reader["BitisTarihi"] != DBNull.Value ? Convert.ToDateTime(reader["BitisTarihi"]) : DateTime.MaxValue
                    });
                }
            }

            return View(kampanyaListesi);
        }

        // ================= YENİ KAMPANYA EKLEME (GET) =================
        [HttpGet]
        public IActionResult AddCampaign()
        {
            var adminEmail = HttpContext.Session.GetString("AdminEmail");
            if (string.IsNullOrEmpty(adminEmail)) return RedirectToAction("AdminLogin", "Account");

            return View();
        }

        // ================= YENİ KAMPANYA EKLEME (POST) =================
        [HttpPost]
        public IActionResult AddCampaign(string baslik, string aciklama, DateTime bitisTarihi, IFormFile resimDosyasi)
        {
            var adminEmail = HttpContext.Session.GetString("AdminEmail");
            if (string.IsNullOrEmpty(adminEmail)) return RedirectToAction("AdminLogin", "Account");

            if (resimDosyasi != null && resimDosyasi.Length > 0)
            {
                string klasorYolu = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "campaigns");

                if (!Directory.Exists(klasorYolu))
                {
                    Directory.CreateDirectory(klasorYolu);
                }

                string dosyaAdi = Guid.NewGuid().ToString() + Path.GetExtension(resimDosyasi.FileName);
                string tamYol = Path.Combine(klasorYolu, dosyaAdi);

                using (var stream = new FileStream(tamYol, FileMode.Create))
                {
                    resimDosyasi.CopyTo(stream);
                }

                string dbResimYolu = "/images/campaigns/" + dosyaAdi;

                using (SqlConnection con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    // DİKKAT: BitisTarihi sütununu SQL sorgusuna ekledik
                    string query = "INSERT INTO Campaigns (Baslik, Aciklama, ResimYolu, BitisTarihi, AktifMi) VALUES (@Baslik, @Aciklama, @ResimYolu, @BitisTarihi, 1)";
                    SqlCommand cmd = new SqlCommand(query, con);

                    cmd.Parameters.AddWithValue("@Baslik", baslik);
                    cmd.Parameters.AddWithValue("@Aciklama", aciklama ?? "");
                    cmd.Parameters.AddWithValue("@ResimYolu", dbResimYolu);
                    cmd.Parameters.AddWithValue("@BitisTarihi", bitisTarihi);

                    con.Open();
                    cmd.ExecuteNonQuery();
                }
            }

            return RedirectToAction("Campaigns");
        }

        // ================= KAMPANYA SİLME =================
        public IActionResult DeleteCampaign(int id)
        {
            var adminEmail = HttpContext.Session.GetString("AdminEmail");
            if (string.IsNullOrEmpty(adminEmail)) return RedirectToAction("AdminLogin", "Account");

            using (SqlConnection con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                string query = "DELETE FROM Campaigns WHERE Id = @Id";
                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@Id", id);

                con.Open();
                cmd.ExecuteNonQuery();
            }

            return RedirectToAction("Campaigns");
        }


        // ================= KUPON YÖNETİMİ (LİSTELEME) =================
        public IActionResult Coupons()
        {
            if (HttpContext.Session.GetString("AdminEmail") == null) return RedirectToAction("AdminLogin", "Account");

            List<Coupon> kuponListesi = new List<Coupon>();
            using (SqlConnection con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                string query = "SELECT * FROM Coupons ORDER BY Id DESC";
                SqlCommand cmd = new SqlCommand(query, con);
                con.Open();
                SqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    kuponListesi.Add(new Coupon
                    {
                        Id = (int)reader["Id"],
                        Kod = reader["Kod"].ToString(),
                        IndirimYuzdesi = (int)reader["IndirimYuzdesi"],
                        BitisTarihi = (DateTime)reader["BitisTarihi"],
                        AktifMi = (bool)reader["AktifMi"]
                    });
                }
            }
            return View(kuponListesi);
        }

        // ================= YENİ KUPON EKLEME (POST) =================
        [HttpPost]
        public IActionResult AddCoupon(string kod, int indirimYuzdesi, DateTime bitisTarihi, int altLimit)
        {
            // Giriş yapmamışsa login sayfasına gönder
            if (HttpContext.Session.GetString("AdminEmail") == null) return RedirectToAction("AdminLogin", "Account");

            using (SqlConnection con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                // SQL Sorgusu: Artık AltLimit'i de içeri ekliyoruz
                string query = "INSERT INTO Coupons (Kod, IndirimYuzdesi, BitisTarihi, AktifMi, AltLimit) VALUES (@Kod, @IndirimYuzdesi, @BitisTarihi, 1, @AltLimit)";

                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@Kod", kod.ToUpper());
                cmd.Parameters.AddWithValue("@IndirimYuzdesi", indirimYuzdesi);
                cmd.Parameters.AddWithValue("@BitisTarihi", bitisTarihi);
                cmd.Parameters.AddWithValue("@AltLimit", altLimit); 

                con.Open();
                cmd.ExecuteNonQuery();
            }

            // İşlem bitince listeye geri dön
            return RedirectToAction("Coupons");
        }

        // ================= YENİ KUPON EKLEME (SAYFAYI AÇAN KISIM) =================
        [HttpGet]
        public IActionResult AddCoupon()
        {
            if (HttpContext.Session.GetString("AdminEmail") == null) return RedirectToAction("AdminLogin", "Account");

            return View();
        }

        // ================= KUPON SİLME =================
        public IActionResult DeleteCoupon(int id)
        {
            if (HttpContext.Session.GetString("AdminEmail") == null) return RedirectToAction("AdminLogin", "Account");

            using (SqlConnection con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                string query = "DELETE FROM Coupons WHERE Id = @Id";
                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@Id", id);
                con.Open();
                cmd.ExecuteNonQuery();
            }
            return RedirectToAction("Coupons");
        }

        // ================= ÜRÜN YÖNETİMİ =================
        public IActionResult Products()
        {
            // 1. GÜVENLİK DUVARI
            var adminEmail = HttpContext.Session.GetString("AdminEmail");
            if (string.IsNullOrEmpty(adminEmail)) return RedirectToAction("AdminLogin", "Account");

            // 2. Ürünleri tutacağımız listeyi hazırlıyoruz
            List<Product> urunListesi = new List<Product>();

            // 3. Veritabanına bağlanıp ürünleri çekiyoruz
            using (SqlConnection con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                string query = "SELECT * FROM Products ORDER BY Id DESC"; // En son eklenen en üstte görünsün
                SqlCommand cmd = new SqlCommand(query, con);

                con.Open();
                SqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    urunListesi.Add(new Product
                    {
                        Id = Convert.ToInt32(reader["Id"]),
                        UrunAdi = reader["UrunAdi"].ToString(),
                        Kategori = reader["Kategori"].ToString(),
                        Fiyat = Convert.ToDecimal(reader["Fiyat"]),
                        Aciklama = reader["Aciklama"] != DBNull.Value ? reader["Aciklama"].ToString() : "",
                        ResimYolu = reader["ResimYolu"].ToString(),
                        StokMiktari = Convert.ToInt32(reader["StokMiktari"]),
                        EklenmeTarihi = Convert.ToDateTime(reader["EklenmeTarihi"])
                    });
                }
            }

            // 4. Ürünleri tasarıma (View) gönderiyoruz
            return View(urunListesi);
        }


        // ================= YENİ ÜRÜN EKLEME =================

        // 1. Ürün Ekleme Sayfasını (Tasarımı) Ekrana Getiren Metot
        [HttpGet]
        public IActionResult AddProduct()
        {
            var adminEmail = HttpContext.Session.GetString("AdminEmail");
            if (string.IsNullOrEmpty(adminEmail)) return RedirectToAction("AdminLogin", "Account");

            return View();
        }

        // 2. Formdan Gelen Ürün Bilgilerini ve Resmi Kaydeden Metot
        [HttpPost]
        public IActionResult AddProduct(string urunAdi, string kategori, decimal fiyat, int stokMiktari, string aciklama, IFormFile resimDosyasi)
        {
            var adminEmail = HttpContext.Session.GetString("AdminEmail");
            if (string.IsNullOrEmpty(adminEmail)) return RedirectToAction("AdminLogin", "Account");

            // Eğer gerçekten bir resim seçildiyse işlemleri yap
            if (resimDosyasi != null && resimDosyasi.Length > 0)
            {
                // A) Sitenin içindeki wwwroot/images klasörüne bu kez "products" (ürünler) diye bir klasör belirliyoruz
                string klasorYolu = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "products");

                if (!Directory.Exists(klasorYolu))
                {
                    Directory.CreateDirectory(klasorYolu);
                }

                // B) Resim ismini benzersiz yapıyoruz
                string dosyaAdi = Guid.NewGuid().ToString() + Path.GetExtension(resimDosyasi.FileName);
                string tamYol = Path.Combine(klasorYolu, dosyaAdi);

                // C) Resmi klasöre fiziksel olarak kopyalıyoruz
                using (var stream = new FileStream(tamYol, FileMode.Create))
                {
                    resimDosyasi.CopyTo(stream);
                }

                string dbResimYolu = "/images/products/" + dosyaAdi;

                // D) Veritabanına (SQL) Kayıt İşlemi
                using (SqlConnection con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    string query = "INSERT INTO Products (UrunAdi, Kategori, Fiyat, Aciklama, ResimYolu, StokMiktari) VALUES (@UrunAdi, @Kategori, @Fiyat, @Aciklama, @ResimYolu, @StokMiktari)";
                    SqlCommand cmd = new SqlCommand(query, con);

                    cmd.Parameters.AddWithValue("@UrunAdi", urunAdi);
                    cmd.Parameters.AddWithValue("@Kategori", kategori);
                    cmd.Parameters.AddWithValue("@Fiyat", fiyat);
                    cmd.Parameters.AddWithValue("@Aciklama", aciklama ?? "");
                    cmd.Parameters.AddWithValue("@ResimYolu", dbResimYolu);
                    cmd.Parameters.AddWithValue("@StokMiktari", stokMiktari);

                    con.Open();
                    cmd.ExecuteNonQuery();
                }
            }

            // İşlem başarıyla bitince bizi tekrar Ürünler listesine göndersin
            return RedirectToAction("Products");
        }

        // ================= ÜRÜN SİLME =================
        public IActionResult DeleteProduct(int id)
        {
            // 1. Güvenlik: Admin girişi yapılmış mı kontrol et
            var adminEmail = HttpContext.Session.GetString("AdminEmail");
            if (string.IsNullOrEmpty(adminEmail)) return RedirectToAction("AdminLogin", "Account");

            // 2. SQL'e bağlanıp o ID'ye sahip ürünü siliyoruz
            using (SqlConnection con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                // Gelen ID'ye eşit olan ürünü sil komutu
                string query = "DELETE FROM Products WHERE Id = @Id";
                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@Id", id);

                con.Open();
                cmd.ExecuteNonQuery(); // Silme işlemini çalıştır
            }

            // 3. İşlem bitince sayfayı yenilemiş gibi tekrar ürünler listesine dön
            return RedirectToAction("Products");
        }


        // ================= LOGOUT =================
        public IActionResult Logout()
        {
            HttpContext.Session.Remove("AdminEmail");
            return RedirectToAction("AdminLogin", "Account");
        }

        // ================= YORUM YÖNETİMİ =================

        [HttpGet]
        public IActionResult Yorumlar()
        {
            // Güvenlik: Sadece giriş yapmış adminler görebilir
            if (HttpContext.Session.GetString("AdminEmail") == null)
            {
                return RedirectToAction("AdminLogin", "Account");
            }

            // Yorumları ve hangi ürüne yapıldığını tutacak dinamik bir liste oluşturuyoruz
            var yorumListesi = new List<dynamic>();

            using (SqlConnection con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                con.Open();
                // INNER JOIN kullanarak Reviews tablosu ile Products tablosunu birleştiriyoruz
                // Böylece admin yorumun HANGİ ÜRÜNE yapıldığını ismen görebilir
                string query = @"SELECT r.Id, p.UrunAdi, r.KullaniciAdi, r.Puan, r.YorumMetni, r.Tarih 
                                 FROM Reviews r 
                                 INNER JOIN Products p ON r.ProductId = p.Id 
                                 ORDER BY r.Tarih DESC";

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            // Verileri isimsiz (anonymous) bir obje olarak listeye ekliyoruz
                            yorumListesi.Add(new
                            {
                                Id = Convert.ToInt32(reader["Id"]),
                                UrunAdi = reader["UrunAdi"].ToString(),
                                KullaniciAdi = reader["KullaniciAdi"].ToString(),
                                Puan = Convert.ToInt32(reader["Puan"]),
                                YorumMetni = reader["YorumMetni"].ToString(),
                                Tarih = Convert.ToDateTime(reader["Tarih"]).ToString("dd.MM.yyyy HH:mm")
                            });
                        }
                    }
                }
            }

            ViewBag.Yorumlar = yorumListesi;
            return View();
        }

        [HttpPost]
        public IActionResult YorumSil(int id)
        {
            // Güvenlik kontrolü
            if (HttpContext.Session.GetString("AdminEmail") == null)
            {
                return RedirectToAction("AdminLogin", "Account");
            }

            using (SqlConnection con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                con.Open();
                string query = "DELETE FROM Reviews WHERE Id = @id";
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    cmd.ExecuteNonQuery();
                }
            }

            // Silme işlemi bitince admini tekrar yorumlar sayfasına yönlendiriyoruz
            return RedirectToAction("Yorumlar");
        }

    }
}