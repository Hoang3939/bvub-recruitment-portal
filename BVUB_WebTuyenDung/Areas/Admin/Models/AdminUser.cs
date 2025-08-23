    using System.ComponentModel.DataAnnotations;

    namespace BVUB_WebTuyenDung.Areas.Admin.Models
    {

        public class AdminUser
        {
            [Key]
            public int AdminId { get; set; }

            [Required]
            public string Username { get; set; }
            public string PasswordHash { get; set; }
            public int Role { get; set; }
            public string Email { get; set; }
        }
    }
