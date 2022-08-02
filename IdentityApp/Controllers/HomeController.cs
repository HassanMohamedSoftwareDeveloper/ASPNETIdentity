using IdentityApp.Models;
using Microsoft.AspNetCore.Mvc;

namespace IdentityApp.Controllers;

public class HomeController : Controller
{
    private readonly ProductDbContext _dbContext;
    public HomeController(ProductDbContext ctx) => _dbContext = ctx;
    public IActionResult Index() => View(_dbContext.Products);
}
