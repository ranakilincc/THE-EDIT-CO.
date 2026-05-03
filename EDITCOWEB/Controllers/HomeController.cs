using System.Diagnostics;
using EDITCOWEB.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Linq;

namespace EDITCOWEB.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IConfiguration _configuration;


        public HomeController(ILogger<HomeController> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        public IActionResult Index()
        {
            List<Campaign> aktifKampanyalar = new List<Campaign>();
            List<Product> butunUrunler = new List<Product>();

            using (SqlConnection con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                con.Open();
                // 1. KAMPANYALARI ÇEKME
                string queryKampanya = "SELECT * FROM Campaigns WHERE AktifMi = 1 AND BitisTarihi > GETDATE() ORDER BY Id DESC";
                using (SqlCommand cmdKampanya = new SqlCommand(queryKampanya, con))
                {
                    using (SqlDataReader readerKampanya = cmdKampanya.ExecuteReader())
                    {
                        while (readerKampanya.Read())
                        {
                            aktifKampanyalar.Add(new Campaign
                            {
                                Id = Convert.ToInt32(readerKampanya["Id"]),
                                Baslik = readerKampanya["Baslik"].ToString(),
                                ResimYolu = readerKampanya["ResimYolu"].ToString(),
                                BitisTarihi = readerKampanya["BitisTarihi"] != DBNull.Value ? Convert.ToDateTime(readerKampanya["BitisTarihi"]) : DateTime.Now.AddDays(1)
                            });
                        }
                    }
                }

                // 2. BÜTÜN ÜRÜNLERİ ÇEKME (Ortalama Puan ve Yorum Sayısı hesaplamaları eklendi)
                string queryUrun = @"
                    SELECT *,
                        ISNULL((SELECT AVG(CAST(Puan AS FLOAT)) FROM Reviews WHERE ProductId = Products.Id), 0) AS OrtalamaPuan,
                        (SELECT COUNT(*) FROM Reviews WHERE ProductId = Products.Id) AS YorumSayisi
                    FROM Products ORDER BY Id DESC";

                using (SqlCommand cmdUrun = new SqlCommand(queryUrun, con))
                {
                    using (SqlDataReader readerUrun = cmdUrun.ExecuteReader())
                    {
                        while (readerUrun.Read())
                        {
                            butunUrunler.Add(new Product
                            {
                                Id = Convert.ToInt32(readerUrun["Id"]),
                                UrunAdi = readerUrun["UrunAdi"].ToString(),
                                Kategori = readerUrun["Kategori"].ToString(),
                                Fiyat = Convert.ToDecimal(readerUrun["Fiyat"]),
                                ResimYolu = readerUrun["ResimYolu"].ToString(),
                                // Yeni eklenen özellikler veritabanından alınıyor
                                OrtalamaPuan = Convert.ToDouble(readerUrun["OrtalamaPuan"]),
                                YorumSayisi = Convert.ToInt32(readerUrun["YorumSayisi"])
                            });
                        }
                    }
                }
            }

            ViewBag.Kampanyalar = aktifKampanyalar;
            ViewBag.Serumlar = butunUrunler.Where(u => u.Kategori == "Serum").ToList();
            ViewBag.Tonikler = butunUrunler.Where(u => u.Kategori == "Tonik").ToList();
            ViewBag.GunesKremleri = butunUrunler.Where(u => u.Kategori == "Güneş Kremi").ToList();
            ViewBag.Temizleyiciler = butunUrunler.Where(u => u.Kategori == "Temizleyici").ToList();
            ViewBag.Nemlendiriciler = butunUrunler.Where(u => u.Kategori == "Nemlendirici").ToList();
            ViewBag.YeniGelenler = butunUrunler.Take(8).ToList();

            return View();
        }


        // ================= ÜRÜNLER VE FİLTRELEME SAYFASI =================
        public IActionResult Urunler(List<string> ciltTipi, List<string> urunTuru, decimal? minFiyat, decimal? maxFiyat)
        {
            List<Product> filtrelenmisUrunler = new List<Product>();

            // 1. Önce veritabanındaki tüm ürünleri çekiyoruz
            using (SqlConnection con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                con.Open();
                string queryUrun = "SELECT * FROM Products ORDER BY Id DESC";
                using (SqlCommand cmdUrun = new SqlCommand(queryUrun, con))
                {
                    using (SqlDataReader readerUrun = cmdUrun.ExecuteReader())
                    {
                        while (readerUrun.Read())
                        {
                            filtrelenmisUrunler.Add(new Product
                            {
                                Id = Convert.ToInt32(readerUrun["Id"]),
                                UrunAdi = readerUrun["UrunAdi"].ToString(),
                                // Ürün türü veritabanında "Kategori" olarak geçiyor
                                Kategori = readerUrun["Kategori"].ToString(),
                                // Eski ürünlerde CiltTipi boş (DBNull) ise hata vermesin diye kontrol ediyoruz
                                CiltTipi = readerUrun["CiltTipi"] != DBNull.Value ? readerUrun["CiltTipi"].ToString() : "",
                                Fiyat = Convert.ToDecimal(readerUrun["Fiyat"]),
                                ResimYolu = readerUrun["ResimYolu"].ToString()
                            });
                        }
                    }
                }
            }

            // 2. Cilt Tipine Göre Filtrele
            if (ciltTipi != null && ciltTipi.Count > 0)
            {
                filtrelenmisUrunler = filtrelenmisUrunler.Where(u => ciltTipi.Contains(u.CiltTipi)).ToList();
            }

            // 3. Ürün Türüne (Kategoriye) Göre Filtrele
            if (urunTuru != null && urunTuru.Count > 0)
            {
                filtrelenmisUrunler = filtrelenmisUrunler.Where(u => urunTuru.Contains(u.Kategori)).ToList();
            }

            // 4. Minimum Fiyata Göre Filtrele
            if (minFiyat.HasValue)
            {
                filtrelenmisUrunler = filtrelenmisUrunler.Where(u => u.Fiyat >= minFiyat).ToList();
            }

            // 5. Maksimum Fiyata Göre Filtrele
            if (maxFiyat.HasValue)
            {
                filtrelenmisUrunler = filtrelenmisUrunler.Where(u => u.Fiyat <= maxFiyat).ToList();
            }

            // Filtrelenmiş listeyi Urunler.cshtml sayfasına gönderiyoruz
            return View(filtrelenmisUrunler);
        }



        // ================= ÜRÜN DETAY SAYFASI =================
        public IActionResult ProductDetail(int id)
        {
            Product secilenUrun = null;

            using (SqlConnection con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                // Tıklanan ürünün ID'sine göre veritabanından o ürünü buluyoruz
                string query = "SELECT * FROM Products WHERE Id = @Id";
                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@Id", id);

                con.Open();
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        secilenUrun = new Product
                        {
                            Id = Convert.ToInt32(reader["Id"]),
                            UrunAdi = reader["UrunAdi"].ToString(),
                            Kategori = reader["Kategori"].ToString(),
                            Fiyat = Convert.ToDecimal(reader["Fiyat"]),
                            Aciklama = reader["Aciklama"] != DBNull.Value ? reader["Aciklama"].ToString() : "",
                            ResimYolu = reader["ResimYolu"].ToString(),
                            StokMiktari = Convert.ToInt32(reader["StokMiktari"])
                        };
                    }
                }
            }

            // Eğer ürün bulunamazsa veya silinmişse, müşteriyi hata sayfasına değil anasayfaya geri gönderelim
            if (secilenUrun == null)
            {
                return RedirectToAction("Index");
            }


            // --- YORUMLARI ÇEKME KISMI BAŞLANGICI ---
            List<Review> yorumlar = new List<Review>();
            using (SqlConnection con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                con.Open();
                // Sadece bu ürüne ait yorumları tarihe göre en yenisi en üstte olacak şekilde çekiyoruz
                string queryReview = "SELECT * FROM Reviews WHERE ProductId = @id ORDER BY Tarih DESC";
                using (SqlCommand cmdReview = new SqlCommand(queryReview, con))
                {
                    cmdReview.Parameters.AddWithValue("@id", id);
                    using (SqlDataReader readerReview = cmdReview.ExecuteReader())
                    {
                        while (readerReview.Read())
                        {
                            yorumlar.Add(new Review
                            {
                                Id = Convert.ToInt32(readerReview["Id"]),
                                ProductId = Convert.ToInt32(readerReview["ProductId"]),
                                KullaniciAdi = readerReview["KullaniciAdi"].ToString(),
                                Puan = Convert.ToInt32(readerReview["Puan"]),
                                YorumMetni = readerReview["YorumMetni"].ToString(),
                                Tarih = Convert.ToDateTime(readerReview["Tarih"])
                            });
                        }
                    }
                }
            }
            // Çektiğimiz yorumları View'da kullanabilmek için ViewBag içine atıyoruz
            ViewBag.Yorumlar = yorumlar;
            // --- YORUMLARI ÇEKME KISMI BİTİŞİ ---


            // Bulduğumuz ürünü detay sayfasına gönderiyoruz
            return View(secilenUrun);
        }

        // ================= YENİ YORUM EKLEME METODU (SESSION İLE) =================
        [HttpPost]
        public IActionResult YorumEkle(int ProductId, int Puan, string YorumMetni)
        {
            // 1. Session'da "UserEmail" var mı diye bakıyoruz. Yoksa giriş sayfasına şutluyoruz.
            string userEmail = HttpContext.Session.GetString("UserEmail");
            if (string.IsNullOrEmpty(userEmail))
            {
                return RedirectToAction("Login", "Account");
            }

            string adSoyad = "Anonim"; // Varsayılan isim

            using (SqlConnection con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                con.Open();

                // 2. O maile sahip kullanıcının ADINI ve SOYADINI veritabanından çekiyoruz (Tam istediğin gibi!)
                string adQuery = "SELECT FirstName, LastName FROM Users WHERE Email = @Email";
                using (SqlCommand adCmd = new SqlCommand(adQuery, con))
                {
                    adCmd.Parameters.AddWithValue("@Email", userEmail);
                    using (SqlDataReader reader = adCmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            adSoyad = reader["FirstName"].ToString() + " " + reader["LastName"].ToString();
                        }
                    }
                }

                // 3. Bulduğumuz bu Ad-Soyad ile yorumu kaydediyoruz
                string query = "INSERT INTO Reviews (ProductId, KullaniciAdi, Puan, YorumMetni) VALUES (@pId, @kAdi, @puan, @yorum)";
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@pId", ProductId);
                    cmd.Parameters.AddWithValue("@kAdi", adSoyad);
                    cmd.Parameters.AddWithValue("@puan", Puan);
                    cmd.Parameters.AddWithValue("@yorum", string.IsNullOrWhiteSpace(YorumMetni) ? (object)DBNull.Value : YorumMetni);

                    cmd.ExecuteNonQuery();
                }
            }

            TempData["YorumBasarili"] = "Yorumunuz başarıyla kaydedildi. Teşekkür ederiz! 💖";

            return RedirectToAction("ProductDetail", new { id = ProductId });
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }



        [HttpGet]
        public IActionResult BotIcinUrunleriGetir()
        {
            // 1. Giriş yapan kullanıcının e-mail bilgisini alıyoruz
            string userEmail = HttpContext.Session.GetString("UserEmail");
            string userSkinType = "Bilinmiyor";

            // 2. Eğer kullanıcı giriş yapmışsa, SQL'den onun kalıcı cilt tipini çekiyoruz
            if (!string.IsNullOrEmpty(userEmail))
            {
                using (SqlConnection con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    con.Open();
                    string query = "SELECT CiltTipi FROM Users WHERE Email = @email";
                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@email", userEmail);
                        var result = cmd.ExecuteScalar();
                        if (result != null && result != DBNull.Value)
                        {
                            userSkinType = result.ToString();
                            // O anki oturumda da hızlı erişim için Session'a yazalım
                            HttpContext.Session.SetString("UserSkinType", userSkinType);
                        }
                    }
                }
            }

            // 3. Botun ürünleri önerebilmesi için veritabanındaki tüm ürün listesini hazırlıyoruz
            string botUrunListesi = "";
            using (SqlConnection con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                con.Open();
                string query = "SELECT Id, UrunAdi, CiltTipi, Aciklama, Fiyat FROM Products ORDER BY Id DESC";

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string id = reader["Id"].ToString();
                            string urunAdi = reader["UrunAdi"].ToString();
                            string ciltTipiUrun = reader["CiltTipi"] != DBNull.Value ? reader["CiltTipi"].ToString() : "Tüm Ciltler";
                            string aciklama = reader["Aciklama"] != DBNull.Value ? reader["Aciklama"].ToString() : "";
                            string fiyat = reader["Fiyat"].ToString();
                            string urunLink = $"/Home/ProductDetail/{id}";

                            botUrunListesi += $"- Ürün: {urunAdi} | Link: {urunLink} | Kimler İçin: {ciltTipiUrun} | Ne İşe Yarar: {aciklama} | Fiyat: {fiyat} TL\n";
                        }
                    }
                }
            }

            // 4. JavaScript'e (Chatbot Arayüzüne) hem ürünleri hem de müşterinin cilt tipini paketleyip gönderiyoruz
            return Json(new
            {
                urunler = botUrunListesi,
                ciltTipi = userSkinType
            });
        }


        [HttpPost]
        public IActionResult CiltTipiniKaydet([FromForm] string ciltTipi)
        {
            // 1. Giriş yapan kullanıcının emailini sessiondan alıyoruz (Senin sisteminde muhtemelen "UserEmail" veya "Email"dir)
            string userEmail = HttpContext.Session.GetString("UserEmail");

            if (!string.IsNullOrEmpty(userEmail))
            {
                // 2. SQL'e kalıcı olarak yazıyoruz
                using (SqlConnection con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    con.Open();
                    string query = "UPDATE Users SET CiltTipi = @cilt WHERE Email = @email";
                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@cilt", ciltTipi);
                        cmd.Parameters.AddWithValue("@email", userEmail);
                        cmd.ExecuteNonQuery();
                    }
                }
            }

            // 3. Yine de o anki sayfa yenilenmeden chatbotun bilmesi için Session'a da yazalım
            HttpContext.Session.SetString("UserSkinType", ciltTipi);

            return Json(new { basari = true });
        }
        public IActionResult CiltTesti()
        {
            // Giriş yapmamışsa Login sayfasına postala
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("UserEmail")))
            {
                return RedirectToAction("Login", "Account");
            }
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Chat(string message)
        {
            // 1. KESİN GİRİŞ KONTROLÜ
            var loginKontrol = HttpContext.Session.GetString("UserEmail");

            if (string.IsNullOrEmpty(loginKontrol))
            {
                return Json(new
                {
                    error = "auth_required",
                    message = "Ürün önerileri alabilmek ve cilt tipini kaydetmek için giriş yapman veya kayıt olman gerekiyor. ✨",
                    link = "/Account/Login"
                });
            }

            // 2. HAFIZA TAZELEME VE İSİM ÇEKME
            string userSkinType = "Bilinmiyor";
            string userName = "Kullanıcı"; // Varsayılan değer

            using (SqlConnection con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                con.Open();
                // Hem CiltTipi hem de FirstName (Ad) bilgisini çekiyoruz
                string queryUser = "SELECT CiltTipi, FirstName FROM Users WHERE Email = @email";
                using (SqlCommand cmd = new SqlCommand(queryUser, con))
                {
                    cmd.Parameters.AddWithValue("@email", loginKontrol);
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            userSkinType = reader["CiltTipi"] != DBNull.Value ? reader["CiltTipi"].ToString() : "Bilinmiyor";
                            userName = reader["FirstName"] != DBNull.Value ? reader["FirstName"].ToString() : "Kullanıcı";
                        }
                    }
                }
            }

            // 3. ÜRÜN LİSTESİNİ ÇEKME
            string botUrunListesi = "";
            using (SqlConnection con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                con.Open();
                string queryUrun = "SELECT UrunAdi, CiltTipi, Aciklama, Fiyat, Id FROM Products";
                using (SqlCommand cmdUrun = new SqlCommand(queryUrun, con))
                {
                    using (SqlDataReader reader = cmdUrun.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            botUrunListesi += $"- Ürün: {reader["UrunAdi"]} | Link: /Home/ProductDetail/{reader["Id"]} | Tip: {reader["CiltTipi"]} | Fiyat: {reader["Fiyat"]} TL\n";
                        }
                    }
                }
            }

            // 4. PYTHON SUNUCUSUNA GÖNDERME
            try
            {
                using (var client = new HttpClient())
                {
                    var content = new FormUrlEncodedContent(new[]
                    {
                 new KeyValuePair<string, string>("message", message),
                 new KeyValuePair<string, string>("urunler", botUrunListesi),
                 new KeyValuePair<string, string>("cilt_tipi", userSkinType),
                 // YENİ: Python'a kullanıcının gerçek ismini gönderiyoruz
                 new KeyValuePair<string, string>("user_name", userName)
             });

                    var response = await client.PostAsync("http://127.0.0.1:8001/chat", content);
                    var responseString = await response.Content.ReadAsStringAsync();
                    return Content(responseString, "application/json");
                }
            }
            catch (Exception ex)
            {
                return Json(new { answer = "Bağlantı hatası: Lütfen Python (8001) sunucusunu başlatın!" });
            }
        }

    }
}

