using System.Net;
using AuthTenant.Application.Commands.Auth;
using AuthTenant.Application.Models.Dtos.Auth;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace AuthTenant.API.Controllers
{
    [ApiController]
    [Produces("application/json")]
    [Tags("Auth")]
    public class AuthController : BaseApiController
    {
        public AuthController(IMediator mediator) : base(mediator) { }

        [HttpPost("login")]
        [ProducesResponseType(typeof(AuthResponseDto), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(object), (int)HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(object), (int)HttpStatusCode.Unauthorized)]
        [ProducesResponseType(typeof(object), (int)HttpStatusCode.NotFound)]
        [ProducesResponseType(typeof(object), (int)HttpStatusCode.InternalServerError)]
        public async Task<ActionResult<AuthResponseDto>> Login([FromBody] LoginRequestDto request)
        {
            if (request == null)
                return BadRequest(new { error = "Request body cannot be null" });

            var command = new LoginCommand(request.Email, request.Password, request.TenantId);
            var result = await _mediator.Send(command);
            return HandleResult(result);
        }

        [HttpPost("register")]
        [ProducesResponseType(typeof(AuthResponseDto), (int)HttpStatusCode.Created)]
        [ProducesResponseType(typeof(object), (int)HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(object), (int)HttpStatusCode.NotFound)]
        [ProducesResponseType(typeof(object), (int)HttpStatusCode.Conflict)]
        [ProducesResponseType(typeof(object), (int)HttpStatusCode.InternalServerError)]
        public async Task<ActionResult<AuthResponseDto>> Register([FromBody] RegisterRequestDto request)
        {
            if (request == null)
                return BadRequest(new { error = "Request body cannot be null" });

            var command = new RegisterCommand(
                request.Email,
                request.Password,
                request.FirstName,
                request.LastName,
                request.TenantId);

            var result = await _mediator.Send(command);

            if (result.IsSuccess)
                return Created($"/api/auth/user/{result.Data?.User?.Id}", result.Data);

            return HandleResult(result);
        }

        [HttpGet("validate-token")]
        [ProducesResponseType(typeof(object), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(object), (int)HttpStatusCode.Unauthorized)]
        [ProducesResponseType(typeof(object), (int)HttpStatusCode.InternalServerError)]
        public ActionResult ValidateToken()
        {
            return Ok(new
            {
                isValid = true,
                message = "Token is valid",
                timestamp = DateTime.UtcNow
            });
        }
    }
}
