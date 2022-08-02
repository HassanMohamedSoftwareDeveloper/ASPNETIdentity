# ASP.NET Core Identity
> ASP.NET Core Identity is the user management system for ASP.NET Core applications. It provides an API
for managing users and roles and for signing users into and out of applications. Users can sign in with
simple passwords, use two-factor authentication, or sign in using third-party platforms provided by Google,
Facebook, and Twitter.

## Creating Application with Identity
```cli
 dotnet new globaljson --output ASPNETIdentity/IdentityTodo
 dotnet new webapp --auth Individual --use-local-db true --output ASPNETIdentity/IdentityTodo
 dotnet new sln -o ASPNETIdentity
 dotnet sln ASPNETIdentity add ASPNETIdentity/IdentityTodo
```

> Navigate to the project folder and build the project
```cli
 cd ASPNETIdentity/IdentityTodo
 dotnet build
```

## Preparing the Project
> Open the project using (Visual Studio or Visual Studio Code) and change the contents of the launchSettings.json file in the Properties folder
```json
{
  "iisSettings": {
    "windowsAuthentication": false,
    "anonymousAuthentication": true,
    "iisExpress": {
      "applicationUrl": "http://localhost:7458",
      "sslPort": 44300
    }
  },
  "profiles": {
    "IdentityTodo": {
      "commandName": "Project",
      "dotnetRunMessages": true,
      "launchBrowser": true,
      "applicationUrl": "https://localhost:7206;http://localhost:5285",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    },
    "IIS Express": {
      "commandName": "IISExpress",
      "launchBrowser": true,
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    }
  }
}

```

## Creating the Data Model
> Add a class <code>TodoItem</code> in the Data Folder
```C#
namespace IdentityTodo.Data;

public class TodoItem
{
    public long Id { get; set; }
    public string Task { get; set; }
    public bool Complete { get; set; }
    public string Owner { get; set; }
}
```
>This class will be used to represent to-do items, which will be stored in a database using Entity
Framework Core. 
Entity Framework Core is also used to store Identity data, and the project template has
created a database context class.

> Adding a Property in the ApplicationDbContext.cs File in the Data Folder.
```C#
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace IdentityTodo.Data;

public class ApplicationDbContext : IdentityDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }
    public DbSet<TodoItem> TodoItems { get; set; }
}
```

## Creating and Applying the Database Migrations
> The example application requires two databases: one to store the to-do items and one for the Identity
user accounts.
1. Installing the Entity Framework Core Tools Package
```cli
dotnet tool uninstall --global dotnet-ef
dotnet tool install --global dotnet-ef
```
2. Creating the Database Migrations
```cli
dotnet ef migrations add AddTodos
```
3. Creating the Database
```cli
dotnet ef database drop --force
dotnet ef database update
```

## Configuring ASP.NET Core Identity
> A configuration change is required to prepare ASP.NET Core Identity.

> Configuring the Application in the <code>Program.cs</code> File in the IdentityTodo Folder
```C#
using IdentityTodo.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = false)
    .AddEntityFrameworkStores<ApplicationDbContext>();
builder.Services.AddRazorPages();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();

app.Run();

```

