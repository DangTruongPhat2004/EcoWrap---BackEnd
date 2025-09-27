using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EcoWrap.Models.Request;

namespace EcoWrap.BLL.IServices
{
    public interface IRegisterAccountService
    {
        Task<bool> RegisterAccountAsync(RegisterAccountRequest request);
    }
}
