using System.Threading.Tasks;
using AutoMapper;
using CompanyEmployee.Filters.ActionFilters;
using Contracts;
using Entities.DTOs;
using Entities.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace CompanyEmployee.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthenticationController : ControllerBase
    {
        private readonly IMapper _mapper;
        private readonly ILoggerManager _logger;
        private readonly UserManager<User> _userManager;
        private readonly IAuthenticationManager _authenticationManager;

        public AuthenticationController(IMapper mapper, ILoggerManager logger,
            UserManager<User> userManager, IAuthenticationManager authenticationManager)
        {
            _mapper = mapper;
            _logger = logger;
            _userManager = userManager;
            _authenticationManager = authenticationManager;
        }
        [AllowAnonymous]
        [HttpPost]
        [ServiceFilter(typeof(ValidationFilterAttribute))]
        public async Task<IActionResult> RegisterUser([FromBody] UserForRegistrationDto userModel)
        {
            var user = _mapper.Map<User>(userModel);
            var userCreationResult = await _userManager.CreateAsync(user, userModel.Password);

            if (userCreationResult.Succeeded)
            {
                await _userManager.AddToRolesAsync(user, userModel.Roles);
                return StatusCode(201);
            }

            foreach (var error in userCreationResult.Errors)
            {
                ModelState.TryAddModelError(error.Code, error.Description);
            }

            return BadRequest(ModelState);

        }

        [AllowAnonymous]
        [HttpPost("login")]
        [ServiceFilter(typeof(ValidationFilterAttribute))]
        public async Task<IActionResult> Authenticate([FromBody]UserForAuthenticationDto user)
        {
            if (await _authenticationManager.ValidateUser(user))
            {
                var token = await _authenticationManager.CreateToken();
                return Ok(new {token});
            }
            _logger.LogWarn($"{nameof(Authenticate)}: Authentication failed. Wrong user name or password.");
            return Unauthorized();
        }
    }

    
}
