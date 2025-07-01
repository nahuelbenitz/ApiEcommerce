namespace ApiEcommerce.Models.Dtos
{
    public class UserRegisterDTO
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
        public required string Username { get; set; }
        public required string Password { get; set; }
        public string? Role { get; set; }
    }
}
