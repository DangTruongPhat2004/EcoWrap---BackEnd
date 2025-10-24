// File: BLL/Services/HangfireService.cs
using EcoWrap.BLL.IServices;
using Hangfire; // Thêm using này
using System;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace EcoWrap.BLL.Services
{
    public class HangfireService : IBackgroundJobService
    {
        public string Schedule<T>(Expression<Func<T, Task>> methodCall, TimeSpan delay)
        {
            // Sử dụng BackgroundJob.Schedule của Hangfire
            // Hangfire sẽ tự động inject (DI) service (ví dụ: IOrderService) khi job chạy
            var jobId = BackgroundJob.Schedule<T>(methodCall, delay);
            return jobId;
        }

        public bool CancelScheduledJob(string jobId)
        {
            if (string.IsNullOrEmpty(jobId))
            {
                return false;
            }

            try
            {
                // Sử dụng BackgroundJob.Delete của Hangfire
                return BackgroundJob.Delete(jobId);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Hangfire Error] Failed to delete job {jobId}: {ex.Message}");
                return false;
            }
        }
        public string Enqueue<T>(Expression<Action<T>> methodCall)
        {
            // Sử dụng BackgroundJob.Enqueue của Hangfire
            var jobId = BackgroundJob.Enqueue<T>(methodCall);
            return jobId;
        }
    }
}