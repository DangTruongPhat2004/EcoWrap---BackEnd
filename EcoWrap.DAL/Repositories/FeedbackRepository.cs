using EcoWrap.DAL.Entities;
using EcoWrap.DAL.IRepositories;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EcoWrap.DAL.Repositories
{
    public class FeedbackRepository : IFeedbackRepository
    {
        private readonly EcoWrapDbContext _context;

        public FeedbackRepository(EcoWrapDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Feedback>> GetAllWithUserAsync()
        {
            return await _context.Feedbacks
                .Include(f => f.User)
                .OrderByDescending(f => f.FeedbackDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<Feedback>> GetByUserIdAsync(int userId)
        {
            return await _context.Feedbacks
                .Where(f => f.UserId == userId)
                .Include(f => f.User)
                .OrderByDescending(f => f.FeedbackDate)
                .ToListAsync();
        }

        public async Task<Feedback?> GetByIdWithUserAsync(int feedbackId)
        {
            return await _context.Feedbacks
                .Include(f => f.User)
                .FirstOrDefaultAsync(f => f.FeedbackId == feedbackId);
        }

        public async Task<Feedback?> GetByIdAsync(int feedbackId)
        {
            return await _context.Feedbacks.FindAsync(feedbackId);
        }

        public async Task AddAsync(Feedback feedback)
        {
            await _context.Feedbacks.AddAsync(feedback);
        }

        public void Delete(Feedback feedback)
        {
            _context.Feedbacks.Remove(feedback);
        }

        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }

        public async Task<(int Count, double AverageRating)> GetFeedbackMetricsAsync()
        {
            var feedbackQuery = _context.Feedbacks.AsQueryable();

            int count = await feedbackQuery.CountAsync();

            double averageRating = 0;
            if (count > 0)
            {
                averageRating = await feedbackQuery.AverageAsync(f => (double)f.Rating);
            }

            return (count, averageRating);
        }
    }
}