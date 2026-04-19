using Microsoft.AspNetCore.Mvc;
using TheEditCo.Models;

namespace TheEditCo.Controllers
{
    public class CheckoutController : Controller
    {
        [HttpGet]
        public IActionResult Index()
        {
            return View(new CheckoutViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Index(CheckoutViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Şimdilik sadece başarılı sayfasına yönlendiriyoruz.
            // Sonraki adımda buraya sipariş kaydetme kodu eklenecek.
            return RedirectToAction("Success");
        }

        public IActionResult Success()
        {
            return View();
        }
    }
}