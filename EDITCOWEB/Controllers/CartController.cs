using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Reflection.PortableExecutable;

namespace EDITCOWEB.Controllers
{
    public class CartController : Controller
    {

        // 1. BURAYI EKLE: Konfigürasyon anahtarını tanımlıyoruz
        private readonly IConfiguration _configuration;

        // 2. BURAYI EKLE: Controller çalıştığında bu anahtarı sisteme bağlıyoruz (Constructor)
        public CartController(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        public IActionResult Index()
        {
            // Giriş yapılmamışsa login'e at
            if (HttpContext.Session.GetString("UserEmail") == null)
            {
                return Redirect("/Account/Login");
            }

            // Giriş yapılmışsa sepet sayfasını göster
            return View();
        }

        [HttpPost]
        public JsonResult CheckCoupon(string code)
        {
            using (SqlConnection con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                // 1. Sorguya AltLimit'i de ekledik
                string query = "SELECT IndirimYuzdesi, AltLimit FROM Coupons WHERE Kod = @Kod AND AktifMi = 1 AND BitisTarihi > GETDATE()";
                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@Kod", code.ToUpper());

                con.Open();

                // 2. BURASI KRİTİK: ExecuteScalar yerine ExecuteReader kullanmalıyız!
                SqlDataReader reader = cmd.ExecuteReader();

                if (reader.Read()) // Şimdi 'reader' tanımlı olduğu için hata vermeyecek
                {
                    int yuzde = (int)reader["IndirimYuzdesi"];
                    int altLimit = (int)reader["AltLimit"];

                    return Json(new
                    {
                        success = true,
                        discountPercent = yuzde,
                        minLimit = altLimit,
                        message = "Kupon onaylandı!"
                    });
                }
                else
                {
                    return Json(new { success = false, message = "Geçersiz veya süresi dolmuş kupon kodu!" });
                }
            }
        }
    }
}
