using System;
using System.ComponentModel.DataAnnotations;

namespace EcoWrap.BLL.DTOs.Request
{
    public class CreateCartRequest
    {
        [Required]
        public int CustomerId { get; set; }

        public decimal? CartTotalPrice { get; set; }

        public bool? IsActive { get; set; } = true;
    }
}