using System.Runtime.InteropServices;

namespace Anadolu.DTO
{
    public class AddToCartDTO
    {
        [NumbersOnly]
        public int ProductId { get; set; }
        [NumbersOnly]
        public int Quantity { get; set; }
        public string UserId { get; set; }
    }
}
