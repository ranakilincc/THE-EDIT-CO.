using System;

namespace EDITCOWEB.Models
{
    public class Product
    {
        public int Id { get; set; }
        public string UrunAdi { get; set; }
        public string Kategori { get; set; }
        public decimal Fiyat { get; set; }
        public string Aciklama { get; set; }
        public string ResimYolu { get; set; }
        public string CiltTipi { get; set; }
        public int StokMiktari { get; set; }
        public DateTime EklenmeTarihi { get; set; }
        public double OrtalamaPuan { get; set; }
        public int YorumSayisi { get; set; }
    }
}
