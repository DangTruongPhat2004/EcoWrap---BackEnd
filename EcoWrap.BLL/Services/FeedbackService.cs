using EcoWrap.BLL.IServices;
using EcoWrap.DAL.Entities;
using EcoWrap.DAL.IRepositories;
using EcoWrap.Models.Request;
using EcoWrap.Models.Response;
using EcoWrap.Models.Responses;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace EcoWrap.BLL.Services
{
    public class FeedbackService : IFeedbackService
    {
        private readonly IFeedbackRepository _feedbackRepo;
        private readonly IUserRepository _userRepo;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public FeedbackService(IFeedbackRepository feedbackRepo, IUserRepository userRepo, IHttpContextAccessor httpContextAccessor)
        {
            _feedbackRepo = feedbackRepo;
            _userRepo = userRepo;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<ApiResponse<FeedbackResponse>> CreateFeedbackAsync(CreateFeedbackRequest request)
        {
            try
            {
                // 1. Kiểm tra User tồn tại (tốt)
                var userIdString = _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out var userId))
                {
                    return new ApiResponse<FeedbackResponse> { Success = false, Message = "Người dùng không được xác thực." };
                }
                var user = await _userRepo.GetByIdAsync(userId);
                if (user == null)
                {
                    return new ApiResponse<FeedbackResponse> { Success = false, Message = "Người dùng không tồn tại." };
                }

                // 2. Tạo feedback (tốt)
                var feedback = new Feedback
                {
                    UserId = userId,
                    Comment = request.Comment,
                    Rating = request.Rating,
                    FeedbackDate = DateTime.UtcNow.AddHours(7)
                };

                await _feedbackRepo.AddAsync(feedback);
                await _feedbackRepo.SaveChangesAsync(); // feedback.FeedbackId đã được cập nhật

                // 3. Lấy lại feedback đầy đủ (tốt)
                var newFeedbackWithUser = await _feedbackRepo.GetByIdWithUserAsync(feedback.FeedbackId);

                //    Dùng newFeedbackWithUser (đã Include) thay vì (feedback, user) (biến cũ)
                var responseData = MapToFeedbackResponse(newFeedbackWithUser!);

                return new ApiResponse<FeedbackResponse>
                {
                    Success = true,
                    Data = responseData, // Bây giờ Data đã đầy đủ
                    Message = "Gửi đánh giá thành công. Cảm ơn bạn đã đóng góp ý kiến!"
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FeedbackService.CreateFeedbackAsync] EXCEPTION: {ex.ToString()}");
                return new ApiResponse<FeedbackResponse> { Success = false, Message = "Lỗi hệ thống khi gửi đánh giá." };
            }
        }

        public async Task<ApiResponse<bool>> DeleteFeedbackAsync(int feedbackId)
        {
            try
            {
                var feedback = await _feedbackRepo.GetByIdAsync(feedbackId);
                if (feedback == null)
                {
                    return new ApiResponse<bool> { Success = false, Message = "Không tìm thấy đánh giá này." };
                }

                _feedbackRepo.Delete(feedback);
                await _feedbackRepo.SaveChangesAsync();

                return new ApiResponse<bool> { Success = true, Message = "Xóa đánh giá thành công." };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FeedbackService.DeleteFeedbackAsync] EXCEPTION: {ex.ToString()}");
                return new ApiResponse<bool> { Success = false, Message = "Lỗi hệ thống khi xóa đánh giá." };
            }
        }

        public async Task<ApiResponse<List<FeedbackResponse>>> GetAllFeedbackAsync()
        {
            try
            {
                var feedbacks = await _feedbackRepo.GetAllWithUserAsync();

                var responseData = feedbacks
                    .Select(MapToFeedbackResponse)
                    .ToList();

                return new ApiResponse<List<FeedbackResponse>>
                {
                    Success = true,
                    Data = responseData,
                    Message = $"Lấy thành công {responseData.Count} đánh giá."
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FeedbackService.GetAllFeedbackAsync] EXCEPTION: {ex.ToString()}");
                return new ApiResponse<List<FeedbackResponse>> { Success = false, Message = "Lỗi hệ thống khi lấy danh sách đánh giá." };
            }
        }

        // --- 1. ViewMyFeedback
        public async Task<ApiResponse<List<FeedbackResponse>>> GetMyFeedbackAsync()
        {
            try
            {
                // Lấy UserId của user đang đăng nhập
                var authResult = TryGetAuthenticatedUserId();
                if (!authResult.Success)
                {
                    return new ApiResponse<List<FeedbackResponse>> { Success = false, Message = authResult.ErrorMessage };
                }

                // Dùng UserId đã lấy để gọi Repository
                var feedbacks = await _feedbackRepo.GetByUserIdAsync(authResult.UserId);
                var responseData = feedbacks.Select(MapToFeedbackResponse).ToList();

                return new ApiResponse<List<FeedbackResponse>>
                {
                    Success = true,
                    Data = responseData,
                    Message = $"Lấy thành công {responseData.Count} đánh giá của bạn."
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FeedbackService.GetMyFeedbackAsync] EXCEPTION: {ex.ToString()}");
                return new ApiResponse<List<FeedbackResponse>> { Success = false, Message = $"Lỗi hệ thống: {ex.Message}" };
            }
        }

        // 2. UpdateMyFeedback
        public async Task<ApiResponse<FeedbackResponse>> UpdateMyFeedbackAsync(int feedbackId, UpdateFeedbackRequest request)
        {
            try
            {
                // Lấy UserId của user đang đăng nhập
                var authResult = TryGetAuthenticatedUserId();
                if (!authResult.Success)
                {
                    return new ApiResponse<FeedbackResponse> { Success = false, Message = authResult.ErrorMessage };
                }

                // Lấy feedback (kèm User để map response)
                var feedback = await _feedbackRepo.GetByIdWithUserAsync(feedbackId);
                if (feedback == null)
                    return new ApiResponse<FeedbackResponse> { Success = false, Message = "Không tìm thấy đánh giá này." };

                // Kiểm tra quyền sở hữu
                if (feedback.UserId != authResult.UserId)
                    return new ApiResponse<FeedbackResponse> { Success = false, Message = "Bạn không có quyền sửa đánh giá này." };

                // Cập nhật
                feedback.Comment = request.Comment;
                feedback.Rating = request.Rating;
                feedback.FeedbackDate = DateTime.UtcNow.AddHours(7);

                await _feedbackRepo.SaveChangesAsync();

                return new ApiResponse<FeedbackResponse>
                {
                    Success = true,
                    Data = MapToFeedbackResponse(feedback),
                    Message = "Cập nhật đánh giá thành công."
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FeedbackService.UpdateMyFeedbackAsync] EXCEPTION: {ex.ToString()}");
                return new ApiResponse<FeedbackResponse> { Success = false, Message = $"Lỗi hệ thống: {ex.Message}" };
            }
        }

        // 3. DeleteMyFeedback
        public async Task<ApiResponse<bool>> DeleteMyFeedbackAsync(int feedbackId)
        {
            try
            {
                // Lấy UserId của user đang đăng nhập
                var authResult = TryGetAuthenticatedUserId();
                if (!authResult.Success)
                {
                    return new ApiResponse<bool> { Success = false, Message = authResult.ErrorMessage };
                }

                // Lấy feedback (không cần Include User, chỉ cần để xóa)
                var feedback = await _feedbackRepo.GetByIdAsync(feedbackId);
                if (feedback == null)
                    return new ApiResponse<bool> { Success = false, Message = "Không tìm thấy đánh giá này." };

                // Kiểm tra quyền sở hữu
                if (feedback.UserId != authResult.UserId)
                    return new ApiResponse<bool> { Success = false, Message = "Bạn không có quyền xóa đánh giá này." };

                _feedbackRepo.Delete(feedback);
                await _feedbackRepo.SaveChangesAsync();

                return new ApiResponse<bool> { Success = true, Message = "Xóa đánh giá thành công." };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FeedbackService.DeleteMyFeedbackAsync] EXCEPTION: {ex.ToString()}");
                return new ApiResponse<bool> { Success = false, Message = $"Lỗi hệ thống: {ex.Message}" };
            }
        }

        // --- Hàm Helper ---
        private FeedbackResponse MapToFeedbackResponse(Feedback feedback)
        {
            if (feedback == null) return null;

            return new FeedbackResponse
            {
                FeedbackId = feedback.FeedbackId,
                UserId = feedback.UserId,
                Comment = feedback.Comment,
                Rating = feedback.Rating,
                FeedbackDate = feedback.FeedbackDate,

                Username = feedback.User?.Username,
                UserFullName = feedback.User?.FullName,
                UserEmail = feedback.User?.Email
            };
        }

        private (bool Success, int UserId, string ErrorMessage) TryGetAuthenticatedUserId()
        {
            var userIdString = _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out var userId))
            {
                // Nếu không lấy được ID, trả về lỗi
                return (false, 0, "Người dùng không được xác thực.");
            }
            // Nếu lấy được ID, trả về thành công
            return (true, userId, null);
        }
    }
}