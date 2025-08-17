using System.ComponentModel.DataAnnotations;

namespace BVUB_WebTuyenDung.Models
{
    public class AdminUser
    {
        [Key]
        public int AdminId { get; set; }
        public string Username { get; set; }
        public string PasswordHash { get; set; }
        public string Role { get; set; }

        [Required, EmailAddress, StringLength(255)]
        public string Email { get; set; }
    }

}
