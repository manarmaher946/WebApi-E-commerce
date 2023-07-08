﻿using Anadolu.Models;
using Anadolu.Repository.Base;
using Microsoft.EntityFrameworkCore;

namespace Anadolu.Repository
{
    public class CartRepository : Repository<Cart>, ICartRepository
    {
        private readonly Context Context;
        public CartRepository(Context _Context) : base(_Context)
        {
            Context = _Context;
        }

        public List<ProductCart> GetCartItemsById(string userid, Func<ProductCart, bool> predicate)
        {
            var cart = Context.Carts.Include(p => p.ProductCarts).ThenInclude(p=>p.Product)
                .Where(c => c.UserId == userid)
                .FirstOrDefault();

            var cartItems = Context.ProductCarts.Include(p => p.Product).
                Where(p => p.CartId == cart.UserId).ToList();


            return cartItems;
        }
        public override Cart GetByIdString(string id, Func<Cart, bool> predicate)
        {
            Cart entity = Context.Carts.Include(c => c.ProductCarts).Where(predicate)
                .Where(c => c.UserId == id).FirstOrDefault();

            return entity;
        }
    }
}
