using System.Security.Claims;
using Cart.Application.DTOs;
using Cart.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cart.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/cart")]
public class CartController : ControllerBase
{
    private readonly CartService _cartService;

    public CartController(CartService cartService)
    {
        _cartService = cartService;
    }

    [HttpGet]
    public async Task<ActionResult<CartDto>> GetCart()
    {
        return Ok(await _cartService.GetCartAsync(UserId));
    }

    [HttpPost("items")]
    public async Task<IActionResult> AddItem(AddCartItemRequest request)
    {
        var cart = await _cartService.AddItemAsync(UserId, request.ProductId, request.Quantity);
        return cart is null ? NotFound($"Product {request.ProductId} not found.") : Ok(cart);
    }

    [HttpDelete("items/{productId:int}")]
    public async Task<ActionResult<CartDto>> RemoveItem(int productId)
    {
        return Ok(await _cartService.RemoveItemAsync(UserId, productId));
    }

    [HttpDelete]
    public async Task<IActionResult> ClearCart()
    {
        await _cartService.ClearCartAsync(UserId);
        return NoContent();
    }

    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;
}
