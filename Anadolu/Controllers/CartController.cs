using Anadolu.DTO;
using Anadolu.Models;
using Anadolu.Repository.Base;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Anadolu.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CartController : ControllerBase
    {

        IUnitOfWork unitOfWork;


        public CartController(IUnitOfWork _unitOfWork)
        {

            unitOfWork = _unitOfWork;
        }

        //[Authorize(Roles = ("User"))]
        [HttpGet("CartItems/{id}")]
        public IActionResult GetCartItemsByID(string id)
        {
            ResultDTO result = new ResultDTO();

            List<ProductCart> CartItems = unitOfWork.CartRepository.GetCartItemsById(id, l => l.IsDeleted == false);


            List<CartProductDTO> cartProductDTO = new List<CartProductDTO>();
            foreach (var c in CartItems)
            {
                CartProductDTO cartproduct = new CartProductDTO
                {
                    Id = c.Id,
                    ProductId=c.ProductId,
                    ProductName = c.Product.Name,
                    ProductPrice = c.Product.Price,
                    Quantity=c.Quantity,
                    ProductImage = c.Product.ImagePath,

                };
                cartProductDTO.Add(cartproduct);
            }
            result.IsPassed = true;
            result.Data = cartProductDTO;
            return Ok(result);
         }

        //[Authorize(Roles = ("User"))]

        [HttpGet("GetProductCount/{userid}")]
        public IActionResult GetProductCount(string userid)
        {
            var ProductsCount = unitOfWork.ProductCartRepository.GetCountofItems(userid);
            return Ok(ProductsCount);
        }

        //[Authorize(Roles = ("User"))]
        [HttpDelete("Delete/{productid}")]
        public IActionResult Delete(int productid)
        {
            unitOfWork.ProductCartRepository.Delete(productid);
            return Ok();
        }

    }
}
