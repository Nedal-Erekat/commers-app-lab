using Catalog.Application.DTOs;
using Catalog.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace Catalog.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly ProductService _productService;

    public ProductsController(ProductService productService)
    {
        _productService = productService;
    }

    [HttpGet]
    public async Task<IActionResult> GetProducts(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] string? category = null)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        Response.Headers["X-Served-By"] = Environment.MachineName;

        var result = await _productService.GetProductsAsync(page, pageSize, category);

        return Ok(new
        {
            source = result.Source,
            data = result.Data,
            page = result.Page,
            pageSize = result.PageSize,
            totalCount = result.TotalCount,
            totalPages = result.TotalPages
        });
    }

    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string q)
    {
        if (string.IsNullOrWhiteSpace(q))
            return BadRequest("Query parameter 'q' is required.");

        Response.Headers["X-Served-By"] = Environment.MachineName;

        var results = await _productService.SearchProductsAsync(q);
        return Ok(results);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        Response.Headers["X-Served-By"] = Environment.MachineName;

        var product = await _productService.GetProductAsync(id);
        return product is null ? NotFound() : Ok(product);
    }

    [HttpPost("{id:int}/decrement-stock")]
    public async Task<IActionResult> DecrementStock(int id, DecrementStockRequest request)
    {
        var success = await _productService.DecrementStockAsync(id, request.Quantity);
        return success ? NoContent() : BadRequest("Insufficient stock or product not found.");
    }
}
