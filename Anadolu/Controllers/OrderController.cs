using Anadolu.DTO;
using Anadolu.Models;
using Anadolu.Repository.Base;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Stripe;
using Stripe.Issuing;
using System.Globalization;

namespace Anadolu.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrderController : ControllerBase
    {
        private readonly IUnitOfWork unit;
        private readonly IConfiguration config;
        public OrderController(IUnitOfWork _unit, IConfiguration config)
        {
            unit = _unit;
            this.config = config;
        }

        //[Authorize(Roles = ("Admin"))]
        [HttpGet("Orders")]
        public IActionResult GetAllOrders()
        {
            ResultDTO result = new ResultDTO();
            List<Order> orders = unit.OrderRepository.GetAll(o => o.IsDeleted == false);
            if (orders != null)
            {
                List<OrderDTO> orderDTOs = new List<OrderDTO>();
                foreach (Order order in orders)
                {
                    OrderDTO dto = new OrderDTO();
                    dto.OrderId = order.Id;
                    dto.UserId = order.UserId;
                    dto.TotalPrice = order.TotalPrice;
                    dto.CustomerName = order.Name;
                    dto.Date = order.Date;

                    orderDTOs.Add(dto);
                }
                result.IsPassed = true;
                result.Data = orderDTOs;
                return Ok(result);

            }
            result.IsPassed = false;
            result.Data = "No Orders Exist";
            return Ok(result);
        }

        //[Authorize(Roles = ("Admin"))]
        [HttpGet("OrderDetails/{Id}")]
        public IActionResult GetOrderById(int Id)
        {
            ResultDTO result = new ResultDTO();
            Order order = unit.OrderRepository.GetById(Id, a => a.IsDeleted == false);
            if (order != null)
            {
                ReturnOrderDetailsDTO returnOrderDetails = new ReturnOrderDetailsDTO();
                OrderStatus orderStatus = unit.OrderStatusRepository.GetById(order.OrderStatusId, o => o.IsDeleted == false);
                returnOrderDetails.OrderStatusName = orderStatus.Status;
                User user = unit.UserRepository.GetByIdString(order.UserId, u => u.IsDeleted == false);

                returnOrderDetails.UserName = user.FirstName + " " + user.LastName;

                ApplicationUser appUser = unit.ApplicationUserRepository
                    .GetByIdString(order.UserId, u => u.IsDeleted == false);

                returnOrderDetails.PhoneNumber = appUser.PhoneNumber;
                returnOrderDetails.OrderId = order.Id;

                returnOrderDetails.Date = order.Date;
                returnOrderDetails.TotalPrice = order.TotalPrice;
                returnOrderDetails.OrderStatusId = order.OrderStatusId;


                List<ProductOrderInOrderDetailsDTO> productOrderInOrderDetails = new List<ProductOrderInOrderDetailsDTO>();
                List<ProductOrder> productOrders = unit.ProductOrderRepository
                    .GetByOrderId(order.Id, po => po.IsDeleted == false);
                for (int i = 0; i < productOrders.Count; i++)
                {
                    ProductOrderInOrderDetailsDTO productOrderInOrder = new ProductOrderInOrderDetailsDTO();

                    productOrderInOrder.ProductName = productOrders[i].Product.Name;
                    productOrderInOrder.ProductPrice = productOrders[i].Product.Price;
                    productOrderInOrder.Quantity = productOrders[i].Quantity;
                    productOrderInOrder.ProductOrderTotalPrice = (decimal)productOrderInOrder.ProductPrice
                        * productOrderInOrder.Quantity;
                    productOrderInOrderDetails.Add(productOrderInOrder);

                }
                returnOrderDetails.ProductOrderInOrderDetails = productOrderInOrderDetails;

                result.IsPassed = true;
                result.Data = returnOrderDetails;
                return Ok(result);
            }
            result.IsPassed = false;
            result.Data = "No Order Exist with This Id";
            return Ok(result);
        }

        //[Authorize]
        [HttpPut("updateproductcart")]
        public IActionResult UpdateProductCart(UpdateAllProductCartsDTO updateAllProductCartsDTO)
        {
            ResultDTO result = new ResultDTO();
            if (ModelState.IsValid)
            {
                Cart cart = unit.CartRepository.GetByIdString(updateAllProductCartsDTO
                    .CartId, c => c.IsDeleted == false);

                if (cart != null)
                {
                    List<ProductCart> productCarts = unit.CartRepository
                        .GetCartItemsById(updateAllProductCartsDTO.CartId, c => c.IsDeleted == false);

                    for (int i = 0; i < productCarts.Count; i++)
                    {
                        productCarts[i].Quantity = updateAllProductCartsDTO.ProductCartDTOs[i].Quantity;

                        unit.ProductCartRepository.Update(productCarts[i].ProductId, productCarts[i]);
                    }
                    result.IsPassed = true;
                    result.Data = updateAllProductCartsDTO.ProductCartDTOs;
                    return Ok(result);
                }
                result.IsPassed = false;
                result.Data = "No Cart With This Id";
                return BadRequest(result);
            }
            result.IsPassed = false;
            result.Data = "ModelState Is Invalid";
            return BadRequest(result);
        }

        //[Authorize(Roles = ("User"))]
        [HttpPost("completeorder")]
        public IActionResult SubmitOrder([FromBody] UserIdDTO UserId)
        {
            ResultDTO result = new ResultDTO();
            User user = unit.UserRepository.GetByIdString(UserId.Id, a => a.IsDeleted == false);
            if (user != null)
            {
                Cart cart = unit.CartRepository.GetByIdString(UserId.Id, c => c.IsDeleted == false);
                List<ProductCart> productCarts = cart.ProductCarts.ToList();

                if (productCarts != null)
                {
                    Order order = new Order();
                    order.UserId = UserId.Id;
                    order.Name = user.FirstName + " " + user.LastName;
                    order.Date = DateTime.Now;
                    order.OrderStatusId = 2;

                    // فاضل السعر الكلي والليستة تتملي
                    unit.OrderRepository.Add(order);

                    List<ProductOrder> productOrders = new List<ProductOrder>();
                    decimal totalPrice = 0m;
                    foreach (ProductCart productCart in productCarts)
                    {
                        Models.Product product = unit.ProductRepository.GetById(productCart.ProductId, p => p.IsDeleted == false);

                        ProductOrder productOrder = new ProductOrder()
                        {
                            Quantity = productCart.Quantity,
                            ProductId = productCart.ProductId,
                            TotalPrice = productCart.TotalPrice * productCart.Quantity,
                            OrderId = order.Id
                        };
                        totalPrice += productOrder.TotalPrice;

                        productOrders.Add(productOrder);
                        unit.ProductOrderRepository.Add(productOrder);
                    }
                    order.ProductOrders = productOrders;
                    order.TotalPrice = totalPrice;

                    try
                    {
                        // Payment Intent Steps
                        StripeConfiguration.ApiKey = config["StripeSettings:SecretKey"];
                        PaymentIntent intent;
                        var options = new PaymentIntentCreateOptions
                        {
                            Amount = (long)(order.TotalPrice * 100),
                            Currency = "try",
                            PaymentMethodTypes = new List<string> { "card" }
                        };
                        var service = new PaymentIntentService();
                        intent = service.Create(options);

                        order.PaymentIntentId = intent.Id;
                        order.ClientSecret = intent.ClientSecret;

                        unit.OrderRepository.Update(order.Id, order);

                        ReturnOrderWithPayment returnOrderWithPayment = new ReturnOrderWithPayment();

                        returnOrderWithPayment.Id = order.Id;
                        returnOrderWithPayment.PaymentIntentId = order.PaymentIntentId;
                        returnOrderWithPayment.UserId = order.UserId;
                        returnOrderWithPayment.OrderStatusId = order.OrderStatusId;
                        returnOrderWithPayment.CustomerName = order.Name;
                        returnOrderWithPayment.ClientSecret = order.ClientSecret;
                        returnOrderWithPayment.TotalPrice = order.TotalPrice;
                        returnOrderWithPayment.Date = order.Date;

                        foreach (ProductCart pc in productCarts)
                        {
                            unit.ProductCartRepository.Delete(pc.Id);
                        }
                        cart.ProductCarts = new List<ProductCart>();

                        result.IsPassed = true;
                        result.Data = returnOrderWithPayment;
                        return Ok(result);
                    }
                    catch (Exception ex)
                    {
                        // Handle the exception appropriately
                        result.IsPassed = false;
                        result.Data = "Error occurred during payment: " + ex.Message;
                        return BadRequest(result);
                    }
                }
                result.IsPassed = false;
                result.Data = "No ProductCarts Exist";
                return BadRequest(result);
            }
            result.IsPassed = false;
            result.Data = "No User Exist";
            return BadRequest(result);
        }



        //[Authorize(Roles =("User"))]
        [HttpPost("addproductcart")]
        public IActionResult AddProductCart(SingleProductCartDTO productCartDTO)
        {
            ResultDTO result = new ResultDTO();
            if (ModelState.IsValid)
            {
                User user = unit.UserRepository.GetByIdString(productCartDTO.CartId, u => u.IsDeleted == false);
                Models.Product product = unit.ProductRepository.GetById(productCartDTO.ProductId, p => p.IsDeleted == false);
                if (product != null && user != null)
                {
                    if ((bool)product.IsAvailable)
                    {
                        List<ProductCart> productCarts = unit.CartRepository
                            .GetCartItemsById(user.ApplicationUserId, p => p.IsDeleted == false);
                        bool flag = false;
                        int productCartId = 0;
                        foreach (ProductCart cartItem in productCarts)
                        {
                            if (cartItem.ProductId == productCartDTO.ProductId)
                            {
                                flag = true;
                                productCartId = cartItem.Id;
                                break;
                            }
                        }
                        if (flag == false)
                        {
                            ProductCart productCart = new ProductCart();
                            productCart.ProductId = productCartDTO.ProductId;
                            productCart.Quantity = productCartDTO.Quantity;
                            productCart.CartId = productCartDTO.CartId;
                            productCart.TotalPrice = (decimal)product.Price * productCartDTO.Quantity;

                            unit.ProductCartRepository.Add(productCart);

                            ReturnProductCartDTO returnProductCartDTO = new ReturnProductCartDTO();
                            returnProductCartDTO.ProductId = productCartDTO.ProductId;
                            returnProductCartDTO.CartId = productCartDTO.CartId;
                            returnProductCartDTO.Quantity = productCartDTO.Quantity;
                            returnProductCartDTO.TotalPrice = productCart.TotalPrice;
                            returnProductCartDTO.ProductId = productCart.ProductId;


                            result.IsPassed = true;

                            result.Data = returnProductCartDTO;
                            return Ok(result);
                        }
                        else
                        {
                            ProductCart productCart = unit.ProductCartRepository
                                .GetById(productCartId, pc => pc.IsDeleted == false);

                            productCart.ProductId = productCartDTO.ProductId;
                            productCart.Quantity += productCartDTO.Quantity;
                            productCart.CartId = productCartDTO.CartId;
                            productCart.TotalPrice = (decimal)product.Price * productCartDTO.Quantity;

                            unit.ProductCartRepository.Update(productCart.Id, productCart);

                            ReturnProductCartDTO returnProductCartDTO = new ReturnProductCartDTO();
                            returnProductCartDTO.ProductId = productCartDTO.ProductId;
                            returnProductCartDTO.CartId = productCartDTO.CartId;
                            returnProductCartDTO.Quantity = productCartDTO.Quantity;
                            returnProductCartDTO.TotalPrice = productCart.TotalPrice;
                            returnProductCartDTO.ProductId = productCart.ProductId;
                            result.IsPassed = true;
                            result.Data = returnProductCartDTO;
                            return Ok(result);
                        }
                    }
                    result.IsPassed = false;
                    result.Data = "This Product is UnAvailable";
                    return BadRequest(result);
                }
                result.IsPassed = false;
                result.Data = "No User OR Product Exist";
                return BadRequest(result);
            }
            result.IsPassed = false;
            result.Data = "ModelState Is Invalid";
            return BadRequest(result);
        }
        [HttpPut("editorderstatusid")]
        public IActionResult EditOrderStatusId(EditOrderStatusIdDTO orderStatusId)
        {
            ResultDTO result = new ResultDTO();

            if (ModelState.IsValid)
            {
                Order order = unit.OrderRepository.GetById(orderStatusId.OrderId, o => o.IsDeleted == false);
                if (order != null)
                {
                    order.OrderStatusId = orderStatusId.OrderStatusId;
                    unit.OrderRepository.Update(order.Id, order);
                    result.IsPassed = true;
                    result.Data = orderStatusId;
                    return Ok(result);
                }
                result.IsPassed = false;
                result.Data = "No Order with This Id Exist";
                return BadRequest(result);
            }
            result.IsPassed = false;
            result.Data = "This ModelState is Invalid";
            return BadRequest(result);
        }

        [HttpGet("orderadditions/{cartId}")]
        public IActionResult OrderAdditions(String cartId)
        {
            ResultDTO result = new ResultDTO();
            Cart cart = unit.CartRepository.GetByIdString(cartId, o=>o.IsDeleted == false);
            if(cart != null)
            {
                OrderAdditionsDTO dto = new OrderAdditionsDTO();

                List<ProductCart> productCarts = cart.ProductCarts.ToList();
                decimal totalPrice = 0m;
                foreach (ProductCart productCart in productCarts)
                {
                    Models.Product product = unit.ProductRepository.GetById(productCart.ProductId, p => p.IsDeleted == false);
                    totalPrice += productCart.TotalPrice * productCart.Quantity;

                    
                    //totalPrice += productOrder.TotalPrice;

                    //productOrders.Add(productOrder);
                    //unit.ProductOrderRepository.Add(productOrder);
                }
                //order.ProductOrders = productOrders;
                dto.OrderTotalPrice= totalPrice;
                dto.ShipingCost = 10.0m;
                dto.FinalTotalPrice = dto.ShipingCost + totalPrice;

                //dto.OrderTotalPrice = order.TotalPrice;
                //dto.ShipingCost = 10;
                //dto.FinalTotalPrice = dto.OrderTotalPrice + dto.ShipingCost;
                
                result.Data = dto;
                result.IsPassed = true;
                return Ok(result);
            }

            result.IsPassed=false;
            result.Data = "No Order Exist";
            return BadRequest(result);
        }
    }
}


