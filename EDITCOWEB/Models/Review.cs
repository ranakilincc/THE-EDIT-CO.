using System;

namespace EDITCOWEB.Models
{
    public class Review
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string KullaniciAdi { get; set; }
        public int Puan { get; set; }
        public string YorumMetni { get; set; }
        public DateTime Tarih { get; set; }
    }
}