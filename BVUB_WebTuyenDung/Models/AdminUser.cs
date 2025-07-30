namespace BVUB_WebTuyenDung.Models
{
    public class AdminUser
    {
        public int AdminId { get; set; }
        public string Username { get; set; }
        public string PasswordHash { get; set; }
        public string Role { get; set; }
    }

}
