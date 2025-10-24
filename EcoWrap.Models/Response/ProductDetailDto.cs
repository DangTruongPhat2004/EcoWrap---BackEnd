using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EcoWrap.Models.DTO
{
    public class ProductDetailDto
    {
        public string ProductName { get; set; }
        public int WeightGram { get; set; } // Trọng lượng theo gam (bắt buộc cho GHTK)
        // Nếu cần các thông tin khác (ví dụ: ProductPrice, SellerId) thì thêm vào đây
    }
}