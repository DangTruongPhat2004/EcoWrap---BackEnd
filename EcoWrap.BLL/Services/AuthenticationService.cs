using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using EcoWrap.BLL.IServices;
using EcoWrap.DAL.Entities;
using EcoWrap.DAL.IRepositories;
using EcoWrap.Models.Request;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace EcoWrap.BLL.Services
{
    public class AuthenticationService : IAuthenticationService
    {
        private readonly IAuthenticationRepository _authRepository;
        private readonly IConfiguration _config;
        private readonly IPasswordHasher<User> _passwordHasher;

        public AuthenticationService(
            IAuthenticationRepository authRepository,
            IConfiguration config,
            IPasswordHasher<User> passwordHasher)
        {
            _authRepository = authRepository;
            _config = config;
            _passwordHasher = passwordHasher;
        }

        public async Task<string?> LoginAsync(LoginRequest request)
        {
            // 1. Tìm user theo Username hoặc Email
            var user = await _authRepository.GetByUsernameOrEmailAsync(request.Username);
            if (user == null)
                return null;

            // 2. Check nếu email chưa verify thì không cho login
            if (!user.IsVerified)
                throw new UnauthorizedAccessException("Email chưa được xác thực. Vui lòng xác nhận email trước khi đăng nhập.");

            // 3. So sánh password hash bằng Identity PasswordHasher
            var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
            if (result == PasswordVerificationResult.Failed)
                return null;

            // 4. Tạo JWT token
            var claims = new[]
            {
                new Claim("id", user.UserId.ToString()),
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.Email)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddHours(2),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}