## Creating the Application Content
> To present the user with their list of to-do items, replace the contents of the <code>Index.cshtml</code> file in the Pages
folder.
```Razor
@page
@model IndexModel
@{
    ViewData["Title"] = "To Do List";
}
<h2 class="text-center">To Do List</h2>
<h4 class="text-center">(@User.Identity.Name)</h4>
<form method="post" asp-page-handler="ShowComplete" class="m-2">
    <div class="form-check">
        <input type="checkbox" class="form-check-input" asp-for="ShowComplete"
               onchange="this.form.submit()" />
        <label class="form-check-label">Show Completed Items</label>
    </div>
</form>
<table class="table table-sm table-striped table-bordered m-2">
    <thead><tr><th>Task</th><th /></tr></thead>
    <tbody>
        @if (Model.TodoItems.Count() == 0)
        {
            <tr>
                <td colspan="2" class="text-center py-4">
                    You have done everything!
                </td>
            </tr>
        }
        else
        {
            @foreach (TodoItem item in Model.TodoItems)
            {
                <tr>
                    <td class="p-2">@item.Task</td>
                    <td class="text-center py-2">
                        <form method="post" asp-page-handler="MarkItem">
                            <input type="hidden" name="id" value="@item.Id" />
                            <input type="hidden" asp-for="ShowComplete" />
                            <button type="submit" class="btn btn-sm btn-secondary">
                                @(item.Complete ? "Mark Not Done" : "Done")
                            </button>
                        </form>
                    </td>
                </tr>
            }
        }
    </tbody>
    <tfoot>
        <tr>
            <td class="pt-4">
                <form method="post" asp-page-handler="AddItem" id="addItem">
                    <input type="hidden" asp-for="ShowComplete" />
                    <input name="task" placeholder="Enter new to do"
                           class="form-control" />
                </form>
            </td>
            <td class="text-center pt-4">
                <button type="submit" form="addItem"
                        class="btn btn-sm btn-secondary">
                    Add
                </button>
            </td>
        </tr>
    </tfoot>
</table>
```
> This content presents the user with a table containing their to-do list, along with the ability to add items
to the list, mark items as done, and include completed items in the table.

> To define the feature that support the content, replace the contents of the <code>Index.cshtml.cs</code> file in the Pages folder with the code.
```C#
using IdentityTodo.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace IdentityTodo.Pages;

[Authorize]
public class IndexModel : PageModel
{
    private ApplicationDbContext Context;
    public IndexModel(ApplicationDbContext ctx)
    {
        Context = ctx;
    }
    [BindProperty(SupportsGet = true)]
    public bool ShowComplete { get; set; }
    public IEnumerable<TodoItem> TodoItems { get; set; }
    public void OnGet()
    {
        TodoItems = Context.TodoItems
        .Where(t => t.Owner == User.Identity.Name).OrderBy(t => t.Task);
        if (!ShowComplete)
        {
            TodoItems = TodoItems.Where(t => !t.Complete);
        }
        TodoItems = TodoItems.ToList();
    }
    public IActionResult OnPostShowComplete()
    {
        return RedirectToPage(new { ShowComplete });
    }
    public async Task<IActionResult> OnPostAddItemAsync(string task)
    {
        if (!string.IsNullOrEmpty(task))
        {
            TodoItem item = new TodoItem
            {
                Task = task,
                Owner = User.Identity.Name,
                Complete = false
            };
            await Context.AddAsync(item);
            await Context.SaveChangesAsync();
        }
        return RedirectToPage(new { ShowComplete });
    }
    public async Task<IActionResult> OnPostMarkItemAsync(long id)
    {
        TodoItem item = Context.TodoItems.Find(id);
        if (item != null)
        {
            item.Complete = !item.Complete;
            await Context.SaveChangesAsync();
        }
        return RedirectToPage(new { ShowComplete });
    }
}
```

## Running the Example Application
```cli
 dotnet run
```

## Recap what we did till now
<code>
We described the tools required for ASP.NET Core and ASP.NET Core Identity development. 
We created an application using a project template that includes Identity and demonstrated the features that
Identity can offer with minimal configuration.
</code>

---

# What we will do now
> Will create more complex project and use it to start exploring the <code>ASP.NET Core</code> identity features in detail.

## Creating the Project
```cli
cd ASPNETIdentity
dotnet new web --no-https --output IdentityApp
dotnet sln add IdentityApp
```

### Installing the Bootstrap CSS Framework
```cli
cd IdentityApp
dotnet tool uninstall --global Microsoft.Web.LibraryManager.Cli
dotnet tool install --global Microsoft.Web.LibraryManager.Cli
libman init -p cdnjs
libman install twitter-bootstrap@4.5.0 -d wwwroot/lib/twitter-bootstrap
```

### Install Entity Framework Core
```cli
dotnet add package Microsoft.EntityFrameworkCore.Design
dotnet add package Microsoft.EntityFrameworkCore.SqlServer
```

