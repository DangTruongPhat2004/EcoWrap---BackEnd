using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EcoWrap.DAL.Entities;

namespace EcoWrap.DAL.IRepositories
{
    public interface IFeedbackRepository
    {
        Task<IEnumerable<Feedback>> GetAllWithUserAsync();
        Task<IEnumerable<Feedback>> GetByUserIdAsync(int userId);
        Task<Feedback?> GetByIdWithUserAsync(int feedbackId);
        Task<Feedback?> GetByIdAsync(int feedbackId);
        Task AddAsync(Feedback feedback);
        void Delete(Feedback feedback);
        Task<int> SaveChangesAsync();
        Task<(int Count, double AverageRating)> GetFeedbackMetricsAsync();
    }
}