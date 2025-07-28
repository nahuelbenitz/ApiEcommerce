using ApiEcommerce.Models;
using ApiEcommerce.Models.Dtos;
using ApiEcommerce.Repository.IRepository;
using Microsoft.AspNetCore.Identity;
using Mapster;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace ApiEcommerce.Repository
{
    public class UserRepository : IUserRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly string _secretKey;

        public UserRepository(ApplicationDbContext context, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager, IConfiguration configuration)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
            _secretKey = configuration.GetValue<string>("ApiSettings:SecretKey") ?? throw new InvalidOperationException("SecrectKey no esta configurada");
        }

        public ApplicationUser? GetUser(string id)
        {
            return _context.ApplicationUsers.FirstOrDefault(x => x.Id == id);
        }

        public ICollection<ApplicationUser> GetUsers()
        {
            return _context.ApplicationUsers.OrderBy(x => x.UserName).ToList();
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
            var user = _context.ApplicationUsers.FirstOrDefault(u => u.UserName != null && u.UserName.ToLower().Trim() == userLoginDTO.Username.ToLower().Trim());
            if (user is null)
            {
                return new UserLoginResponseDTO()
                {
                    Token = "",
                    User = null,
                    Message = "Username no encontrado"
                };
            }

            if (userLoginDTO.Password is null)
            {
                return new UserLoginResponseDTO()
                {
                    Token = "",
                    User = null,
                    Message = "Password requerido"
                };
            }

            bool isValid = await _userManager.CheckPasswordAsync(user, userLoginDTO.Password);

            if (!isValid)
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

            var roles = await _userManager.GetRolesAsync(user);

            var key = Encoding.UTF8.GetBytes(_secretKey);

            var tokenDescriptor = new SecurityTokenDescriptor()
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim("id", user.Id.ToString()),
                    new Claim("user", user.UserName ?? string.Empty),
                    new Claim(ClaimTypes.Role, roles.FirstOrDefault() ?? string.Empty)
                }),
                Expires = DateTime.UtcNow.AddHours(2),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = handlerToken.CreateToken(tokenDescriptor);

            return new UserLoginResponseDTO()
            {
                Token = handlerToken.WriteToken(token),
                User = user.Adapt<UserDataDTO>(),
                Message = "Usuario logueado correctamente"
            };
        }

        public async Task<UserDataDTO> Register(CreateUserDTO createUserDTO)
        {
            var user = new ApplicationUser
            {
                UserName = createUserDTO.Username,
                Name = createUserDTO.Name
            };

            var result = await _userManager.CreateAsync(user, createUserDTO.Password);

            if (result.Succeeded)
            {
                var userRole = createUserDTO.Role ?? "User";

                //Pregunto si existe el rol, en caso contrario lo creo
                var roleExist = await _roleManager.RoleExistsAsync(userRole);
                if (!roleExist)
                {
                    var identityRole = new IdentityRole(userRole);
                    await _roleManager.CreateAsync(identityRole);
                }
                await _userManager.AddToRoleAsync(user, userRole);
                var createdUser = _context.ApplicationUsers.FirstOrDefault(u => u.UserName == createUserDTO.Username);
                return createdUser.Adapt<UserDataDTO>();
            }

            throw new ApplicationException("No se pudo crear el registro");
        }
    }
}
