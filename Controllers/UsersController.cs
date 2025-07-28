using ApiEcommerce.Models.Dtos;
using ApiEcommerce.Repository.IRepository;
using Asp.Versioning;
using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApiEcommerce.Controllers
{
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    [ApiVersionNeutral]
    public class UsersController : ControllerBase
    {
        private readonly IUserRepository _userRepository;

        public UsersController(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult GetUsers()
        {
            var users = _userRepository.GetUsers();
            var userDTO = users.Adapt<List<UserDTO>>();
            return Ok(userDTO);
        }

        [HttpGet("{id}", Name = "GetUser")]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult GetUser(string id)
        {
            var user = _userRepository.GetUser(id);
            if (user == null)
            {
                return NotFound($"El usuario con el id {id} no existe");
            }
            var userDTO = user.Adapt<UserDTO>();
            return Ok(userDTO);
        }

        [AllowAnonymous]
        [HttpPost(Name = "RegisterUser")]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> RegisterUser([FromBody] CreateUserDTO createUserDTO)
        {
            if(createUserDTO is null || !ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (string.IsNullOrWhiteSpace(createUserDTO.Username))
            {
                return BadRequest("Username es requerido");
            }

            if (!_userRepository.IsUniqueUser(createUserDTO.Username))
            {
                return BadRequest("El usuario ya existe");
            }

            var result = await _userRepository.Register(createUserDTO);

            if(result is null)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Error al registrar el usuario");
            }

            return CreatedAtRoute("GetUser", new {id = result.Id}, result);
        }

        [AllowAnonymous]
        [HttpPost("Login", Name = "LoginUser")]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> RegisterUser([FromBody] UserLoginDTO userLoginDTO)
        {
            if (userLoginDTO is null || !ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var user = await _userRepository.Login(userLoginDTO);

            if (user is null)
            {
                return Unauthorized();
            }

            return Ok(user);
        }
    }
}
