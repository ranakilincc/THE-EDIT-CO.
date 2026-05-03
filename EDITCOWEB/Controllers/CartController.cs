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
            var userEmail = HttpContext.Session.GetString("UserEmail");
            if (string.IsNullOrEmpty(userEmail))
            {
                return Redirect("/Account/Login");
            }

            List<dynamic> cartItems = new List<dynamic>();

            using (SqlConnection con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                con.Open();

                // UrunAdi ve Fiyat kolonlarını senin tablona göre eşledim
                string query = @"
    SELECT c.Id as CartId, p.Id as ProductId, p.UrunAdi as ProductName, p.Fiyat as Price, 
           p.ResimYolu, c.Quantity, (p.Fiyat * c.Quantity) as TotalPrice 
    FROM dbo.Cart c
    INNER JOIN dbo.Products p ON c.ProductId = p.Id
    INNER JOIN dbo.Users u ON c.UserId = u.Id
    WHERE u.Email = @Email";

                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@Email", userEmail);

                SqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    cartItems.Add(new
                    {
                        CartId = reader["CartId"],
                        ProductId = reader["ProductId"],
                        ProductName = reader["ProductName"].ToString(),
                        Price = reader["Price"],
                        ResimYolu = reader["ResimYolu"]?.ToString(), 
                        Quantity = reader["Quantity"],
                        TotalPrice = reader["TotalPrice"]
                    });
                }
            }

            return View(cartItems);
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


        [HttpPost]
        public IActionResult Add(int productId, int quantity)
        {
            var userEmail = HttpContext.Session.GetString("UserEmail");

            if (string.IsNullOrEmpty(userEmail))
            {
                return Json(new { success = false, message = "Oturum kapalı görünüyor Rana, lütfen tekrar giriş yap! ✨" });
            }

            try
            {
                using (SqlConnection con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    con.Open();

                    // 1. Kullanıcıyı buluyoruz
                    string userQuery = "SELECT Id FROM Users WHERE Email = @Email";
                    SqlCommand userCmd = new SqlCommand(userQuery, con);
                    userCmd.Parameters.AddWithValue("@Email", userEmail);

                    var userIdResult = userCmd.ExecuteScalar();

                    if (userIdResult == null)
                    {
                        return Json(new { success = false, message = "Email veritabanında bulunamadı: " + userEmail });
                    }

                    int userId = Convert.ToInt32(userIdResult);

                    // 2. Ürün sepette var mı kontrol et ve EKLE/GÜNCELLE
                    // Not: Tablo adını tam olarak dbo.Cart olarak belirttim
                    string query = @"
                IF EXISTS (SELECT 1 FROM dbo.Cart WHERE UserId = @UserId AND ProductId = @ProductId)
                BEGIN
                    UPDATE dbo.Cart SET Quantity = Quantity + @Qty 
                    WHERE UserId = @UserId AND ProductId = @ProductId
                END
                ELSE
                BEGIN
                    INSERT INTO dbo.Cart (UserId, ProductId, Quantity) 
                    VALUES (@UserId, @ProductId, @Qty)
                END";

                    SqlCommand cmd = new SqlCommand(query, con);
                    cmd.Parameters.AddWithValue("@UserId", userId);
                    cmd.Parameters.AddWithValue("@ProductId", productId);
                    cmd.Parameters.AddWithValue("@Qty", quantity);

                    cmd.ExecuteNonQuery();
                }

                return Json(new { success = true, message = "Ürün sepetine başarıyla eklendi! 🛒✨" });
            }
            catch (Exception ex)
            {
                // Bir hata olursa, hatanın tam sebebini mesaj olarak dönüyoruz
                return Json(new { success = false, message = "Bir hata oluştu: " + ex.Message });
            }
        }


        [HttpPost]
        public IActionResult Delete(int id)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    con.Open();
                    string query = "DELETE FROM dbo.Cart WHERE Id = @Id";
                    SqlCommand cmd = new SqlCommand(query, con);
                    cmd.Parameters.AddWithValue("@Id", id);
                    cmd.ExecuteNonQuery();
                }
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public IActionResult Clear()
        {
            var userEmail = HttpContext.Session.GetString("UserEmail");
            try
            {
                using (SqlConnection con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    con.Open();
                    string query = "DELETE FROM dbo.Cart WHERE UserId = (SELECT Id FROM Users WHERE Email = @Email)";
                    SqlCommand cmd = new SqlCommand(query, con);
                    cmd.Parameters.AddWithValue("@Email", userEmail);
                    cmd.ExecuteNonQuery();
                }
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }


        [HttpGet]
        public IActionResult GetCartCount()
        {
            var userEmail = HttpContext.Session.GetString("UserEmail");
            if (string.IsNullOrEmpty(userEmail)) return Json(0);

            try
            {
                using (SqlConnection con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    con.Open();
                    // COUNT(*) her zaman bir sayı döner
                    string query = "SELECT COUNT(*) FROM dbo.Cart WHERE UserId = (SELECT Id FROM Users WHERE Email = @Email)";
                    SqlCommand cmd = new SqlCommand(query, con);
                    cmd.Parameters.AddWithValue("@Email", userEmail);

                    var result = cmd.ExecuteScalar();
                    return Json(result != DBNull.Value ? Convert.ToInt32(result) : 0);
                }
            }
            catch { return Json(0); }
        }


    }
}
