namespace EDITCOWEB.Models
{
    public class CustomerViewModel
    {
        public int Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public DateTime? CreatedAt { get; set; } // Üyelik Tarihi
        public string CiltTipi { get; set; } // Müşterinin Cilt Tipi
    }
}