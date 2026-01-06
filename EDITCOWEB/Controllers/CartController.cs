using Microsoft.AspNetCore.Mvc;

namespace EDITCOWEB.Controllers
{
    public class CartController : Controller
    {
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
    }
}