### Defining a Connection String
> Defining a Connection String in the <code>appsettings.json</code> File in the IdentityApp Folder
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "ConnectionStrings": {
    "AppDataConnection": "Server=(localdb)\\MSSQLLocalDB;Database=IdentityAppData;MultipleActiveResultSets=true"
  }
}

```

### Creating the Data Model
> Create the <code>Models</code> folder and add to it a class file named <code>Product.cs</code>.
```C#
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.ComponentModel.DataAnnotations.Schema;

namespace IdentityApp.Models;

public class Product
{
    public long Id { get; set; }
    public string Name { get; set; }
    [Column(TypeName = "decimal(8, 2)")]
    public decimal Price { get; set; }
    public string Category { get; set; }
}
```

> In the <code>Models</code> folder add <code>ProductDbContext.cs</code> class.
```C#
using Microsoft.EntityFrameworkCore;

namespace IdentityApp.Models;

public class ProductDbContext : DbContext
{
    public ProductDbContext(DbContextOptions<ProductDbContext> options)
        : base(options) { }
    public DbSet<Product> Products { get; set; }
    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.Entity<Product>().HasData(
        new Product
        {
            Id = 1,
            Name = "Kayak",
            Category = "Watersports",
            Price = 275
        },
        new Product
        {
            Id = 2,
            Name = "Lifejacket",
            Category = "Watersports",
            Price = 48.95m
        },
        new Product
        {
            Id = 3,
            Name = "Soccer Ball",
            Category = "Soccer",
            Price = 19.50m
        },
        new Product
        {
            Id = 4,
            Name = "Corner Flags",
            Category = "Soccer",
            Price = 34.95m
        },
        new Product
        {
            Id = 5,
            Name = "Stadium",
            Category = "Soccer",
            Price = 79500
        },
        new Product
        {
            Id = 6,
            Name = "Thinking Cap",
            Category = "Chess",
            Price = 16
        },
        new Product
        {
            Id = 7,
            Name = "Unsteady Chair",
            Category = "Chess",
            Price = 29.95m
        },
        new Product
        {
            Id = 8,
            Name = "Human Chess Board",
            Category = "Chess",
            Price = 75
        },
        new Product
        {
            Id = 9,
            Name = "Bling-Bling King",
            Category = "Chess",
            Price = 1200
        });
    }
}

```

### Creating MVC Controllers and Views
> Create folder <code>Controllers</code> and add to it class named <code>HomeController.cs</code>.

> This controller will present the first level of access, which will be available to anyone.
```C#
using IdentityApp.Models;
using Microsoft.AspNetCore.Mvc;

namespace IdentityApp.Controllers;

public class HomeController : Controller
{
    private readonly ProductDbContext _dbContext;
    public HomeController(ProductDbContext ctx) => _dbContext = ctx;
    public IActionResult Index() => View(_dbContext.Products);
}
```

> Create another folder <code>Views</code> and create <code>Home</code> folder on it.
> Add to <code>Home</code> folder view named <code>Index.cshtml</code>.
```Razor
@model IQueryable<Product>
<h4 class="bg-primary text-white text-center p-2">MVC - Level 1 - Anyone</h4>
<div class="text-center">
<h6 class="p-2">
The store contains @Model.Count() products.
</h6>
</div>
```

> Add a class file named <code>StoreController.cs</code> in the <code>Controllers</code> folder.

> This controller will present the second level of access, which is available to users who are signed
into the application.
```C#
using IdentityApp.Models;
using Microsoft.AspNetCore.Mvc;

namespace IdentityApp.Controllers;

public class StoreController : Controller
{
    private readonly ProductDbContext _DbContext;
    public StoreController(ProductDbContext ctx) => _DbContext = ctx;
    public IActionResult Index() => View(_DbContext.Products);
}
```

> In <code>Views</code> folder create <code>Store</code> folder on it.
> Add to <code>Store</code> folder view named <code>Index.cshtml</code>.
```Razor
@model IQueryable<Product>
<h4 class="bg-primary text-white text-center p-2">MVC - Level 2 - Signed In Users</h4>
<div class="p-2">
    <table class="table table-sm table-striped table-bordered">
        <thead>
            <tr>
                <th>ID</th>
                <th>Name</th>
                <th>Category</th>
                <th class="text-right">Price</th>
            </tr>
        </thead>
        <tbody>
            @foreach (Product p in Model.OrderBy(p => p.Id))
            {
                <tr>
                    <td>@p.Id</td>
                    <td>@p.Name</td>
                    <td>@p.Category</td>
                    <td class="text-right">$@p.Price.ToString("F2")</td>
                </tr>
            }
        </tbody>
    </table>
