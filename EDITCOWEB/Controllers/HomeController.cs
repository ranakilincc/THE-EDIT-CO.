using System.Diagnostics;
using EDITCOWEB.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient; // Veritabaný (SQL) baðlantýsý için bu kütüphaneyi ekledik

namespace EDITCOWEB.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IConfiguration _configuration; // Veritabaný yolunu (Connection String) okumak için ekledik

        // Metodun parantez içini _configuration'ý tanýyacak þekilde güncelledik
        public HomeController(ILogger<HomeController> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        public IActionResult Index()
        {
            // 1. Kampanyalarý tutacaðýmýz boþ bir liste oluþturuyoruz
            List<Campaign> aktifKampanyalar = new List<Campaign>();

            // 2. Veritabanýna baðlanýyoruz
            using (SqlConnection con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                // Sadece AktifMi = 1 olan (Yayýndaki) kampanyalarý çekiyoruz
                string query = "SELECT * FROM Campaigns WHERE AktifMi = 1 ORDER BY Id DESC";
                SqlCommand cmd = new SqlCommand(query, con);

                con.Open();
                SqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    aktifKampanyalar.Add(new Campaign
                    {
                        Id = Convert.ToInt32(reader["Id"]),
                        Baslik = reader["Baslik"].ToString(),
                        ResimYolu = reader["ResimYolu"].ToString()
                    });
                }
            }

            // 3. Bulduðumuz kampanyalarý anasayfa tasarýmýna (ViewBag ile) gönderiyoruz
            ViewBag.Kampanyalar = aktifKampanyalar;

            return View();
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
