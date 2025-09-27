using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EcoWrap.Models.Request; 

namespace EcoWrap.BLL.IServices
{
    public interface IAuthenticationService
    {
        Task<string?> LoginAsync(LoginRequest request);
    }
}