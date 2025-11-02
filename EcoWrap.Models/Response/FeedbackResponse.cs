using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EcoWrap.Models.Response
{
    public class FeedbackResponse
    {
        public int FeedbackId { get; set; }
        public int UserId { get; set; }
        public string? Comment { get; set; }
        public byte Rating { get; set; }
        public DateTime? FeedbackDate { get; set; }

        // Thông tin người gửi (lấy từ User liên kết)
        public string? Username { get; set; }
        public string? UserFullName { get; set; }
        public string? UserEmail { get; set; }
    }
}