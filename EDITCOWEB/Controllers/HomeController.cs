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
            List<Product> butunUrunler = new List<Product>(); // Bütün ürünleri tutacaðýmýz ana liste

            using (SqlConnection con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))

            {
                con.Open();
                // 1. KAMPANYALARI ÇEKME
                string queryKampanya = "SELECT * FROM Campaigns WHERE AktifMi = 1 ORDER BY Id DESC";
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
                                ResimYolu = readerKampanya["ResimYolu"].ToString()
                            });
                        }
                    }
                }

                // 2. BÜTÜN ÜRÜNLERÝ ÇEKME (Artýk TOP 8 deðil, hepsini çekiyoruz)
                string queryUrun = "SELECT * FROM Products ORDER BY Id DESC";
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
                                ResimYolu = readerUrun["ResimYolu"].ToString()
                            });
                        }
                    }
                }
            }

            // 3. VÝTRÝNE GÖNDERMEDEN ÖNCE KATEGORÝLERE GÖRE AYIRMA ÝÞLEMÝ (Ýþte sihir burada!)
            ViewBag.Kampanyalar = aktifKampanyalar;

            // Ürünleri "Kategori" ismine göre filtreleyip ayrý çantalara koyuyoruz
            ViewBag.Serumlar = butunUrunler.Where(u => u.Kategori == "Serum").ToList();
            ViewBag.Tonikler = butunUrunler.Where(u => u.Kategori == "Tonik").ToList();
            ViewBag.GunesKremleri = butunUrunler.Where(u => u.Kategori == "Güneþ Kremi").ToList();
            ViewBag.Temizleyiciler = butunUrunler.Where(u => u.Kategori == "Temizleyici").ToList();
            ViewBag.Nemlendiriciler = butunUrunler.Where(u => u.Kategori == "Nemlendirici").ToList();

            // Eðer "Yeni Gelenler" bölümünü de tutmak istersen, en son eklenen 8 ürünü ayrý bir çantaya koyabiliriz
            ViewBag.YeniGelenler = butunUrunler.Take(8).ToList();

            return View();
        }

        // ================= ÜRÜN DETAY SAYFASI =================
        public IActionResult ProductDetail(int id)
        {
            Product secilenUrun = null;

            using (SqlConnection con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                // Týklanan ürünün ID'sine göre veritabanýndan o ürünü buluyoruz
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

            // Eðer ürün bulunamazsa veya silinmiþse, müþteriyi hata sayfasýna deðil anasayfaya geri gönderelim
            if (secilenUrun == null)
            {
                return RedirectToAction("Index");
            }

            // Bulduðumuz ürünü detay sayfasýna gönderiyoruz
            return View(secilenUrun);
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
    }
}