/*
        [Authorize]
        [HttpPost("addproductcart")]

        public IActionResult AddProductCart(ProductCartDTO productCartDTO)
        {
            ResultDTO result = new ResultDTO();
            if (ModelState.IsValid)
            {
                Cart cart = unit.CartRepository.GetByIdString(productCartDTO.CartId, u => u.IsDeleted == false);
                Models.Product product = unit.ProductRepository.GetById(productCartDTO.ProductId, p => p.IsDeleted == false);
                if (product != null && cart != null)
                {
                    List<ProductCart> productCarts = unit.CartRepository
                        .GetCartItemsById(cart.UserId, p => p.IsDeleted == false);
                    bool flag = false;
                    int productCartId = 0;
                    foreach(ProductCart cartItem in productCarts)
                    {
                        if(cartItem.ProductId == productCartDTO.ProductId)
                        {
                            flag = true;
                            productCartId = cartItem.Id;
                            break;
                        }
                    }
                    if (flag == false)
                    {
                        ProductCart productCart = new ProductCart();
                        productCart.ProductId = productCartDTO.ProductId;
                        productCart.Quantity = productCartDTO.Quantity;
                        productCart.CartId = productCartDTO.CartId;
                        productCart.TotalPrice = (decimal)product.Price * productCartDTO.Quantity;

                        unit.ProductCartRepository.Add(productCart);

                        ReturnProductCartDTO returnProductCartDTO = new ReturnProductCartDTO();
                        returnProductCartDTO.ProductId = productCartDTO.ProductId;
                        returnProductCartDTO.CartId = productCartDTO.CartId;
                        returnProductCartDTO.Quantity = productCartDTO.Quantity;
                        returnProductCartDTO.TotalPrice = productCart.TotalPrice;

                        result.IsPassed = true;
                        result.Data = returnProductCartDTO;
                        return Ok(result);
                    }
                    else
                    {
                        ProductCart productCart = unit.ProductCartRepository
                            .GetById(productCartId, pc => pc.IsDeleted == false);
                        
                        productCart.ProductId = productCartDTO.ProductId;
                        productCart.Quantity += productCartDTO.Quantity;
                        productCart.CartId = productCartDTO.CartId;
                        productCart.TotalPrice = (decimal)product.Price * productCartDTO.Quantity;

                        unit.ProductCartRepository.Update(productCart.Id,productCart);

                        ReturnProductCartDTO returnProductCartDTO = new ReturnProductCartDTO();
                        returnProductCartDTO.ProductId = productCartDTO.ProductId;
                        returnProductCartDTO.CartId = productCartDTO.CartId;
                        returnProductCartDTO.Quantity = productCartDTO.Quantity;
                        returnProductCartDTO.TotalPrice = productCart.TotalPrice;

                        result.IsPassed = true;
                        result.Data = returnProductCartDTO;
                        return Ok(result);
                    }
                }
                result.IsPassed = false;
                result.Data = "No User OR Product Exist";
                return BadRequest(result);
            }
            result.IsPassed = false;
            result.Data = "ModelState Is Invalid";
            return BadRequest(result);
        }
 */