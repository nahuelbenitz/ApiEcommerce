using ApiEcommerce.Models;
using ApiEcommerce.Models.Dtos;
using ApiEcommerce.Repository.IRepository;
using AutoMapper;
using Microsoft.AspNetCore.Identity;
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
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IMapper _mapper;

        public UserRepository(ApplicationDbContext context, IConfiguration configuration, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager, IMapper mapper)
        {
            _context = context;
            _secretKey = configuration.GetValue<string>("ApiSettings:SecretKey");
            _userManager = userManager;
            _roleManager = roleManager;
            _mapper = mapper;
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
            var user = await _context.ApplicationUsers.FirstOrDefaultAsync<ApplicationUser>(u => u.UserName != null && u.UserName.ToLower().Trim() == userLoginDTO.Username.ToLower().Trim());
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
            if (string.IsNullOrWhiteSpace(_secretKey))
            {
                throw new InvalidOperationException("SecrectKey no esta configurada");
            }

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
                User = _mapper.Map<UserDataDTO>(user),
                Message = "Usuario logueado correctamente"
            };

        }

        public async Task<UserDataDTO> Register(CreateUserDTO createUserDTO)
        {
            if (string.IsNullOrEmpty(createUserDTO.Username))
            {
                throw new ArgumentNullException("El username es requerido");
            }

            if (createUserDTO.Password is null)
            {
                throw new ArgumentNullException("El password es requerido");
            }

            var user = new ApplicationUser()
            {
                UserName = createUserDTO.Username,
                Email = createUserDTO.Username,
                NormalizedEmail = createUserDTO.Username.ToUpper(),
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
                return _mapper.Map<UserDataDTO>(createdUser);
            }

            throw new ApplicationException("No se pudo crear el registro");
        }
    }
}
