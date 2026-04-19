using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace TheEditCo.Models
{
    public class Order
    {
        public int Id { get; set; }

        public string UserId { get; set; }

        [Required]
        public string FullName { get; set; }

        [Required]
        public string PhoneNumber { get; set; }

        [Required]
        public string AddressLine { get; set; }

        [Required]
        public string City { get; set; }

        [Required]
        public string PostalCode { get; set; }

        public decimal TotalAmount { get; set; }

        public DateTime OrderDate { get; set; } = DateTime.Now;

        public string OrderStatus { get; set; } = "Hazırlanıyor";

        public List<OrderItem> OrderItems { get; set; }
    }
}