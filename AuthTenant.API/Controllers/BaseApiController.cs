using System.Net;
using AuthTenant.Application.Common;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace AuthTenant.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(object), (int)HttpStatusCode.BadRequest)]
    [ProducesResponseType(typeof(object), (int)HttpStatusCode.InternalServerError)]
    public abstract class BaseApiController : ControllerBase
    {
        protected readonly IMediator _mediator;
        protected BaseApiController(IMediator mediator)
        {
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        }

        protected ActionResult<T> HandleResult<T>(Result<T> result)
        {
            if (result == null)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError,
                    new { error = "Result cannot be null", timestamp = DateTime.UtcNow });
            }

            if (result.IsSuccess && result.Data != null)
                return Ok(result.Data);

            if (result.IsSuccess && result.Data == null)
            {
                return NotFound(new
                {
                    error = "Resource not found",
                    timestamp = DateTime.UtcNow
                });
            }

            if (result.Errors?.Any() == true)
            {
                return BadRequest(new
                {
                    errors = result.Errors,
                    message = "Validation failed",
                    timestamp = DateTime.UtcNow
                });
            }

            if (!string.IsNullOrEmpty(result.Error))
            {
                var statusCode = DetermineStatusCode(result.Error);

                return StatusCode(statusCode, new
                {
                    error = result.Error,
                    timestamp = DateTime.UtcNow
                });
            }

            return StatusCode((int)HttpStatusCode.InternalServerError,
                new
                {
                    error = "An unexpected error occurred",
                    timestamp = DateTime.UtcNow
                });
        }

        protected ActionResult HandleResultAsActionResult<T>(Result<T> result)
        {
            if (result == null)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError,
                    new { error = "Result cannot be null", timestamp = DateTime.UtcNow });
            }

            if (result.IsSuccess && result.Data != null)
                return Ok(result.Data);

            if (result.IsSuccess && result.Data == null)
            {
                return NotFound(new
                {
                    error = "Resource not found",
                    timestamp = DateTime.UtcNow
                });
            }

            if (result.Errors?.Any() == true)
            {
                return BadRequest(new
                {
                    errors = result.Errors,
                    message = "Validation failed",
                    timestamp = DateTime.UtcNow
                });
            }

            if (!string.IsNullOrEmpty(result.Error))
            {
                var statusCode = DetermineStatusCode(result.Error);

                return StatusCode(statusCode, new
                {
                    error = result.Error,
                    timestamp = DateTime.UtcNow
                });
            }

            return StatusCode((int)HttpStatusCode.InternalServerError,
                new
                {
                    error = "An unexpected error occurred",
                    timestamp = DateTime.UtcNow
                });
        }

        private static int DetermineStatusCode(string error)
        {
            if (string.IsNullOrEmpty(error))
                return (int)HttpStatusCode.InternalServerError;

            var errorLower = error.ToLowerInvariant();

            if (errorLower.Contains("not found") ||
                errorLower.Contains("does not exist") ||
                errorLower.Contains("não encontrado"))
            {
                return (int)HttpStatusCode.NotFound;
            }

            if (errorLower.Contains("already exists") ||
                errorLower.Contains("duplicate") ||
                errorLower.Contains("conflict") ||
                errorLower.Contains("já existe"))
            {
                return (int)HttpStatusCode.Conflict;
            }

            if (errorLower.Contains("forbidden") ||
                errorLower.Contains("access denied") ||
                errorLower.Contains("permission") ||
                errorLower.Contains("unauthorized"))
            {
                return (int)HttpStatusCode.Forbidden;
            }

            if (errorLower.Contains("invalid") ||
                errorLower.Contains("validation") ||
                errorLower.Contains("required") ||
                errorLower.Contains("format") ||
                errorLower.Contains("inválido"))
            {
                return (int)HttpStatusCode.BadRequest;
            }

            return (int)HttpStatusCode.BadRequest;
        }

        protected ActionResult ValidationError(string message, string? field = null)
        {
            var error = new
            {
                message,
                field,
                timestamp = DateTime.UtcNow
            };

            return BadRequest(error);
        }

        protected ActionResult SuccessResponse(string message, object? data = null)
        {
            var response = new
            {
                message,
                data,
                timestamp = DateTime.UtcNow
            };

            return Ok(response);
        }
    }
}
