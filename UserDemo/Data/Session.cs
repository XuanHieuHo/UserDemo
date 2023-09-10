
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UserDemo.Data
{
    [Table("Session")]
    public class Session
    {
        [Key]
        public Guid Id { get; set; }
        [Required]
        public int UserNameId { get; set; }
        [ForeignKey("UserNameId")]
        public NguoiDung NguoiDung { get; set; }
        [Required]
        public string RefreshToken { get; set; }
        [Required]
        public Boolean IsLocked { get; set; } = false;
        [Required]
        public DateTimeOffset CreatedAt { get; set; } = DateTime.Now;
        [Required]
        public DateTimeOffset ExpiresAt { get; set;}
    }
}
