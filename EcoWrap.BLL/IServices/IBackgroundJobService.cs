// File: BLL/IServices/IBackgroundJobService.cs
using System;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace EcoWrap.BLL.IServices
{
    public interface IBackgroundJobService
    {
        string Schedule<T>(Expression<Func<T, Task>> methodCall, TimeSpan delay);
        bool CancelScheduledJob(string jobId);
        string Enqueue<T>(Expression<Action<T>> methodCall);
    }
}