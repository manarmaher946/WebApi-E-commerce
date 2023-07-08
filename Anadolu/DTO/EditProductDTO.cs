﻿using System.ComponentModel.DataAnnotations;

namespace Anadolu.DTO
{
    public class EditProductDTO
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public float Price { get; set; }
        [Required]
        public bool? IsAvailable { get; set; }
        public string Description { get; set; }

        public int SubCategoryId { get; set; }
        public IFormFile? File { get; set; }
    }
}
