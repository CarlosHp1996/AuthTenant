using System.Net;
using AuthTenant.Application.Commands.Product;
using AuthTenant.Application.Common;
using AuthTenant.Application.Models.Dtos.Product;
using AuthTenant.Application.Queries.Product;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthTenant.API.Controllers
{
    [ApiController]
    [Authorize]
    [Produces("application/json")]
    [Tags("Products")]
    public class ProductsController : BaseApiController
    {
        public ProductsController(IMediator mediator) : base(mediator) { }

        [HttpGet]
        [ProducesResponseType(typeof(PagedResult<ProductDto>), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(object), (int)HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(object), (int)HttpStatusCode.Unauthorized)]
        [ProducesResponseType(typeof(object), (int)HttpStatusCode.Forbidden)]
        [ProducesResponseType(typeof(object), (int)HttpStatusCode.InternalServerError)]
        public async Task<ActionResult<PagedResult<ProductDto>>> GetProducts(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? searchTerm = null,
            [FromQuery] bool? isActive = null)
        {
            if (page < 1)
                return BadRequest(new { error = "Page number must be greater than 0" });

            if (pageSize < 1 || pageSize > 100)
                return BadRequest(new { error = "Page size must be between 1 and 100" });

            var query = new GetProductsQuery(page, pageSize, searchTerm, isActive);
            var result = await _mediator.Send(query);
            return HandleResult(result);
        }

        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(ProductDto), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(object), (int)HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(object), (int)HttpStatusCode.Unauthorized)]
        [ProducesResponseType(typeof(object), (int)HttpStatusCode.Forbidden)]
        [ProducesResponseType(typeof(object), (int)HttpStatusCode.NotFound)]
        [ProducesResponseType(typeof(object), (int)HttpStatusCode.InternalServerError)]
        public async Task<ActionResult<ProductDto>> GetProduct(Guid id)
        {
            if (id == Guid.Empty)
                return BadRequest(new { error = "Product ID cannot be empty" });

            var query = new GetProductByIdQuery(id);
            var result = await _mediator.Send(query);
            return HandleResult(result);
        }

        [HttpPost]
        [ProducesResponseType(typeof(ProductDto), (int)HttpStatusCode.Created)]
        [ProducesResponseType(typeof(object), (int)HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(object), (int)HttpStatusCode.Unauthorized)]
        [ProducesResponseType(typeof(object), (int)HttpStatusCode.Forbidden)]
        [ProducesResponseType(typeof(object), (int)HttpStatusCode.Conflict)]
        [ProducesResponseType(typeof(object), (int)HttpStatusCode.InternalServerError)]
        public async Task<ActionResult<ProductDto>> CreateProduct([FromBody] CreateProductDto request)
        {
            if (request == null)
                return BadRequest(new { error = "Request body cannot be null" });

            var command = new CreateProductCommand(
                request.Name,
                request.Description,
                request.Price,
                request.SKU,
                request.StockQuantity);

            var result = await _mediator.Send(command);

            if (result.IsSuccess)
                return Created($"/api/products/{result.Data?.Id}", result.Data);

            return HandleResult(result);
        }

        [HttpPut("{id:guid}")]
        [ProducesResponseType(typeof(ProductDto), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(object), (int)HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(object), (int)HttpStatusCode.Unauthorized)]
        [ProducesResponseType(typeof(object), (int)HttpStatusCode.Forbidden)]
        [ProducesResponseType(typeof(object), (int)HttpStatusCode.NotFound)]
        [ProducesResponseType(typeof(object), (int)HttpStatusCode.Conflict)]
        [ProducesResponseType(typeof(object), (int)HttpStatusCode.InternalServerError)]
        public async Task<ActionResult<ProductDto>> UpdateProduct(Guid id, [FromBody] UpdateProductDto request)
        {
            if (id == Guid.Empty)
                return BadRequest(new { error = "Product ID cannot be empty" });

            if (request == null)
                return BadRequest(new { error = "Request body cannot be null" });

            var command = new UpdateProductCommand(
                id,
                request.Name,
                request.Description,
                request.Price,
                request.SKU,
                request.StockQuantity,
                request.IsActive);

            var result = await _mediator.Send(command);
            return HandleResult(result);
        }

        [HttpDelete("{id:guid}")]
        [ProducesResponseType(typeof(object), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(object), (int)HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(object), (int)HttpStatusCode.Unauthorized)]
        [ProducesResponseType(typeof(object), (int)HttpStatusCode.Forbidden)]
        [ProducesResponseType(typeof(object), (int)HttpStatusCode.NotFound)]
        [ProducesResponseType(typeof(object), (int)HttpStatusCode.InternalServerError)]
        public async Task<ActionResult> DeleteProduct(Guid id)
        {
            if (id == Guid.Empty)
                return BadRequest(new { error = "Product ID cannot be empty" });

            var command = new DeleteProductCommand(id);
            var result = await _mediator.Send(command);

            if (result.IsSuccess)
                return Ok(new { message = "Product successfully deleted", productId = id });

            return HandleResultAsActionResult(result);
        }

        [HttpGet("active")]
        [ProducesResponseType(typeof(PagedResult<ProductDto>), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(object), (int)HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(object), (int)HttpStatusCode.Unauthorized)]
        [ProducesResponseType(typeof(object), (int)HttpStatusCode.Forbidden)]
        [ProducesResponseType(typeof(object), (int)HttpStatusCode.InternalServerError)]
        public async Task<ActionResult<PagedResult<ProductDto>>> GetActiveProducts(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            return await GetProducts(page, pageSize, null, true);
        }
    }
}