</div>
```

> Add a class file named <code>AdminController.cs</code> to the <code>Controllers</code> folder.

>This controller will present the third level of content, which will be available only to
administrators.
```C#
using IdentityApp.Models;
using Microsoft.AspNetCore.Mvc;

namespace IdentityApp.Controllers;

public class AdminController : Controller
{
    private ProductDbContext DbContext;
    public AdminController(ProductDbContext ctx) => DbContext = ctx;
    public IActionResult Index() => View(DbContext.Products);
    [HttpGet]
    public IActionResult Create() => View("Edit", new Product());
    [HttpGet]
    public IActionResult Edit(long id)
    {
        Product p = DbContext.Find<Product>(id);
        if (p != null)
        {
            return View("Edit", p);
        }
        return RedirectToAction(nameof(Index));
    }
    [HttpPost]
    public IActionResult Save(Product p)
    {
        DbContext.Update(p);
        DbContext.SaveChanges();
        return RedirectToAction(nameof(Index));
    }
    [HttpPost]
    public IActionResult Delete(long id)
    {
        Product p = DbContext.Find<Product>(id);
        if (p != null)
        {
            DbContext.Remove(p);
            DbContext.SaveChanges();
        }
        return RedirectToAction(nameof(Index));
    }
}
```

> In <code>Views</code> folder create <code>Admin</code> folder on it.
> Add to <code>Admin</code> folder view named <code>Index.cshtml</code>.
```Razor
@model IQueryable<Product>
<h4 class="bg-primary text-white text-center p-2">MVC Level 3 - Administrators</h4>
<div class="p-2">
    <table class="table table-sm table-striped table-bordered">
        <thead>
            <tr>
                <th>ID</th>
                <th>Name</th>
                <th>Category</th>
                <th class="text-right">Price</th>
                <th></th>
            </tr>
        </thead>
        <tbody>
            @foreach (Product p in Model.OrderBy(p => p.Id))
            {
                <tr>
                    <td>@p.Id</td>
                    <td>@p.Name</td>
                    <td>@p.Category</td>
                    <td class="text-right">$@p.Price.ToString("F2")</td>
                    <td class="text-center">
                        <form method="post">
                            <a class="btn btn-sm btn-warning" asp-action="edit"
                           asp-route-id="@p.Id">Edit</a>
                            <button class="btn btn-sm btn-danger"
                                asp-action="delete" asp-route-id="@p.Id">
                                Delete
                            </button>
                        </form>
                    </td>
                </tr>
            }
        </tbody>
    </table>
</div>
<a class="btn btn-primary mx-2" asp-action="Create">Create</a>
```

> Add to <code>Admin</code> folder view named <code>Edit.cshtml</code>.
```Razor
@model Product
<h4 class="bg-primary text-white text-center p-2">MVC Level 3 - Administrators</h4>
<form method="post" asp-action="save" class="p-2">
    <div class="form-group">
        <label>ID</label>
        <input class="form-control" readonly asp-for="Id" />
    </div>
    <div class="form-group">
        <label>Name</label>
        <input class="form-control" asp-for="Name" />
    </div>
    <div class="form-group">
        <label>Category</label>
        <input class="form-control" asp-for="Category" />
    </div>
    <div class="form-group">
        <label>Price</label>
        <input class="form-control" type="number" asp-for="Price" />
    </div>
    <div class="text-center">
        <button type="submit" class="btn btn-primary">Save</button>
        <a class="btn btn-secondary" asp-action="Index">Cancel</a>
    </div>
