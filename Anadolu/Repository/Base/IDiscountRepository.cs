﻿using Anadolu.Models;

namespace Anadolu.Repository.Base
{
    public interface IDiscountRepository : IRepository<Discount>
    {
        Discount GetDiscountByProductId(int productId);
    }
}
