using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Order.Application.Services;

namespace Order.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/orders")]
public class OrdersController : ControllerBase
{
    private readonly OrderService _orderService;

    public OrdersController(OrderService orderService)
    {
        _orderService = orderService;
    }

    [HttpPost]
    public async Task<IActionResult> Checkout()
    {
        var order = await _orderService.CheckoutAsync(UserId, BearerToken);
        return order is null ? BadRequest("Cart is empty.") : Ok(order);
    }

    [HttpGet]
    public async Task<IActionResult> GetOrders()
    {
        return Ok(await _orderService.GetOrdersAsync(UserId));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetOrder(Guid id)
    {
        var order = await _orderService.GetOrderAsync(UserId, id);
        return order is null ? NotFound() : Ok(order);
    }

    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    private string BearerToken
    {
        get
        {
            var header = Request.Headers.Authorization.ToString();
            const string prefix = "Bearer ";
            return header.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) ? header[prefix.Length..] : header;
        }
    }
}