</form>
```

> To enable <code>tag helpers</code> and import the data model namespace and some useful ASP.NET Core Identity
namespaces, add a Razor View Imports file named <code>_ViewImports.cshtml</code> file in the <code>Views</code> folder.
```Razor
@addTagHelper *, Microsoft.AspNetCore.Mvc.TagHelpers
@using IdentityApp.Models
@using Microsoft.AspNetCore.Identity
@using System.Security.Claims
```
> To automatically specify a <code>layout</code> for the views in the application, add a Razor View Start file named
<code>_ViewStart.cshtml</code> to the <code>Views</code> folder.
```Razor
@{
    Layout = "_Layout";
}
```
> In <code>Views</code> folder create <code>Shared</code> folder on it.
> Add to <code>Shared</code> folder view named <code>_Layout.cshtml</code>.

>his file provides the HTML structure into which views (and Razor Pages)
will render their content, including a link for the CSS stylesheet from the Bootstrap package.
```Razor
<!DOCTYPE html>
<html>
<head>
    <meta name="viewport" content="width=device-width" />
    <title>Identity App</title>
    <link href="/lib/twitter-bootstrap/css/bootstrap.min.css" rel="stylesheet" />
</head>
<body>
    <partial name="_NavigationPartial" />
    @RenderBody()
</body>
</html>
```

> Add to <code>Shared</code> folder view named <code>_NavigationPartial.cshtml</code>.
```Razor
<div class="text-center m-2">
    <a class="btn btn-secondary btn-sm" asp-controller="Home">Level 1</a>
    <a class="btn btn-secondary btn-sm" asp-controller="Store">Level 2</a>
    <a class="btn btn-secondary btn-sm" asp-controller="Admin">Level 3</a>
</div>
```

### Creating Razor Pages
> Create folder <code>Pages</code> and add razor page <code>Landing.cshtml</code> on it.

> This page will present the first level of access, which is available to anyone.
```Razor
@page "/pages"
@model IdentityApp.Pages.LandingModel
<h4 class="bg-info text-white text-center p-2">Pages - Level 1 - Anyone</h4>
<div class="text-center">
    <h6 class="p-2">
        The store contains @Model.DbContext.Products.Count() products.
    </h6>
</div>
```

> To define the page model class, add the code <code>Landing.cshtml.cs</code> file in the <code>Pages</code> folder.
```C#
using IdentityApp.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace IdentityApp.Pages;

public class LandingModel : PageModel
{
    public LandingModel(ProductDbContext ctx) => DbContext = ctx;
    public ProductDbContext DbContext { get; set; }
}
```

> Add a Razor Page named <code>Store.cshtml</code> to the <code>Pages</code> folder.

> This page will be available to signed-in users.
```Razor
@page "/pages/store"
@model IdentityApp.Pages.StoreModel
<h4 class="bg-info text-white text-center p-2">Pages - Level 2 - Signed In Users</h4>
<div class="p-2">
    <table class="table table-sm table-striped table-bordered">
        <thead>
            <tr>
                <th>ID</th>
                <th>Name</th>
                <th>Category</th>
                <th class="text-right">Price</th>
            </tr>
        </thead>
        <tbody>
            @foreach (Product p in Model.DbContext.Products.OrderBy(p => p.Id))
            {
                <tr>
                    <td>@p.Id</td>
                    <td>@p.Name</td>
                    <td>@p.Category</td>
                    <td class="text-right">$@p.Price.ToString("F2")</td>
                </tr>
            }
        </tbody>
    </table>
</div>
```

> To define the page model class, add the code <code>Store.cshtml.cs</code> file in the <code>Pages</code> folder.
```C#
using IdentityApp.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace IdentityApp.Pages;

public class StoreModel : PageModel
{
    public StoreModel(ProductDbContext ctx) => DbContext = ctx;
    public ProductDbContext DbContext { get; set; }
}

