using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EcoWrap.Models.Request
{
    public class CreateFeedbackRequest
    {
        [Required(ErrorMessage = "Vui lòng nhập nội dung đánh giá.")]
        [StringLength(1000, ErrorMessage = "Nội dung đánh giá không được vượt quá 1000 ký tự.")]
        public string? Comment { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn điểm đánh giá.")]
        [Range(1, 5, ErrorMessage = "Điểm đánh giá phải từ 1 đến 5 sao.")]
        public byte Rating { get; set; }
    }
}