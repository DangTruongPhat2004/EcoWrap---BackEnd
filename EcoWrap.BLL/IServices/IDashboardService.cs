using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EcoWrap.Models.Response;

namespace EcoWrap.BLL.IServices
{
    public interface IDashboardService
    {
        Task<ApiResponse<DashboardResponse>> GetDashboardMetricsAsync();
    }
}