```

> Add a Razor Page named <code>Admin.cshtml</code> to the <code>Pages</code> folder.

> This page will be available only to administrators
```Razor
@page "/pages/admin"
@model IdentityApp.Pages.AdminModel
<h4 class="bg-info text-white text-center p-2">Pages Level 3 - Administrators</h4>
<div class="p-2">
    <table class="table table-sm table-striped table-bordered">
        <thead>
            <tr>
                <th>ID</th>
                <th>Name</th>
                <th>Category</th>
                <th class="text-right">Price</th>
                <th></th>
            </tr>
        </thead>
        <tbody>
            @foreach (Product p in Model.DbContext.Products.OrderBy(p => p.Id))
            {
                <tr>
                    <td>@p.Id</td>
                    <td>@p.Name</td>
                    <td>@p.Category</td>
                    <td class="text-right">$@p.Price.ToString("F2")</td>
                    <td class="text-center">
                        <form method="post">
                            <button class="btn btn-sm btn-danger"
                                asp-route-id="@p.Id">
                                Delete
                            </button>
                            <a class="btn btn-sm btn-warning" asp-page="Edit"
                           asp-route-id="@p.Id">Edit</a>
                        </form>
                    </td>
                </tr>
            }
        </tbody>
    </table>
</div>
<a class="btn btn-info mx-2" asp-page="Edit">Create</a>
```

> To define the page model class, add the code <code>Admin.cshtml.cs</code> file in the <code>Pages</code> folder.
```C#
using IdentityApp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace IdentityApp.Pages;

public class AdminModel : PageModel
{
    public AdminModel(ProductDbContext ctx) => DbContext = ctx;
    public ProductDbContext DbContext { get; set; }
    public IActionResult OnPost(long id)
    {
        Product p = DbContext.Find<Product>(id);
        if (p != null)
        {
            DbContext.Remove(p);
            DbContext.SaveChanges();
        }
        return Page();
    }
}
```

> Add a Razor Page named <code>Edit.cshtml</code> to the <code>Pages</code> folder.
```Razor
@page "/pages/edit/{id:long?}"
@model IdentityApp.Pages.EditModel
<h4 class="bg-info text-white text-center p-2">Product Page</h4>
<form method="post" class="p-2">
    <div class="form-group">
        <label>ID</label>
        <input class="form-control" readonly asp-for="@Model.Product.Id" />
    </div>
    <div class="form-group">
        <label>Name</label>
        <input class="form-control" asp-for="@Model.Product.Name" />
    </div>
    <div class="form-group">
        <label>Category</label>
        <input class="form-control" asp-for="@Model.Product.Category" />
    </div>
    <div class="form-group">
        <label>Price</label>
        <input class="form-control" type="number" asp-for="@Model.Product.Price" />
    </div>
    <div class="text-center">
        <button type="submit" class="btn btn-secondary">Save</button>
        <a class="btn btn-secondary" asp-page="Admin">Cancel</a>
    </div>
</form>
```

> To define the page model class, add the code <code>Edit.cshtml.cs</code> file in the <code>Pages</code> folder.
```C#
using IdentityApp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace IdentityApp.Pages;

public class EditModel : PageModel
{
    public EditModel(ProductDbContext ctx) => DbContext = ctx;
    public ProductDbContext DbContext { get; set; }
    public Product Product { get; set; }
    public void OnGet(long id)
    {
        Product = DbContext.Find<Product>(id) ?? new Product();
    }
    public IActionResult OnPost([Bind(Prefix = "Product")] Product p)
    {
        DbContext.Update(p);
        DbContext.SaveChanges();
        return RedirectToPage("Admin");
    }
}
```

> Add a Razor View Imports file named <code>_ViewImports.cshtml</code> to the <code>Pages</code> folder enable tag helpers in Razor Pages and import some namespaces used in
the views (and some that are useful for working with ASP.NET Core Identity).
```Razor
@addTagHelper *, Microsoft.AspNetCore.Mvc.TagHelpers
@using Microsoft.AspNetCore.Mvc.RazorPages
@using Microsoft.AspNetCore.Identity
@using System.Security.Claims
@using IdentityApp.Pages
@using IdentityApp.Models
```

> Add a Razor View Start file named <code>_ViewStart.cshtml</code> to the <code>Pages</code> folder.
```Razor
@{
    Layout = "_Layout";
}
````

