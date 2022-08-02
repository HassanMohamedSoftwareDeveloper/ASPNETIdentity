using IdentityApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IdentityApp.Controllers;

[Authorize]
public class StoreController : Controller
{
    private readonly ProductDbContext _DbContext;
    public StoreController(ProductDbContext ctx) => _DbContext = ctx;
    public IActionResult Index() => View(_DbContext.Products);
}
