using System.ComponentModel.DataAnnotations;

namespace TheEditCo.Models
{
    public class CheckoutViewModel
    {
        [Required(ErrorMessage = "Ad soyad zorunludur.")]
        public string FullName { get; set; }

        [Required(ErrorMessage = "Telefon numarası zorunludur.")]
        [Phone(ErrorMessage = "Geçerli bir telefon numarası giriniz.")]
        public string PhoneNumber { get; set; }

        [Required(ErrorMessage = "Adres zorunludur.")]
        public string AddressLine { get; set; }

        [Required(ErrorMessage = "Şehir zorunludur.")]
        public string City { get; set; }

        [Required(ErrorMessage = "Posta kodu zorunludur.")]
        public string PostalCode { get; set; }

        [Required(ErrorMessage = "Kart üzerindeki isim zorunludur.")]
        public string CardHolderName { get; set; }

        [Required(ErrorMessage = "Kart numarası zorunludur.")]
        public string CardNumber { get; set; }

        [Required(ErrorMessage = "Son kullanma tarihi zorunludur.")]
        public string ExpiryDate { get; set; }

        [Required(ErrorMessage = "CVV zorunludur.")]
        public string CVV { get; set; }
    }
}