> Add a Razor Layout named <code>_NavigationPartial.cshtml</code> to the <code>Pages</code> folder.
```Razor
<div class="text-center m-2">
<a class="btn btn-secondary btn-sm" asp-page="Landing">Level 1</a>
<a class="btn btn-secondary btn-sm" asp-page="Store">Level 2</a>
<a class="btn btn-secondary btn-sm" asp-page="Admin">Level 3</a>
</div>
```

---
<code>The Razor Pages share a layout with the MVC controllers, and only the contents of the partial view will be different, allowing easy navigation between pages</code>
---
### Configure the Application
> Configuring the Application in the <code>Program.cs</code> File in the <code>IdentityApp</code> Folder.
```C#
using IdentityApp.Models;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();
builder.Services.AddDbContext<ProductDbContext>(opts =>
{
    opts.UseSqlServer(
    builder.Configuration["ConnectionStrings:AppDataConnection"]);
});
var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
app.UseStaticFiles();
app.UseRouting();

app.UseEndpoints(endpoints =>
{
    endpoints.MapDefaultControllerRoute();
    endpoints.MapRazorPages();
});

app.Run();

```

### Creating the Database
> Add Migration
```cli
dotnet ef migrations add Initial
```

> Delete and Create the database
```cli
dotnet ef database drop --force
dotnet ef database update
```

### Running the Application
```cli
dotnet run
```

## Enabling HTTPS Connections
> When using an ASP.NET Core application that requires authentication, it is important to ensure that
all requests are sent using HTTPS, which encrypts the messages between the browser and ASP.NET Core to
guard against eavesdropping.

### HTTPS VS. SSL VS. TLS
> **HTTPS** is the combination of **HTTP** and the transport layer security (**TLS**) or secure sockets layer (**SSL**).

> **TLS** has replaced the obsolete **SSL** protocol, but the term  **SSL** has become synonymous with secure
networking and is often used even when  **TLS** is responsible for securing a connection.

> If you are interested in security and cryptography, then the details of **HTTPS** are worth exploring, and 
https://en.wikipedia.org/wiki/HTTPS is a good place to start.

### Generating a Test Certificate
> An important **HTTPS** feature is the use of a certificate that allows web browsers to confirm they are
communicating with the right web server and not an impersonator.

> Generating and Trusting a New Certificate
```cli
dotnet dev-certs https --clean
dotnet dev-certs https --trust
```

### Enabling HTTPS
> To enable HTTPS, make the changes to the <code>launchSettings.json</code> file in the <code>Properties</code> folder.
```json
{
  "iisSettings": {
    "windowsAuthentication": false,
    "anonymousAuthentication": true,
    "iisExpress": {
      "applicationUrl": "http://localhost:34520",
      "sslPort": 44350
    }
  },
  "profiles": {
    "IdentityApp": {
      "commandName": "Project",
      "dotnetRunMessages": true,
      "launchBrowser": true,
      "applicationUrl": "http://localhost:5292;https://localhost:44350",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    },
    "IIS Express": {
      "commandName": "IISExpress",
      "launchBrowser": true,
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    }
  }
}
```

### Enabling HTTPS Redirection
```C#
using IdentityApp.Models;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();
builder.Services.AddDbContext<ProductDbContext>(opts =>
{
    opts.UseSqlServer(
    builder.Configuration["ConnectionStrings:AppDataConnection"]);
});

builder.Services.AddHttpsRedirection(opts =>
{
    opts.HttpsPort = 44350;
});

var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseEndpoints(endpoints =>
{
    endpoints.MapDefaultControllerRoute();
    endpoints.MapRazorPages();
});

app.Run();

```

## Restricting Access with an Authorization Policy
### Applying the Level 2 Authorization Policy
> The <code>Authorize</code> attribute is used to restrict access, applies the attribute tp the <code>Store</code> controller.
```C#
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
```

