namespace UserDemo.Models
{
    public class Payload
    {
        public Guid Id { get; set; }
        public string UserNameId { get; set; }
        public DateTimeOffset IssuedAt { get; set; }
        public DateTimeOffset ExpiredAt { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public string HoTen { get; set; }
    }
}
