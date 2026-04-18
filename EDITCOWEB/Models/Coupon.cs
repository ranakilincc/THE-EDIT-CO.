using System;

namespace EDITCOWEB.Models
{
    public class Coupon
    {
        public int Id { get; set; }
        public string Kod { get; set; }
        public int IndirimYuzdesi { get; set; }
        public DateTime BitisTarihi { get; set; }
        public bool AktifMi { get; set; }
    }
}
