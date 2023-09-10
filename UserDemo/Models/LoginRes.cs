namespace UserDemo.Models
{
    public class LoginRes
    {
        public Guid SessionId { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public string Email { get; set; }
       
        public string HoTen { get; set; }
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
        public DateTimeOffset ExpiresAt { get; set; }
    }
}
