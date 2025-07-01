using ApiEcommerce.Models;
using ApiEcommerce.Models.Dtos;
using ApiEcommerce.Repository.IRepository;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace ApiEcommerce.Repository
{
    public class UserRepository : IUserRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly string? _secretKey;


        public UserRepository(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _secretKey = configuration.GetValue<string>("ApiSettings:SecretKey");
        }

        public User? GetUser(int id)
        {
            return _context.Users.FirstOrDefault(x => x.Id == id);
        }

        public ICollection<User> GetUsers()
        {
            return _context.Users.OrderBy(x => x.Username).ToList();
        }

        public bool IsUniqueUser(string username)
        {
            return !_context.Users.Any(x => x.Username.ToLower().Trim() == username.ToLower().Trim());
        }

        public async Task<UserLoginResponseDTO> Login(UserLoginDTO userLoginDTO)
        {
            if (string.IsNullOrEmpty(userLoginDTO.Username))
            {
                return new UserLoginResponseDTO()
                {
                    Token = "",
                    User = null,
                    Message = "El Username es requerido"
                };
            }
            var user = await _context.Users.FirstOrDefaultAsync<User>(u => u.Username.ToLower().Trim() == userLoginDTO.Username.ToLower().Trim());
            if (user == null)
            {
                return new UserLoginResponseDTO()
                {
                    Token = "",
                    User = null,
                    Message = "Username no encontrado"
                };
            }

            if (!BCrypt.Net.BCrypt.Verify(userLoginDTO.Password, user.Password))
            {
                return new UserLoginResponseDTO()
                {
                    Token = "",
                    User = null,
                    Message = "Las credenciales son incorrectas"
                };
            }

            //JWT
            var handlerToken = new JwtSecurityTokenHandler();
            if (string.IsNullOrWhiteSpace(_secretKey))
            {
                throw new InvalidOperationException("SecrectKey no esta configurada");
            }

            var key = Encoding.UTF8.GetBytes(_secretKey);

            var tokenDescriptor = new SecurityTokenDescriptor()
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim("id", user.Id.ToString()),
                    new Claim("user", user.Username),
                    new Claim(ClaimTypes.Role, user.Role ?? string.Empty)
                }),
                Expires = DateTime.UtcNow.AddHours(2),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = handlerToken.CreateToken(tokenDescriptor);

            return new UserLoginResponseDTO()
            {
                Token = handlerToken.WriteToken(token),
                User = new UserRegisterDTO()
                {
                    Username = user.Username,
                    Name = user.Name,
                    Role = user.Role,
                    Password = user.Password ?? ""
                },
                Message = "Usuario logueado correctamente"
            };

        }

        public async Task<User> Register(CreateUserDTO createUserDTO)
        {
            var encryptedPassword = BCrypt.Net.BCrypt.HashPassword(createUserDTO.Password);

            var user = new User()
            {
                Username = createUserDTO.Username ?? "No Username",
                Name = createUserDTO.Name,
                Role = createUserDTO.Role,
                Password = encryptedPassword
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return user;
        }
    }
}
