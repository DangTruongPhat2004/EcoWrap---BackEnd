using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EcoWrap.BLL.IServices
{
    public interface IForgotPasswordService
    {
        Task<bool> SendResetCodeAsync(string email);
        Task<bool> ResetPasswordAsync(string email, string otp, string newPassword);
    }
}