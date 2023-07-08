using System.ComponentModel.DataAnnotations;

namespace Anadolu.DTO
{
    public class OrderDTO
    {
        public string CustomerName { get; set; }
        public DateTime Date { get; set; }
        [Required]
        public decimal? TotalPrice { get; set; }
        public int OrderId { get; set; }
        public string UserId { get; set; }
    }
}
