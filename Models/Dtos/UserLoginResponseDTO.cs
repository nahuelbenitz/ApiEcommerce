namespace ApiEcommerce.Models.Dtos
{
    public class UserLoginResponseDTO
    {
        public UserDataDTO? User { get; set; }
        public string? Token { get; set; }
        public string? Message { get; set; }
    }
}
