using Microsoft.AspNetCore.Mvc;
using EDITCOWEB.Models;
using System.Net;
using System.Net.Mail;
using Microsoft.AspNetCore.Http; // Session kullanabilmek için gerekli

namespace EDITCOWEB.Controllers
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

            // --- MAİL GÖNDERME İŞLEMİ ---
            string musteriEmail = HttpContext.Session.GetString("UserEmail");
            string musteriAd = model.FullName;

            if (!string.IsNullOrEmpty(musteriEmail))
            {
                SiparisOnayMailiGonder(musteriEmail, musteriAd);
            }

            // İşlem bitince başarılı sayfasına yönlendiriyoruz
            return RedirectToAction("Success");
        }

        // İŞTE EKSİK OLMA İHTİMALİ ÇOK YÜKSEK OLAN KISIM BURASI!
        // Bu kod olmadan sistem "Success.cshtml" dosyasını bulsa bile ekrana basmaz.
        public IActionResult Success()
        {
            return View();
        }

        // MAİL GÖNDERME METODU
        private void SiparisOnayMailiGonder(string musteriEmail, string musteriAd)
        {
            try
            {
                string gondericiMail = "ranakilinc23@gmail.com";
                string gondericiSifre = "epnn gjyf klfh ybul";

                string konu = "Siparişin Alındı! ✨ - THE EDIT CO.";

                string icerik = $@"
            <div style='font-family: Arial, sans-serif; color: #333; padding: 20px;'>
                <h2 style='color: #e0a6aa;'>Merhaba {musteriAd},</h2>
                <p>Siparişini başarıyla aldık ve hazırlamak için sabırsızlanıyoruz! 🌸</p>
                <p>Siparişin kargoya verildiğinde sana tekrar haber vereceğiz.</p>
                <br>
                <p>Bizi tercih ettiğin için teşekkür ederiz.</p>
                <p><strong>The Edit Co. Ekibi</strong></p>
            </div>";

                var smtp = new SmtpClient
                {
                    Host = "smtp.gmail.com",
                    Port = 587,
                    EnableSsl = true,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential(gondericiMail, gondericiSifre)
                };

                using (var message = new MailMessage(new MailAddress(gondericiMail, "The Edit Co."), new MailAddress(musteriEmail))
                {
                    Subject = konu,
                    Body = icerik,
                    IsBodyHtml = true
                })
                {
                    smtp.Send(message);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Mail gönderme hatası: " + ex.Message);
            }
        }
    }
}