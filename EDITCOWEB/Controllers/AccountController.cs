using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.AspNetCore.Identity;
using EDITCOWEB.Models;

namespace EDITCOWEB.Controllers
{
    public class AccountController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly PasswordHasher<User> _passwordHasher;

        public AccountController(IConfiguration configuration)
        {
            _configuration = configuration;
            _passwordHasher = new PasswordHasher<User>();
        }

        // ================= REGISTER =================

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Register(User user)
        {
            if (!ModelState.IsValid)
                return View(user);

            string hashedPassword =
                _passwordHasher.HashPassword(user, user.Password);

            using SqlConnection con = new SqlConnection(
                _configuration.GetConnectionString("DefaultConnection"));

            string query = @"
                INSERT INTO Users (FirstName, LastName, Email, PasswordHash)
                VALUES (@FirstName, @LastName, @Email, @PasswordHash)";

            SqlCommand cmd = new SqlCommand(query, con);
            cmd.Parameters.AddWithValue("@FirstName", user.FirstName);
            cmd.Parameters.AddWithValue("@LastName", user.LastName);
            cmd.Parameters.AddWithValue("@Email", user.Email);
            cmd.Parameters.AddWithValue("@PasswordHash", hashedPassword);

            con.Open();
            cmd.ExecuteNonQuery();

            return RedirectToAction("Login");
        }

        // ================= LOGIN =================

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Login(string email, string password)
        {
            using SqlConnection con = new SqlConnection(
                _configuration.GetConnectionString("DefaultConnection"));

            string query = "SELECT * FROM Users WHERE Email = @Email";

            SqlCommand cmd = new SqlCommand(query, con);
            cmd.Parameters.AddWithValue("@Email", email);

            con.Open();
            SqlDataReader reader = cmd.ExecuteReader();

            if (reader.Read())
            {
                string storedHash = reader["PasswordHash"].ToString();

                var result = _passwordHasher.VerifyHashedPassword(
                    null,
                    storedHash,
                    password
                );

                if (result == PasswordVerificationResult.Success)
                {
                    HttpContext.Session.SetString(
                        "UserEmail",
                        reader["Email"].ToString()
                    );

                    return RedirectToAction("Index", "Home");
                }
            }

            ViewBag.Error = "E-posta veya şifre hatalı";
            return View();
        }

        // ================= PROFILE =================

        // ================= PROFILE =================

        [HttpGet]
        public IActionResult Profile()
        {
            // 1. Session'dan giriş yapan kullanıcının mailini al
            var email = HttpContext.Session.GetString("UserEmail");

            if (email == null)
            {
                return RedirectToAction("Login");
            }

            User aktifKullanici = null;

            // 2. Senin yöntemle (SqlConnection) veritabanına bağlan
            using (SqlConnection con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                string query = "SELECT FirstName, LastName, Email FROM Users WHERE Email = @Email";
                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@Email", email);

                con.Open();
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    // Eğer veritabanında bu maile sahip kullanıcı bulunursa bilgilerini çek
                    if (reader.Read())
                    {
                        aktifKullanici = new User
                        {
                            FirstName = reader["FirstName"].ToString(),
                            LastName = reader["LastName"].ToString(),
                            Email = reader["Email"].ToString()
                        };
                    }
                }
            }

            // 3. Kullanıcı herhangi bir sebepten bulunamazsa (silinmiş vs.) girişe yönlendir
            if (aktifKullanici == null)
            {
                return RedirectToAction("Login");
            }

            // 4. Bulunan kullanıcı modelini bizim tasarladığımız HTML sayfasına gönder
            return View(aktifKullanici);
        }

        // ================= LOGOUT =================

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Home");
        }
    }
}