> Apply the <code>Authorize</code> attribute to <code>Store.cshtml.cs</code> file in the <code>Pages</code> folder.
```C#
using IdentityApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace IdentityApp.Pages;

[Authorize]
public class StoreModel : PageModel
{
    public StoreModel(ProductDbContext ctx) => DbContext = ctx;
    public ProductDbContext DbContext { get; set; }
}
```

> When the attribute is applied without any arguments, the effect is to restrict access to any signed-in
user. 

> I applied the attribute to the class, which applies this authorization policy to all of the action methods
defined by the controller.

> I have applied the attribute to the page model class of the Store Razor Page.

<code>The attribute can also be applied to Razor Pages with the **@attribute** expression </code>

### Applying the Level 3 Authorization Policy
> The <code>Authorize</code> attribute can be used to define more specific access restrictions. The most common
approach is to restrict access to users who have been assigned to a specific <code>role</code>.

> Restricting Access in the <code>AdminController.cs</code> File in the Controllers Folder.
```C#
using IdentityApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IdentityApp.Controllers;
[Authorize(Roles ="Admin")]
public class AdminController : Controller
{
    private ProductDbContext DbContext;
    public AdminController(ProductDbContext ctx) => DbContext = ctx;
    public IActionResult Index() => View(DbContext.Products);
    [HttpGet]
    public IActionResult Create() => View("Edit", new Product());
    [HttpGet]
    public IActionResult Edit(long id)
    {
        Product p = DbContext.Find<Product>(id);
        if (p != null)
        {
            return View("Edit", p);
        }
        return RedirectToAction(nameof(Index));
    }
    [HttpPost]
    public IActionResult Save(Product p)
    {
        DbContext.Update(p);
        DbContext.SaveChanges();
        return RedirectToAction(nameof(Index));
    }
    [HttpPost]
    public IActionResult Delete(long id)
    {
        Product p = DbContext.Find<Product>(id);
        if (p != null)
        {
            DbContext.Remove(p);
            DbContext.SaveChanges();
        }
        return RedirectToAction(nameof(Index));
    }
}

```

> Restricting Access in the <code>>Admin.cshtml.cs</code> File in the Pages Folder.
```C#
using IdentityApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace IdentityApp.Pages;

[Authorize(Roles = "Admin")]
public class AdminModel : PageModel
{
    public AdminModel(ProductDbContext ctx) => DbContext = ctx;
    public ProductDbContext DbContext { get; set; }
    public IActionResult OnPost(long id)
    {
        Product p = DbContext.Find<Product>(id);
        if (p != null)
        {
            DbContext.Remove(p);
            DbContext.SaveChanges();
        }
        return Page();
    }
}


```

> Restricting Access in the <code>Edit.cshtml.cs</code> File in the Pages Folder.
```C#
using IdentityApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace IdentityApp.Pages;

[Authorize(Roles = "Admin")]
public class EditModel : PageModel
{
    public EditModel(ProductDbContext ctx) => DbContext = ctx;
    public ProductDbContext DbContext { get; set; }
    public Product Product { get; set; }
    public void OnGet(long id)
    {
        Product = DbContext.Find<Product>(id) ?? new Product();
    }
    public IActionResult OnPost([Bind(Prefix = "Product")] Product p)
    {
        DbContext.Update(p);
        DbContext.SaveChanges();
        return RedirectToPage("Admin");
    }
}
```

### Configuring the Application
> Enable the ASP.NET Core features that handle authorization and authentication.
```C#
using IdentityApp.Models;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();
builder.Services.AddDbContext<ProductDbContext>(opts =>
{
    opts.UseSqlServer(
    builder.Configuration["ConnectionStrings:AppDataConnection"]);
});

builder.Services.AddHttpsRedirection(opts =>
{
    opts.HttpsPort = 44350;
});

var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.UseEndpoints(endpoints =>
{
    endpoints.MapDefaultControllerRoute();
    endpoints.MapRazorPages();
});

app.Run();

```

## Recap what we did till now
<code>I created the example application that I will use throughout this part of the book. The
application is simple but defines three levels of access control, which I use to explain how ASP.NET Core
Identity works and how it integrates into the ASP.NET Core platform. </code>