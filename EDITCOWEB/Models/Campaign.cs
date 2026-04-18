namespace EDITCOWEB.Models
{
    public class Campaign
    {
        public int Id { get; set; }
        public string Baslik { get; set; }
        public string Aciklama { get; set; }
        public string ResimYolu { get; set; }
        public bool AktifMi { get; set; }
        public DateTime BitisTarihi { get; set; }
    }
}
