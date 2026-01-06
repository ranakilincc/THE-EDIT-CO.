using Microsoft.AspNetCore.Mvc;

namespace EDITCOWEB.Controllers
{
    public class FavoritesController : Controller
    {
        public IActionResult Index()
        {
            // Eğer kullanıcı giriş yapmamışsa (Session boşsa), Giriş Sayfasına at
            if (HttpContext.Session.GetString("UserEmail") == null)
            {
                return Redirect("/Account/Login");
            }

            // Giriş yapmışsa Favoriler sayfasını göster
            return View();
        }
    }
}