using System.ComponentModel.DataAnnotations;

namespace UserDemo.Models
{
    public class RegisterModel
    {
        [Required]
        [MaxLength(50)]
        public string UserName { get; set; }
        [Required]
        [MaxLength(250)]
        public string Password { get; set; }
        [Required]
        [MaxLength(250)]
        public string HoTen { get; set; }
        [Required]
        [MaxLength(250)]
        public string Email { get; set; }
    }
}
