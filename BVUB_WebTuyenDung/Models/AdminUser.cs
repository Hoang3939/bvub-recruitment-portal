using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BVUB_WebTuyenDung.Models
{
    public class AdminUser
    {
        [Key]
        public int AdminId { get; set; }

        [Required]
        public string Username { get; set; }
        public string PasswordHash { get; set; }
        public string Role { get; set; }
    }

}
