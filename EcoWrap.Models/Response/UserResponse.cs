namespace EcoWrap.Models.Response
{
    public class UserResponse
    {
        public int UserId { get; set; }
        public int RoleId { get; set; }
        public string Username { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string? FullName { get; set; }
        public string? PhoneNumber { get; set; }
        public bool IsVerified { get; set; }
    }
}