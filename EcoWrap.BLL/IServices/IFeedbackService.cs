using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EcoWrap.Models.Request;
using EcoWrap.Models.Response;

namespace EcoWrap.BLL.IServices
{
    public interface IFeedbackService
    {
        Task<ApiResponse<FeedbackResponse>> CreateFeedbackAsync(CreateFeedbackRequest request);
        Task<ApiResponse<List<FeedbackResponse>>> GetAllFeedbackAsync();
        Task<ApiResponse<bool>> DeleteFeedbackAsync(int feedbackId);
        Task<ApiResponse<List<FeedbackResponse>>> GetMyFeedbackAsync();
        Task<ApiResponse<FeedbackResponse>> UpdateMyFeedbackAsync(int feedbackId, UpdateFeedbackRequest request);
        Task<ApiResponse<bool>> DeleteMyFeedbackAsync(int feedbackId);
    }
}