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
<code>I created the example application. The
application is simple but defines three levels of access control, which I use to explain how ASP.NET Core
Identity works and how it integrates into the ASP.NET Core platform. </code>

# Using the Identity UI Package
> Microsoft provides a build user interface for Identity, known as Identity UI, which makes it possible to get up
and running quickly.

| Question    | Answer | 
| :---        | :---   |
| What is it? | The Identity UI package is a set of Razor Pages and supporting classes provided by Microsoft to jump-start the use of ASP.NET Core Identity in ASP.NET Core projects. |
| Why is it useful? | The Identity UI package provides all the workflows required for basic user management, including creating accounts and signing in with passwords, authenticators, and third-party services. |
| How is it used? | The Identity UI package is added to projects as a NuGet package and enabled with the <code>AddDefaultIdentity</code> extension method. |
| Are there any pitfalls or limitations? | The approach that Identity UI takes doesn’t suit all projects. This can be remedied either by adapting the features it provides or by working directly with the Identity API to create custom alternatives. |
| Are there any alternatives? | Identity provides an API that can be used to create custom alternatives to the Identity UI package. |

| Problem    | Solution | 
| :---       | :---     |
| Add Identity and the Identity UI package to a project | Add the NuGet packages to the project and configure them using the <code>AddDefaultIdentity</code> method in the Startup class. Create a database migration and use it to prepare a database for storing user data. |
| Present the user with the registration or sign-in links | Create a shared partial view named <code>_LoginPartial.cshtml</code>. |
| Create a consistent layout for the application and the Identity UI package | Define a Razor Layout and refer to it in a Razor View Start created in the <code>Areas/Identity/Pages</code> folder. |
| Add support for confirmations | Create an implementation of the <code>IEmailSender</code> interface and register it as a service in the <code>Program</code> class. |
| Display QR codes for configuring authenticator applications | Add the qrcodejs JavaScript package to the project and create a script element that applies it to the URL produced by the Identity UI package. |

## Adding ASP.NET Core Identity to the Project
> Adding the ASP.NET Core Identity Packages
```cli
dotnet add package Microsoft.Extensions.Identity.Core
dotnet add package Microsoft.AspNetCore.Identity.EntityFrameworkCore
```
> The first package contains the core Identity features. 

> The second package contains the features required to store data in a database using Entity Framework Core.

## Adding the Identity UI Package to the Project
```cli
dotnet add package Microsoft.AspNetCore.Identity.UI
```

## Defining the Database Connection String
> The easiest way to store Identity data is in a database, and Microsoft provides built-in support for doing this with Entity Framework Core. Although you can use a single database for the application’s domain data and the Identity data, 

> I recommend you keep everything separate so that you can manage the schemas independently. <code>According to Adam Freeman</code>

> Adding a Connection String in the <code>appsettings.json</code> File in the <code>IdentityApp</code> Folder.
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
    "AppDataConnection": "Server=(localdb)\\MSSQLLocalDB;Database=IdentityAppData;MultipleActiveResultSets=true",
    "IdentityConnection": "Server=(localdb)\\MSSQLLocalDB;Database=IdentityAppUserData;MultipleActiveResultSets=true"
  }
}
```

## Configuring the Application
> Configuring the Application in the <code>Program.cs</code> File in the <code>IdentityApp</code> Folder.
```C#
using IdentityApp.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

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

builder.Services.AddDbContext<IdentityDbContext>(opts => {
    opts.UseSqlServer(
    builder.Configuration["ConnectionStrings:IdentityConnection"],
    opts => opts.MigrationsAssembly("IdentityApp")
    );
});

builder.Services.AddDefaultIdentity<IdentityUser>()
.AddEntityFrameworkStores<IdentityDbContext>();

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

> The AddDbContext method is used to set up an Entity Framework Core database context for Identity.

> The database context class is <code>IdentityDbContext</code>, which is included in the Identity packages and includes details of the schema that will be used to store identity data.

> Because the <code>IdentityDbContext</code> class is defined in a different assembly, I have to tell Entity Framework Core to create database migrations in the IdentityApp project, like this:
```C#
services.AddDbContext<IdentityDbContext>(opts => {
opts.UseSqlServer(
Configuration["ConnectionStrings:IdentityConnection"],
opts => opts.MigrationsAssembly("IdentityApp")
);
});
```

> Setup up ASP.NET Core Identity.
```C#
services.AddDefaultIdentity<IdentityUser>()
    .AddEntityFrameworkStores<IdentityDbContext>();
```

> The reason that ASP.NET Core threw exceptions for requests to restricted URLs was that no services had been registered to authentication requests.

> The <code>AddDefaultIdentity</code> method sets up those services using sensible default values.

> The generic type argument specifies the class Identity will use to represent users.

> The default class is <code>IdentityUser</code>, which is included in the Identity package.

> The <code>AddEntityFrameworkStores</code> method sets up data storage using Entity Framework Core, and the generic type argument specifies the database context that will be used.

> Identity uses two kinds of datastore: the <code>user store</code> and the <code>role store</code>.

> The <code>user store</code> is the heart of Identity and is used to store all of the user data, including email addresses, passwords, and so on. Confusingly, membership of roles is kept in the user store.

> The <code>role store</code> contains additional information about roles that are used only in complex applications.

## Creating the Database
> Entity Framework Core requires a database migration, which will be used to create the database for Identity data.
```cli
dotnet ef migrations add IdentityInitial --context IdentityDbContext
dotnet ef database drop --force --context IdentityDbContext
dotnet ef database update --context IdentityDbContext
```

## Preparing the Login Partial View
> The Identity UI package requires a partial view named <code>_LoginPartial</code>, which is displayed at the top of every page. 

> Add a Razor View named <code>_LoginPartial.cshtml</code> to the <code>Views/Shared</code> folder.
```Razor
<div>Placeholder Content</div>
```

# Testing the Application with Identity
> run the application then click <code>level 2</code> button.

> the result will be like below

![Result!](/Images/1.png "Result")

# Completing the Application Setup
> The basic configuration is complete, but several features require additional work before they function correctly.

## Displaying Login Information
> Replacing the Contents of the <code>_LoginPartial.cshtml</code> File in the <code>Pages/Shared</code> Folder.
```Razor
<nav class="nav">
    @if (User.Identity.IsAuthenticated)
    {
        <a asp-area="Identity" asp-page="/Account/Manage/Index"
       class="nav-link bg-secondary text-white">
            @User.Identity.Name
        </a>
        <a asp-area="Identity" asp-page="/Account/Logout"
       class="nav-link bg-secondary text-white">
            Logout
        </a>
    }
    else
    {
        <a asp-area="Identity" asp-page="/Account/Login"
       class="nav-link bg-secondary text-white">
            Login/Register
        </a>
    }
</nav>
```

## Creating a Consistent Layout
> The Identity UI package is a collection of Razor Pages set up in a separate ASP.NET Core area. This means a project can override individual files from the Identity UI package by creating Razor Pages with the same names. 

> Add a Razor Layout named <code>_CustomIdentityLayout.cshtml</code> to the <code>Pages/Shared</code> folder.
```Razor
<!DOCTYPE html>
<html>
<head>
    <meta name="viewport" content="width=device-width" />
    <title>Identity App</title>
    <link rel="stylesheet" href="/Identity/lib/bootstrap/dist/css/bootstrap.css" />
    <link rel="stylesheet" href="/Identity/css/site.css" />
    <script src="/Identity/lib/jquery/dist/jquery.js"></script>
    <script src="/Identity/lib/bootstrap/dist/js/bootstrap.bundle.js"></script>
    <script src="/Identity/js/site.js" asp-append-version="true"></script>
</head>
<body>
    <nav class="navbar navbar-dark bg-secondary">
        <a class="navbar-brand text-white">IdentityApp</a>
        <div class="text-white"><partial name="_LoginPartial" /></div>
    </nav>
    <div class="m-2">
        @RenderBody()
        @await RenderSectionAsync("Scripts", required: false)
    </div>
</body>
</html>
```

> To use the new view, create the <code>Areas/Identity/Pages</code> folder and add to it a Razor View Start file named <code>_ViewStart.cshtml</code>.
```Razor
@{
    Layout = "_CustomIdentityLayout";
}
```

> Adding a Header in the <code>_Layout.cshtml</code> File in the <code>Views/Shared</code> Folder.
```Razor
<!DOCTYPE html>
<html>
<head>
    <meta name="viewport" content="width=device-width" />
    <title>Identity App</title>
    <link href="/lib/twitter-bootstrap/css/bootstrap.min.css" rel="stylesheet" />
</head>
<body>
    <nav class="navbar navbar-dark bg-secondary">
        <a class="navbar-brand text-white">IdentityApp</a>
        <div class="text-white"><partial name="_LoginPartial" /></div>
    </nav>
    <partial name="_NavigationPartial" />
    @RenderBody()
</body>
</html>
```

## Configuring Confirmations
> A confirmation is an email message that asks the user to click a link to confirm an action, such as creating an account or changing a password.

> The Identity support for confirmations but the Identity UI package provides a simplified confirmation process that requires an implementation of the <code>IEmailSender</code> interface, which is defined in the <code>Microsoft.AspNetCore.Identity.UI.Services</code> namespace.

> The <code>IEmailSender</code> interface defines one method.

| Name | Description |
| :--- | :---        |
| SendEmailAsync(emailAddress, subject,htmlMessage) | This method sends an email using the specified address,subject, and HTML message body. |

> The Identity UI package includes an implementation of the interface whose <code>SendEmailAsync</code> method does nothing.

> We will create a dummy email service till now.

> Create the <code>IdentityApp/Services</code> folder and add to it a class file named <code>ConsoleEmailSender.cs</code>.
```C#
using Microsoft.AspNetCore.Identity.UI.Services;
using System.Web;

namespace IdentityApp.Services;

public class ConsoleEmailSender : IEmailSender
{
    public Task SendEmailAsync(string emailAddress, string subject, string htmlMessage)
    {
        System.Console.WriteLine("---New Email----");
        System.Console.WriteLine($"To: {emailAddress}");
        System.Console.WriteLine($"Subject: {subject}");
        System.Console.WriteLine(HttpUtility.HtmlDecode(htmlMessage));
        System.Console.WriteLine("-------");
        return Task.CompletedTask;
    }
}
```

> Register <code>Email Sender Service</code> in the <code>Program.cs</code> File in the <code>IdentityApp</code> Folder.
```C#
using IdentityApp.Models;
using IdentityApp.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.UI.Services;
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

builder.Services.AddDbContext<IdentityDbContext>(opts =>
{
    opts.UseSqlServer(
    builder.Configuration["ConnectionStrings:IdentityConnection"],
    opts => opts.MigrationsAssembly("IdentityApp")
    );
});

builder.Services.AddScoped<IEmailSender, ConsoleEmailSender>();

builder.Services.AddDefaultIdentity<IdentityUser>()
.AddEntityFrameworkStores<IdentityDbContext>();

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

<mark>Notice that I have registered the email service before the call to the AddDefaultIdentity method so that my custom service takes precedence over the placeholder implementation in the Identity UI package.</mark>

## Displaying QR Codes
> Identity provides support for two-factor authentication, where the user has to present additional credentials to sign into the application.

> The Identity UI package supports a specific type of additional credential, which is a code generated by an authenticator application.

> An authenticator application is set up once and then generates authentication codes that can be validated by the application.

> To complete the setup for authenticators with Identity UI, a third-party JavaScript library named <code>qrcodejs</code> is required to generate QR codes that can be scanned by mobile devices to simplify the initial setup process.

> Adding a JavaScript Package.
```cli
libman install qrcodejs -d wwwroot/lib/qrcode
```

> Add the script elements to the <code>_CustomIdentityLayout.cshtml</code> file in the <code>Pages/Shared</code> folder.
```Razor
<!DOCTYPE html>
<html>
<head>
    <meta name="viewport" content="width=device-width" />
    <title>Identity App</title>
    <link rel="stylesheet" href="/Identity/lib/bootstrap/dist/css/bootstrap.css" />
    <link rel="stylesheet" href="/Identity/css/site.css" />
    <script src="/Identity/lib/jquery/dist/jquery.js"></script>
    <script src="/Identity/lib/bootstrap/dist/js/bootstrap.bundle.js"></script>
    <script src="/Identity/js/site.js" asp-append-version="true"></script>
    <script type="text/javascript" src="/lib/qrcode/qrcode.min.js"></script>
</head>
<body>
    <nav class="navbar navbar-dark bg-secondary">
        <a class="navbar-brand text-white">IdentityApp</a>
        <div class="text-white"><partial name="_LoginPartial" /></div>
    </nav>
    <div class="m-2">
        @RenderBody()
        @await RenderSectionAsync("Scripts", required: false)
    </div>

    <script type="text/javascript">
        var element = document.getElementById("qrCode");
        if (element !== null) {
            new QRCode(element, {
                text: document.getElementById("qrCodeData").getAttribute("data-url"),
                width: 150, height: 150
            });
            element.previousElementSibling?.remove();
        }
    </script>

</body>
</html>
```

# Using the Identity UI Workflows (processes)
> The basic configuration of the Identity UI package is complete.

> Each workflow combines multiple features to support a task, such as creating a new user account or changing a password.

## Registration
> The Identity UI package supports self-registration, which means that anyone can create a new account and then use it to sign into the application.

> There is one additional feature enabled by the configuration changes in the previous section, which is that a confirmation email is sent when a new account is created.

> Identity can be configured to require the user to click the confirmation link before signing into the application.

> The Identity UI Pages for Registration.

| Page | Description |
| :--- | :---        |
| Account/Register | This page prompts the user to create a new account. |
| Account/RegisterConfirmation | This is the page that handles the URLs sent in confirmation emails. |
| Account/ResendEmailConfirmation  | This page allows the user to request another confirmation email. |
| Account/ConfirmEmail | This is the page that handles the URLs sent in the emails when the user requests a confirmation be reset. |

## Signing In and Out of the Application
> One of the most important features provided by the Identity UI package is to sign users in and out of the application.

> The Identity UI Pages for Signing In and Signing Out.

| Page | Description |
| :--- | :---        |
| Account/Login | This page asks the user for their credentials or, if configured, to choose an external authentication service. |
| Account/ExternalLogin | This page is displayed after the user has signed into the application using an external authentication service. |
| Account/SetPassword | This page is used when an account has been created with an external authentication provider but the user wants to be able to sign in with a local password. |
| Account/Logout | This page allows the user to sign out of the application. |
| Account/Lockout | This page is displayed when the account is locked out following a series of failed sign-ins. |

## Using Two-Factor Authentication
> Identity supports a range of two-factor authentication options, one of which— authenticators—is available through the Identity UI package.

> To explore this workflow, you will need an authenticator application.

![Setting up two-factor authentication!](/Images/2.png "Setting up two-factor authentication")

> Once you have set up an authenticator, you will be redirected to the TwoFactorAuthentication page, which presents buttons for different management tasks.

> The Reset Recovery Codes button is used to generate single-use codes that can be used to sign in if the authenticator app is unavailable (such as when a mobile device has been lost or stolen).

> Click the button, and you will be presented with a set of recovery codes.

> It is not obvious, but each line shows two recovery codes, separated by a space.

> Each code can be used only once, after which it is invalidated.

![Generating recovery codes!](/Images/3.png "Generating recovery codes")

> The Identity UI Pages for Managing an Authenticator.

| Page | Description |
| :--- | :--- |
| Account/Manage/TwoFactorAuthentication | This is the page displayed when the user clicks the TwoFactor Authentication link in the self-management feature. It links to other pages that handle individual authenticator tasks. |
| Account/Manage/EnableAuthenticator | This page displays the QR code and setup key required to configure an authenticator. |
| Account/Manage/ResetAuthenticator | This page allows the user to generate a new authenticator setup code, which will invalidate the existing authenticator and allow a new one to be set up, which is done by the EnableAuthenticator page. |
| Account/Manage/GenerateRecoveryCodes  | This page generates a new set of recovery codes and then redirects to the ShowRecoveryCodes page to display them. |
| Account/Manage/ShowRecoveryCodes | This page generates a new set of recovery codes and then redirects to the ShowRecoveryCodes page to display them. |
| Account/Manage/Disable2fa | This page allows the user to disable the authenticator and return to signing into the application with just a password. |

> Sign into the application Once the password has been checked, you will be prompted to enter the current code displayed by the authenticator app. Enter the code and click the Log In button.

![Signing in using an authenticator code!](/Images/4.png "Signing in using an authenticator code")

> Click the Log In with a Recovery Code link instead of entering the authenticator code. You will be prompted to enter one of the recovery codes you generated earlier,

![Signing in with a recovery code!](/Images/5.png "Signing in with a recovery code")

> The Identity UI Pages for Two-Factor Authentication

| Name | Description |
| :--- | :--- |
| Account/LoginWith2fa | This page prompts the user to enter an authenticator code. |
| Account/LoginWithRecoveryCode | This page prompts the user to enter a recovery code |

## Recovering a Password
> If a user has forgotten their password, they can go through a recovery process to generate a new one.

> Password recovery works only if a user confirmed their email address following registration—the Identity UI package won’t send the recovery password email if a user hasn’t confirmed their email address.

> When reset the password , the password stored in the Identity user store will be updated, and you can sign into the application using the new password.

![Requesting password recovery!](/Images/6.png "Requesting password recovery")

![Choosing a new password!](/Images/7.png "Choosing a new password")

> The Identity UI Pages for Password Recovery

| Name | Description |
| :--- | :--- |
| Account/ForgotPassword | This page prompts the user for their email address and sends the confirmation email. |
| Account/ForgotPasswordConfirmation | This page is displayed once the confirmation email has been sent. |
| Account/ResetPassword | This page is targeted by the URL sent in the confirmation email. It prompts the user for their email address and a new password. |
| Account/ResetPasswordConfirmation | This page is displayed once the password has been changed and provides the user with confirmation that the process has been completed. |

## Changing Account Details
> The self-management features include support for changing the user’s details, including the phone number, email address, and password. 

![The phone and password change pages!](/Images/8.png "The phone and password change pages")

> The Identity UI Pages for Changing Account Details

| Name | Description |
| :--- | :--- |
| Account/Manage/Index | This page allows the user to set a phone number. |
| Account/Manage/ChangePassword | This page allows a new password to be chosen. |
| Account/Manage/Email | This page allows a new email address to be chosen and sends a confirmation email to the user. |
| Account/ConfirmEmailChange | This page allows a new email address to be chosen and sends a confirmation email to the user. |

## Managing Personal Data
> The Identity UI package provides a generic personal data feature that provides access to the data in the user store and allows the user to delete their account.

![Managing personal data!](/Images/9.png "Managing personal data")

![Managing personal data!](/Images/10.png "Managing personal data")

> The Identity UI Pages for Managing Personal Data

| Name | Description |
| :--- | :--- |
| Account/Manage/PersonalData | This is the page that presents the user with the buttons for downloading or deleting data. |
| Account/Manage/DownloadPersonalData | This is the page that generates the JDON document containing the user’s data. |
| Account/Manage/DeletePersonalData | This is the page that prompts the user for their password and deletes the account. |

## Denying Access
> The final workflow is used when the user is denied access to an action or Razor Page. This is known as the forbidden response, and it is the counterpart to the challenge response that prompts for user credentials

![The forbidden response!](/Images/11.png "The forbidden response")

> The Identity UI Page for the Forbidden Response

| Name | Description |
| :--- | :--- |
| Account/AccessDenied | This page displays a warning to the user. |

## Recap what we did till now
<code>I showed you how to Add Identity and the Identity UI package to a project. 
I showed you how to prepare the Identity database, I explained how to override individual files to create a consistent layout, and I
described the default workflows that the Identity UI package provides.</code>

# Configuring Identity
> Learn how to configure Identity, including how to support third-party services from Google, Facebook, and Twitter.

> Some of these configuration options are part of the ASP.NET Core platform, but since they are so closely related to ASP.NET Core Identity.

##  Putting Identity Configuration Options in Context :
| Question | Answer |
| :--- | :--- |
| What are they? | The Identity configuration options are a set of properties whose values are used by the classes that implement the Identity API, which can be used directly or consumed through the Identity UI package. |
| Why are they useful? | These configuration options let you change the way that Identity behaves, which can make your application easier to use or allow you to meet the type of security standard that is commonly found in large corporations. |
| How are they used? | Identity is configured using the standard ASP.NET Core options pattern. The configuration for external authentication services is done using extension methods provided in the package that Microsoft provides for each provider. |
| Are there any pitfalls or limitations?  | It is important to ensure that configuration changes do not cause problems for existing user accounts by enforcing a restriction that prevents the user from signing in. |
| Are there any alternatives? | The configuration options are used by the classes that provide the Identity API, which means the only way to avoid them is to create custom implementations. |

## Part Summary
| Problem | Solution |
| :--- | :--- |
| Specify policies for usernames, email addresses, passwords, account confirmations, and lockouts | Specify policies for usernames,
email addresses, passwords, account confirmations, and lockouts |
| Configure Facebook authentication | Install the package Microsoft provides for Facebook and use the AddFacebook method to configure the application ID and secret. |
| Configure Google authentication | Install the package Microsoft provides for Google and use the AddGoogle method to configure the application ID and secret. |
| Configure Twitter authentication | Install the package Microsoft provides for Twitter and use the AddTwitter method to configure the application ID and secret. |

> Resetting the Databases
```Cli
cd IdentityApp
dotnet ef database drop --force --context ProductDbContext
dotnet ef database drop --force --context IdentityDbContext
dotnet ef database update --context ProductDbContext
dotnet ef database update --context IdentityDbContext
```

## Configuring Identity
> Identity is configured using the standard ASP.NET Core options pattern, using the settings defined by the <code>IdentityOptions</code> class defined in the <code>Microsoft.AspNetCore.Identity</code> namespace.

> Useful IdentityOptions Properties

| Name | Description |
| :--- | :--- |
| User | This property is used to configure the <code>username</code> and <code>email</code> options for user accounts using the <code>UserOptions</code> class. |
| Password | This property is used to define the <code>password policy</code> using the <code>PasswordOptions</code> class. |
| SignIn | This property is used to specify the <code>confirmation</code> requirements for accounts using the <code>SignInOptions</code> class. |
| Lockout | This property uses the <code>LockoutOptions</code> class to define the policy for locking out accounts after a number of failed attempts to sign in. |

### Configuring User Options
> The <code>IdentityOptions.User</code> property is assigned a <code>UserOptions</code> object, which is used to configure the properties.

> The UserOptions Properties

| Name | Description |
| :--- | :--- |
| AllowedUserNameCharacters | This property specifies the characters allowed in usernames. The default value is the set of upper and lowercase A–Z characters, the digits 0–9, and the symbols -._@+ (hyphen, period, underscore, at character,and plus symbol). |
| RequireUniqueEmail | This property determines whether email addresses must be unique. The default value is false. |

> The <code>Identity UI</code> package isn’t affected by either property because it uses email addresses as usernames. 

> One consequence of this decision is that email addresses are effectively unique because Identity requires usernames to be unique.

> The default value of the <code>UserOptions.RequireUniqueEmail</code> property is <code>false</code>.

> You will receive an error message if you trying to create an account with email already created because the <code>Identity UI</code> package uses the <code>email</code> address as the <code>username</code> when creating an account.

![Creating an account with an existing email address!](/Images/12.png "Creating an account with an existing email address")

### Configuring Password Options
> The <code>IdentityOptions.Password</code> property is assigned a <code>PasswordOptions</code> object, which is used to configure the properties.

> The PasswordOptions Properties :

| Name | Description |
| :--- | :--- |
| RequiredLength | This property specifies a minimum number of characters for passwords. The default value is 6. |
| RequiredUniqueChars | This property specifies the minimum number of unique characters that a password must contain. The default value is 1. |
| RequireNonAlphanumeric | This property specifies whether passwords must contain nonalphanumeric characters, such as punctuation characters. The default value is true. |
| RequireLowercase | This property specifies whether passwords must contain lowercase characters. The default value is true. |
| RequireUppercase | This property specifies whether passwords must contain uppercase characters. The default value is true. |
| RequireDigit | This property specifies whether passwords must contain number characters.The default value is true.|

> The <code>IdentityUI</code> package only uses <code>email</code> addresses to identify users, to which the <code>UserOptions.AllowedUserNameCharacters</code> does not apply.

> Configuring <code>Password</code> Settings in the <code>Program.cs</code> File in the <code>IdentityApp</code> Folder.

```C#
builder.Services.AddDefaultIdentity<IdentityUser>(opts =>
{
    opts.Password.RequiredLength = 8;
    opts.Password.RequireDigit = false;
    opts.Password.RequireLowercase = false;
    opts.Password.RequireUppercase = false;
    opts.Password.RequireNonAlphanumeric = false;
})
.AddEntityFrameworkStores<IdentityDbContext>();
```
> Password Length < 8

![Specifying password options!](/Images/13.png "Specifying password options")

> Password Length >= 8

![Specifying password options!](/Images/14.png "Specifying password options")

### Configuring Sign-in Confirmation Requirements
> The <code>IdentityOptions.SignIn</code> property is assigned a <code>SignInOptions</code> object, which is used to configure the confirmation requirements for accounts using the properties.

> The SignInOptions Properties

| Name | Description |
| :--- | :--- |
| RequireConfirmedEmail | When this property is set to true, only accounts with confirmed email addresses can sign in. The default value is false. |
| RequireConfirmedPhoneNumber | When this property is set to true, only accounts with confirmed phone numbers can sign in. The default value is false. |
| RequireConfirmedAccount | When set to true, only accounts that pass verification by the <code>IUserConfirmation< T></code> interface can sign in. The default implementation checks that the email address has been confirmed. This default value for this property is false. |

> The Identity UI package doesn’t support phone number confirmations, so the RequireConfirmedPhoneNumber property must not be set to true because it will lock all users out of the application.

> It is a good idea to set the <code>RequireConfirmedAccount</code> property to true, If the application uses email for tasks such as password recovery.

> Requiring Email Confirmations in the <code>Program.cs</code> File in the <code>IdentityApp</code> folder.
```C#
builder.Services.AddDefaultIdentity<IdentityUser>(opts =>
{
    opts.Password.RequiredLength = 8;
    opts.Password.RequireDigit = false;
    opts.Password.RequireLowercase = false;
    opts.Password.RequireUppercase = false;
    opts.Password.RequireNonAlphanumeric = false;

    opts.SignIn.RequireConfirmedAccount = true;
})
.AddEntityFrameworkStores<IdentityDbContext>();
```

![The Identity UI confirmation behavior!](/Images/15.png "The Identity UI confirmation behavior")

> If you attempt to sign in using the new account without using the confirmation link, then you will be presented with a generic Invalid Login Attempt error.

> Click the Resend Email Confirmation link displayed by the Login page to generate a new confirmation email.

![Confirming an email address and signing into the application!](/Images/16.png "Confirming an email address and signing into the application")

### Configuring Lockout Options
> The <code>IdentityOptions.Lockout</code> property is assigned a LockoutOptions object, which is used to configure lockouts that prevent sign-ins, even if the correct password is used, after a number of failed attempts.

> The LockoutOptions Properties :

| Name | Description |
| :--- | :--- |
| MaxFailedAccessAttempts | This property specifies the number of failed attempts allowed before an account is locked out. The default value is 5. |
| DefaultLockoutTimeSpan | This property specifies the duration for lockouts. The default value is 5 minutes. |
| AllowedForNewUsers | This property determines whether the lockout feature is enabled for new accounts. The default value is true. |

## Configuring External Authentication
> External authentication delegate the process of authenticating users to a third-party service.

> External authentication generally uses the OAuth protocol.

> A registration process is required for each external service, during which the application is described and the level of access to user data, known as the scope, is declared.

> During registration, you will usually have to specify a redirection URL.

> During the authentication process, the external service will send the user’s browser an HTTP redirection to this URL, which triggers a request to ASP.NET Core, providing the application with data required to complete the sign-in.

> During <code>development</code>, this URL will be to <code>localhost</code>, 
such as <code>https://localhost:44350/signin-google</code>, for example.

---
<code>
When you are ready to deploy your application, you will need to update your application’s registration
with each external service to use a publicly accessible urL that contains a hostname that appears in the dnS.
</code>
---

> The registration process produces two data items: the <code>client ID</code> and the <code>client secret</code>. 

> The <code>client ID</code> identifies the application to the external authentication service and can be shared publicly. 

> The <code>client secret</code> is secret, as the name suggests, and should be <code>protected</code>.

### Configuring Facebook Authentication
> To register the application with Facebook, go to <code> https://developers.facebook.com/apps </code> and sign in with your Facebook account.

1. Click the Create App button.
2. Select <code>Build Connected Experiences</code> from the list, and click the Continue button. 
3. Enter <code>IdentityApp</code> into the <code>App Display Name</code> field and click the <code>Create</code> App button.


  ![Creating a new application!](/Images/17.png "Creating a new application")
  ![Creating a new application!](/Images/18.png "Creating a new application")
  ![Creating a new application!](/Images/19.png "Creating a new application")

> Once you have created a Facebook application, you will be returned to the developer dashboard and presented with a list of optional products to use.

> Locate <code>Facebook Login</code> and click the <code>Setup</code> button.

  ![The Facebook Login settings!](/Images/20.png "The Facebook Login settings")
  ![The Facebook Login settings!](/Images/21.png "The Facebook Login settings")

> You don’t need to specify a redirection URL because Facebook allows redirection to localhost URLs during development.

> When you are ready to deploy the application, you will need to return to this page and finalize your configuration, including providing the public-facing redirection URL.

> Details of the configuration options are included in the Facebook Login documentation
<code> https://developers.facebook.com/docs/facebook-login </code>.

> Navigate to the <code>Basic</code> section in the Settings area to get the <code>App ID</code> and <code>App Secret</code>.

  ![The application credentials for external authentication!](/Images/22.png "The application credentials for external authentication")

### Configuring ASP.NET Core for Facebook Authentication
> Store the <code>App ID</code> and <code>App Secret</code> using the <code>.NET secrets</code> feature, 
which ensures that these values won’t be included when the source code is committed into a repository.
```cli
dotnet user-secrets init
dotnet user-secrets set "Facebook:AppId" "<app-id>"
dotnet user-secrets set "Facebook:AppSecret" "<app-secret>"
```
> Adding the Facebook Package
```cli
dotnet add package Microsoft.AspNetCore.Authentication.Facebook
```
> Configuring <code>Facebook Authentication</code> in the <code>Program.cs</code> File in the <code>IdentityApp</code> Folder.
```C#
builder.Services.AddAuthentication().AddFacebook(opts =>
{
    opts.AppId = builder.Configuration["Facebook:AppId"];
    opts.AppSecret = builder.Configuration["Facebook:AppSecret"];
});
```
> The <code>AddAuthentication</code> method sets up the ASP.NET Core authentication features.

> This method is called automatically by the <code>AddDefaultIdentity</code> method, which is why it has not been needed until now.

> The <code>AddFacebook</code> method sets up the Facebook authentication support provided by Microsoft, which is configured using the <code>options pattern</code> with the <code>FacebookOptions</code> class.

> Selected FacebookOptions Properties :

| Name | Description |
| :--- | :--- |
| AppId | This property is used to configure the App ID, which is the term Facebook uses for the client ID. |
| AppSecret | This property is used to configure the App Secret, which is the term Facebook uses for the client secret. |
| Fields | This property specifies the data values that are requested from Facebook during authentication. The default values are name, email, first_name, and last_name. See https://developers.facebook.com/docs/graph-api/reference/user for a full list of fields, but bear in mind that some fields require applications to go through an additional validation process.|

> Restart the application and try to loin again.

  ![Signing in with Facebook!](/Images/23.png "Signing in with Facebook")
  ![Signing in with Facebook!](/Images/24.png "Signing in with Facebook")
  ![Signing in with Facebook!](/Images/25.png "Signing in with Facebook")

### Configuring Google Authentication
> To register the example application, navigate to <code> https://console.developers.google.com </code> and <code>sign in</code> with a Google account. 

1. Click the <code>OAuth Consent Screen option</code>.
2. Select <code>External for User Type</code>. which will allow any Google account to authenticate for your application.
3. Click Create, and you will be presented with a form. 
4. Enter <code>IdentityApp</code> into the <code>App Name</code> field and enter your <code>email address</code> in the <code>User Support Email</code> and <code>Developer Contact Information</code> sections of the form. 
5. Click Save and Continue, and you will be presented with the scope selection screen, which is used to specify the scopes that your application requires.
6. Click the Add or Remove Scopes button, and you be presented with the list of scopes that your application can request. 
7. Check three scopes: <code>openid, auth/userinfo.email, and auth/userinfo.profile</code>. 
8. Click the Update button to save your selection.
9. Click Save and Continue to return to the OAuth consent screen and then click Back to Dashboard.

> Configuring the Google OAuth consent screen:

  ![Configuring the Google OAuth consent screen!](/Images/26.png "Configuring the Google OAuth consent screen")
  ![Configuring the Google OAuth consent screen!](/Images/27.png "Configuring the Google OAuth consent screen")
  ![Configuring the Google OAuth consent screen!](/Images/28.png "Configuring the Google OAuth consent screen")
  ![Configuring the Google OAuth consent screen!](/Images/29.png "Configuring the Google OAuth consent screen")
  ![Configuring the Google OAuth consent screen!](/Images/30.png "Configuring the Google OAuth consent screen")

> Click the Publish App button and click Confirm:

  ![Publishing the application!](/Images/31.png "Publishing the application")

> Click the <code>Credentials</code> link, click the <code>Create Credentials</code> button at the top of the page, and select <code>OAuth Client ID</code> from the list of options.

> Select <code>Web Application</code> from the Application Type list and enter <code>IdentityApp</code> in the Name field.

> Click <code>Add URI</code> in the <code>Authorized Redirect URIs</code> section and enter <code> https://localhost:44350/signin-google </code> into the text field.

> Click the Create button, and you will be presented with the <code>client ID</code> and <code>client secret</code> for your application.

  ![Configuring application credentials!](/Images/32.png "Configuring application credentials")
  ![Configuring application credentials!](/Images/33.png "Configuring application credentials")
  ![Configuring application credentials!](/Images/34.png "Configuring application credentials")

### Configuring ASP.NET Core for Google Authentication
> Store the <code>Client ID</code> and <code>Client Secret</code> using the .NET secrets feature, which ensures that these values won’t be included when the source code is committed into a repository.
```cli
dotnet user-secrets init
dotnet user-secrets set "Google:ClientId" "<client-id>"
dotnet user-secrets set "Google:ClientSecret" "<client-secret>"
```

> Adding the Google Package
```cli
dotnet add package Microsoft.AspNetCore.Authentication.Google
```

> Configuring <code>Google Authentication</code> in the <code>Program.cs</code> File in the <code>IdentityApp</code> Folder
```C#
builder.Services.AddAuthentication().AddFacebook(opts =>
{
    opts.AppId = builder.Configuration["Facebook:AppId"];
    opts.AppSecret = builder.Configuration["Facebook:AppSecret"];
})
    .AddGoogle(opts =>
    {
        opts.ClientId = builder.Configuration["Google:ClientId"];
        opts.ClientSecret = builder.Configuration["Google:ClientSecret"];
    });
```
> The <code>AddGoogle</code> method sets up the <code>Google authentication</code> handler and is configured using the options pattern with the <code>GoogleOptions</code> class.

> Selected GoogleOptions Properties:

| Name | Description |
| :--- | :--- |
| ClientId | This property is used to specify the client ID for the application. |
| ClientSecret | This property is used to specify the application’s client secret. |
| Scope | This property is used to set the scopes that are requested from the authentication service. The default value requests the scopes specified during the setup process, but additional scopes are available. See <code> https://developers.google.com/identity/protocols/oauth2/web-server </code>. |

> Restart the application and try to loin again.

  ![Signing in with Google!](/Images/35.png "Signing in with Google")

### Configuring Twitter Authentication
> To register the application with Twitter, go to <code> https://developer.twitter.com/en/portal/dashboard </code>and <code>sign in</code> with a Twitter account.

1. Click the Create Project button.
1. Set the project name to Identity Project, and click the Next button.
1. Select a description from the list and click the Next button.
1. Enter a name and click the Complete button to finish the first part of the setup.

<mark>The name must be unique</mark>

  ![Creating a Twitter application configuration!](/Images/36.png "Creating a Twitter application configuration")
  ![Creating a Twitter application configuration!](/Images/37.png "Creating a Twitter application configuration")
  ![Creating a Twitter application configuration!](/Images/38.png "Creating a Twitter application configuration")
  ![Creating a Twitter application configuration!](/Images/39.png "Creating a Twitter application configuration")
  ![Creating a Twitter application configuration!](/Images/40.png "Creating a Twitter application configuration")

> When you create the Twitter app, you will be presented with a set of keys:

> <code>It is important to make a note of the API key and the API key secret (which are how Twitter refers to the client ID and the client secret) because you won’t be able to see them again.</code>

  ![Completing the registration process!](/Images/41.png "Completing the registration process")
  ![Completing the registration process!](/Images/42.png "Completing the registration process")
  ![Completing the registration process!](/Images/43.png "Completing the registration process")
  ![Completing the registration process!](/Images/44.png "Completing the registration process")
  

> You will also have to enter URLs for the website.

### Configuring ASP.NET Core for Twitter Authentication
> Store the <code>Client ID</code> and <code>Client Secret</code> using the .NET secrets feature, which ensures that these values won’t be included when the source code is committed into a repository.

> Storing the Twitter Client ID and Secret
```cli
dotnet user-secrets init
dotnet user-secrets set "Twitter:ApiKey" "<client-id>"
dotnet user-secrets set "Twitter:ApiSecret" "<client-secret>"
```
> Adding the Twitter Package
```cli
dotnet add package Microsoft.AspNetCore.Authentication.Twitter
```

> Configuring <code>Twitter Authentication</code> in the <code>Program.cs</code> File in the <code>IdentityApp</code> Folder.
```C#
builder.Services.AddAuthentication().AddFacebook(opts =>
{
    opts.AppId = builder.Configuration["Facebook:AppId"];
    opts.AppSecret = builder.Configuration["Facebook:AppSecret"];
})
    .AddGoogle(opts =>
    {
        opts.ClientId = builder.Configuration["Google:ClientId"];
        opts.ClientSecret = builder.Configuration["Google:ClientSecret"];
    })
    .AddTwitter(opts =>
    {
        opts.ConsumerKey = builder.Configuration["Twitter:ApiKey"];
        opts.ConsumerSecret = builder.Configuration["Twitter:ApiSecret"];
    });
```

> The <code>AddTwitter</code> method sets up the Twitter authentication handler and is configured using the options pattern with the <code>TwitterOptions</code> class.

> Selected TwitterOptions Properties:

| Name | Description |
| :--- | :--- |
| ConsumerKey | This property is used to specify the client ID for the application. |
| ConsumerSecret | This property is used to specify the application’s client secret. |
| RetrieveUserDetails | When set to true, this property requests user data, including the email address, as part of the authentication process. This property isn’t required when using the Identity UI package, which allows users to enter an email address. |

> Restart the application and try to loin again.

  ![Signing in with Twitter!](/Images/45.png "Signing in with Twitter")

### Configuring Microsoft Authentication
> To register the application with Microsoft, go to <code> https://go.microsoft.com/fwlink/?linkid=2083908 </code>and <code>sign in</code> with a Microsoft account.

> Navigate to the Azure portal - App registrations page and create or sign into a Microsoft account:
1. Select New registration.
1. Enter a Name.
1. Select an option for Supported account types.
    + The MicrosoftAccount package supports App Registrations created using "Accounts in any organizational directory" or "Accounts in any organizational directory and Microsoft accounts" options by default.
    + To use other options, set AuthorizationEndpoint and TokenEndpoint members of MicrosoftAccountOptions used to initialize the Microsoft Account authentication to the URLs displayed on Endpoints page of the App Registration after it is created (available by clicking Endpoints on the Overview page).
1. Under Redirect URI, enter your development URL with <code>/signin-microsoft</code> appended. For example, <code> https://localhost:44350/signin-microsoft </code> . The Microsoft authentication scheme configured later in this sample will automatically handle requests at /signin-microsoft route to implement the OAuth flow.
1. Select Register.

> Create client secret

1. In the left pane, select Certificates & secrets.
1. Under Client secrets, select New client secret
    + Add a description for the client secret.
    + Select the Add button.
1. Under Client secrets, copy the value of the client secret.

  ![Create App!](/Images/46.png "Create App")
  ![Create App!](/Images/47.png "Create App")

### Configuring ASP.NET Core for Microsoft Authentication
> Store the <code>Client ID</code> and <code>Client Secret</code> using the .NET secrets feature, which ensures that these values won’t be included when the source code is committed into a repository.

> Storing the microsoftt Client ID and Secret
```cli
dotnet user-secrets init
dotnet user-secrets set "Microsoft:ClientId" "<client-id>"
dotnet user-secrets set "Microsoft:ClientSecret" "<client-secret>"
```

> Application (client) ID found on the Overview page of the App Registration and Client Secret you created on the Certificates & secrets page with Secret Manager. 

> Adding the MicrosoftAccount Package
```cli
dotnet add package Microsoft.AspNetCore.Authentication.MicrosoftAccount
```
> Configuring <code>Microsoft Authentication</code> in the <code>Program.cs</code> File in the <code>IdentityApp</code> Folder.
```C#
builder.Services.AddAuthentication().AddFacebook(opts =>
{
    opts.AppId = builder.Configuration["Facebook:AppId"];
    opts.AppSecret = builder.Configuration["Facebook:AppSecret"];
})
    .AddGoogle(opts =>
    {
        opts.ClientId = builder.Configuration["Google:ClientId"];
        opts.ClientSecret = builder.Configuration["Google:ClientSecret"];
    })
    .AddTwitter(opts =>
    {
        opts.ConsumerKey = builder.Configuration["Twitter:ApiKey"];
        opts.ConsumerSecret = builder.Configuration["Twitter:ApiSecret"];
    })
     .AddMicrosoftAccount(microsoftOptions =>
    {
        microsoftOptions.ClientId = builder.Configuration["Microsoft:ClientId"];
        microsoftOptions.ClientSecret = builder.Configuration["Microsoft:ClientSecret"];
    });
```

> The <code>AddMicrosoftAccount</code> method sets up the Microsoft authentication handler and is configured using the options pattern with the <code>MicrosoftOptions</code> class.

> Selected MicrosoftOptions Properties:

| Name | Description |
| :--- | :--- |
| ClientId | This property is used to specify the client ID for the application. |
| ClientSecret | This property is used to specify the application’s client secret. |


> Restart the application and try to loin again.

  ![Signing in with Microsoft!](/Images/48.png "Signing in with Microsoft")

## Recap what we did till now
<code>I described the Identity configuration options, which determine the validation requirements
for accounts, passwords, and control-related features such as lockouts. I also described the process for
configuring ASP.NET Core and the Identity UI package to support external authentication services from
Google, Facebook, Microsoft, and Twitter.</code>

---

## Adapting Identity UI
> The Identity UI package has been designed for self-service applications that support authenticators for twofactor authentication and external authentication with third-party services.

> There is some flexibility within that design so that individual features can be altered or disabled, and new features can be introduced.

> Putting Identity UI Adaptations in Context:

| Question | Answer |
| :--- | :--- |
| What are they? | Adaptations allow the files in the Identity UI package to be added to the project so they can be modified, allowing features to be created, customized, or disabled. |
| Why are they useful? | If your project almost fits into the general model expected by the Identity UI package, adaptations can customize Identity UI to make it fit your needs exactly |
| How are they used? | azor Pages, views, and other files are added to the project using a process known as scaffolding. Scaffolded files are given precedence over those in the Identity UI package, which means that changes made to the scaffolded files become part of the content presented to the user. |
| Are there any pitfalls or limitations? | Although you can customize individual features, you cannot adjust the underlying approach taken by the Identity UI package, which means there are limits to extent of the changes you can make. Some scaffolding operations overwrite files even if they have been changed, requiring an awkward shuffling of files to protected customizations. |
| Are there any alternatives? | Identity provides an API that can be used to create custom workflows. |

### What is th Identity UI Scaffolding ?
> Is the process of using existig feature in the Identity UI package in your project, So you can easy modify features rather than re-creating them completely.

### How to Use UI Scaffolding ?
> The scaffolding process relies on a global .NET tool package.

> Use a PowerShell command prompt to run the below commands to remove any existing version of the package and install the version required.
```cli
dotnet tool uninstall --global dotnet-aspnet-codegenerator
dotnet tool install --global dotnet-aspnet-codegenerator
```
> An additional package must be added to the project to provide the global tool with the templates it needs to create new items.
```cli
dotnet add package Microsoft.VisualStudio.Web.CodeGeneration.Design
```

### Identity UI Pages for Scaffolding
> The command-line scaffolding tool allows individual pages to be scaffolded.

> To list the  complete list of the Razor Pages available for scaffolding use below command.
```cli
dotnet aspnet-codegenerator identity --listFiles
```

### Using the Identity UI Scaffolding

####  Using Scaffolding to Change HTML :

1. Installing the Font Awesome Package
```cli
 libman install font-awesome -d wwwroot/lib/font-awesome
```
2. Update <code>_CustomIdentityLayout.cshtml</code> File in the <code>Pages/Shared</code> Folder with below code.
```Razor
<!DOCTYPE html>
<html>
<head>
    <meta name="viewport" content="width=device-width" />
    <title>Identity App</title>
    <link rel="stylesheet" href="/Identity/lib/bootstrap/dist/css/bootstrap.css" />
    <link rel="stylesheet" href="/Identity/css/site.css" />
    <script src="/Identity/lib/jquery/dist/jquery.js"></script>
    <script src="/Identity/lib/bootstrap/dist/js/bootstrap.bundle.js"></script>
    <script src="/Identity/js/site.js" asp-append-version="true"></script>
    <script type="text/javascript" src="/lib/qrcode/qrcode.min.js"></script>
    <link href="/lib/font-awesome/css/all.min.css" rel="stylesheet" />
</head>
<body>
    <nav class="navbar navbar-dark bg-secondary">
        <a class="navbar-brand text-white">IdentityApp</a>
        <div class="text-white"><partial name="_LoginPartial" /></div>
    </nav>
    <div class="m-2">
        @RenderBody()
        @await RenderSectionAsync("Scripts", required: false)
    </div>

    <script type="text/javascript">
        var element = document.getElementById("qrCode");
        if (element !== null) {
            new QRCode(element, {
                text: document.getElementById("qrCodeData").getAttribute("data-url"),
                width: 150, height: 150
            });
            element.previousElementSibling?.remove();
        }
    </script>

</body>
</html>
```
3. Add a Razor View named <code>_ExternalButtonPartial.cshtml</code> to the <code>Pages/Shared</code> folder and use it to define the partial view.
```Razor
@model Microsoft.AspNetCore.Authentication.AuthenticationScheme

<button type="submit"
        class="btn btn-primary" name="provider" value="@Model.Name">
    <i class="@($"fab fa-{Model.Name.ToLower()}")"></i>
    @Model.DisplayName
</button>
```
> The model for the partial view is an <code>AuthenticationScheme</code> object, which is how ASP.NET Core describes an authentication option, with Name and DisplayName properties.

4. Now scaffold the page that displays the login buttons
```cli
dotnet aspnet-codegenerator identity --dbContext Microsoft.AspNetCore.Identity.EntityFrameworkCore.IdentityDbContext --files Account.Login
```
> The <code>dotnet aspnet-codegenerator identity</code> selects the Identity UI scaffolding tool.

> The <code>--dbContext</code> argument is used to specify the Entity Framework Core database context class.

> This argument must specify the complete name, including the namespace, of the context class used by the application.

> <mark>If the name does not match exactly, the scaffolding tool will create a new database context class, which will lead to
inconsistent results later.</mark>

> The <code>--files</code> argument specifies the files that should be scaffolded, using one or more names from the list produced in the previous section, separated by <code>semicolons</code>.

5. Applying a Partial in the <code>Login.cshtml</code> File in the <code>Areas/Identity/Pages/Account</code> Folder.
```Razor
@page
@model LoginModel

@{
    ViewData["Title"] = "Log in";
}

<h1>@ViewData["Title"]</h1>
<div class="row">
    <div class="col-md-4">
        <section>
            <form id="account" method="post">
                <h2>Use a local account to log in.</h2>
                <hr />
                <div asp-validation-summary="ModelOnly" class="text-danger"></div>
                <div class="form-floating">
                    <input asp-for="Input.Email" class="form-control" autocomplete="username" aria-required="true" />
                    <label asp-for="Input.Email" class="form-label"></label>
                    <span asp-validation-for="Input.Email" class="text-danger"></span>
                </div>
                <div class="form-floating">
                    <input asp-for="Input.Password" class="form-control" autocomplete="current-password" aria-required="true" />
                    <label asp-for="Input.Password" class="form-label"></label>
                    <span asp-validation-for="Input.Password" class="text-danger"></span>
                </div>
                <div>
                    <div class="checkbox">
                        <label asp-for="Input.RememberMe" class="form-label">
                            <input class="form-check-input" asp-for="Input.RememberMe" />
                            @Html.DisplayNameFor(m => m.Input.RememberMe)
                        </label>
                    </div>
                </div>
                <div>
                    <button id="login-submit" type="submit" class="w-100 btn btn-lg btn-primary">Log in</button>
                </div>
                <div>
                    <p>
                        <a id="forgot-password" asp-page="./ForgotPassword">Forgot your password?</a>
                    </p>
                    <p>
                        <a asp-page="./Register" asp-route-returnUrl="@Model.ReturnUrl">Register as a new user</a>
                    </p>
                    <p>
                        <a id="resend-confirmation" asp-page="./ResendEmailConfirmation">Resend email confirmation</a>
                    </p>
                </div>
            </form>
        </section>
    </div>
    <div class="col-md-6 col-md-offset-2">
        <section>
            <h3>Use another service to log in.</h3>
            <hr />
            @{
                if ((Model.ExternalLogins?.Count ?? 0) == 0)
                {
                    <div>
                        <p>
                            There are no external authentication services configured. See this <a href="https://go.microsoft.com/fwlink/?LinkID=532715">article
                            about setting up this ASP.NET application to support logging in via external services</a>.
                        </p>
                    </div>
                }
                else
                {
                    <form id="external-account" asp-page="./ExternalLogin" asp-route-returnUrl="@Model.ReturnUrl" method="post" class="form-horizontal">
                        <div>
                            <p>
                                @foreach (var provider in Model.ExternalLogins)
                                {
                                    <partial name="_ExternalButtonPartial"model="provider" />
                                }
                            </p>
                        </div>
                    </form>
                }
            }
        </section>
    </div>
</div>

@section Scripts {
    <partial name="_ValidationScriptsPartial" />
}
```
> In the case of the external authentication buttons, I also need to change the Account/Register page, which also presents buttons for the configured services.

6. Scaffold the page that add Register page
```cli
dotnet aspnet-codegenerator identity --dbContext Microsoft.AspNetCore.Identity.EntityFrameworkCore.IdentityDbContext --files Account.Register
```

7. Applying a Partial in the <code>Register.cshtml</code> File in the <code>Areas/Identity/Pages/Account</code> Folder.
```Razor
@page
@model RegisterModel
@{
    ViewData["Title"] = "Register";
}

<h1>@ViewData["Title"]</h1>

<div class="row">
    <div class="col-md-4">
        <form id="registerForm" asp-route-returnUrl="@Model.ReturnUrl" method="post">
            <h2>Create a new account.</h2>
            <hr />
            <div asp-validation-summary="ModelOnly" class="text-danger"></div>
            <div class="form-floating">
                <input asp-for="Input.Email" class="form-control" autocomplete="username" aria-required="true" />
                <label asp-for="Input.Email"></label>
                <span asp-validation-for="Input.Email" class="text-danger"></span>
            </div>
            <div class="form-floating">
                <input asp-for="Input.Password" class="form-control" autocomplete="new-password" aria-required="true" />
                <label asp-for="Input.Password"></label>
                <span asp-validation-for="Input.Password" class="text-danger"></span>
            </div>
            <div class="form-floating">
                <input asp-for="Input.ConfirmPassword" class="form-control" autocomplete="new-password" aria-required="true" />
                <label asp-for="Input.ConfirmPassword"></label>
                <span asp-validation-for="Input.ConfirmPassword" class="text-danger"></span>
            </div>
            <button id="registerSubmit" type="submit" class="w-100 btn btn-lg btn-primary">Register</button>
        </form>
    </div>
    <div class="col-md-6 col-md-offset-2">
        <section>
            <h3>Use another service to register.</h3>
            <hr />
            @{
                if ((Model.ExternalLogins?.Count ?? 0) == 0)
                {
                    <div>
                        <p>
                            There are no external authentication services configured. See this <a href="https://go.microsoft.com/fwlink/?LinkID=532715">
                                article
                                about setting up this ASP.NET application to support logging in via external services
                            </a>.
                        </p>
                    </div>
                }
                else
                {
                    <form id="external-account" asp-page="./ExternalLogin" asp-route-returnUrl="@Model.ReturnUrl" method="post" class="form-horizontal">
                        <div>
                            <p>
                                @foreach (var provider in Model.ExternalLogins)
                                {
                                    <partial name="_ExternalButtonPartial" model="provider" />
                                }
                            </p>
                        </div>
                    </form>
                }
            }
        </section>
    </div>
</div>

@section Scripts {
    <partial name="_ValidationScriptsPartial" />
}
```
> Restart the application

![Using scaffolding to change HTML!](/Images/49.png "Using scaffolding to change HTML")

![Using scaffolding to change HTML!](/Images/50.png "Using scaffolding to change HTML")

#### Using Scaffolding to Modify C# Code
> Scaffolding doesn’t just override the view part of a Razor Page. It also creates a page model class containing the C# code that implements the features presented by the page.

> Locate the <code>OnPostAsync</code> method in the <code>Login.cshtml.cs</code> file and change the final argument to the <code>PasswordSignInAsync</code> method.
```C#
 var result = await _signInManager.PasswordSignInAsync(Input.Email, Input.Password, Input.RememberMe, lockoutOnFailure: true);
```

> By default, the Login page signs users into the application so that failed attempts do not lead to lockouts.

> Alters this behavior so that failed attempts count toward a lockout, based on the configuration options we did before. 

> Restart the application try to login with invalid password credentials more than 5 times as configure you will Lockout.

> Further attempts to sign in, even with the correct password, will fail until the lockout expires.

![Locking out an account!](/Images/51.png "Locking out an account")
![Locking out an account!](/Images/52.png "Locking out an account")

### Configuring the Account Management Pages
> The Identity UI package uses a layout and partial view to present the navigation links for the selfmanagement features.

![The account self-management features!](/Images/53.png "The account self-management features")

> The Identity UI Razor Pages for account management are defined in the <code>Areas/Identity/Pages/Account/Manage</code> folder and have the Account.

> Manage prefix when listed with the scaffolding tool.

> Listing the Management Pages:
```cli
dotnet aspnet-codegenerator identity --listFiles | Where-Object {$_ -like '*Manage*'}
```
![Listing the Management Pages!](/Images/54.png "Listing the Management Pages")

> In addition to the Razor Pages for specific features, the list contains two files that are useful in their own right: Account.Manage._Layout and Account.Manage._ManageNav.

> The _Layout file is the Razor Layout used by the management Razor Pages. The _ManageNav file is a partial view that generates the links on the left of the layout.

> Scaffolding the Management Layout Files:
```cli
dotnet aspnet-codegenerator identity --dbContext Microsoft.AspNetCore.Identity.EntityFrameworkCore.IdentityDbContext --files "Account.Manage._Layout;Account.Manage._ManageNav"
```

#### Changing the Management Layout
> The <code>_Layout.cshtml</code> file created by the previous command is used to present a consistent layout for all the account management Razor Pages.

> Changing Text in the <code>_Layout.cshtml</code> File in the <code>Areas/Identity/Pages/Account/Manage</code> Folder:
```Razor
@{
    if (ViewData.TryGetValue("ParentLayout", out var parentLayout))
    {
        Layout = (string)parentLayout;
    }
    else
    {
        Layout = "/Areas/Identity/Pages/_Layout.cshtml";
    }
}

<h2>Account Self-Management</h2>

<div>
    <h2>Change your account settings</h2>
    <hr />
    <div class="row">
        <div class="col-md-3">
            <partial name="_ManageNav" />
        </div>
        <div class="col-md-9">
            @RenderBody()
        </div>
    </div>
</div>

@section Scripts {
    @RenderSection("Scripts", required: false)
}
```

> Restart the application

![Modifying the self-management layout!](/Images/55.png "Modifying the self-management layout")

#### Adding an Account Management Page
#### Preparing the Navigation Link
> To add a new page to the management interface, the first step is to modify the ManageNavPages class, which is used to keep track of the selected page so that the appropriate link is highlighted in the layout.

> Preparing for a New Page in the <code>ManageNavPages.cs</code> File in the <code>Areas/Identity/Pages/Account/Manage</code> Folder:
```C#
 public static string StoreData => "StoreData";

 public static string StoreDataNavClass(ViewContext viewContext)
            => PageNavClass(viewContext, StoreData);
```
> The first part of the ManageNavPages class is a set of read-only string properties for each of the Razor Pages for which links are displayed.
These properties make it easy to replace the default pages without breaking the way the links are displayed.

> The next section is a set of methods used by the <code>_ManageNav</code> partial to set the classes for the link elements for each page.
These methods used the private <code>PageNavClass</code> method to return the string <code>active</code> if the page they represent has been <code>selected</code>, which is determined by reading a view data property named <code>ActivePage</code>.

#### Adding the Navigation Link
> Adding a Link in the <code>_ManageNav.cshtml</code> File in the <code>Areas/Identity/Pages/Account/Manage</code>:
```Razor
@inject SignInManager<IdentityUser> SignInManager
@{
    var hasExternalLogins = (await SignInManager.GetExternalAuthenticationSchemesAsync()).Any();
}
<ul class="nav nav-pills flex-column">
    <li class="nav-item"><a class="nav-link @ManageNavPages.IndexNavClass(ViewContext)" id="profile" asp-page="./Index">Profile</a></li>
    <li class="nav-item"><a class="nav-link @ManageNavPages.StoreDataNavClass(ViewContext)"id="personal-data" asp-page="./StoreData">Store Data</a></li>
    <li class="nav-item"><a class="nav-link @ManageNavPages.EmailNavClass(ViewContext)" id="email" asp-page="./Email">Email</a></li>
    <li class="nav-item"><a class="nav-link @ManageNavPages.ChangePasswordNavClass(ViewContext)" id="change-password" asp-page="./ChangePassword">Password</a></li>
    @if (hasExternalLogins)
    {
        <li id="external-logins" class="nav-item"><a id="external-login" class="nav-link @ManageNavPages.ExternalLoginsNavClass(ViewContext)" asp-page="./ExternalLogins">External logins</a></li>
    }
    <li class="nav-item"><a class="nav-link @ManageNavPages.TwoFactorAuthenticationNavClass(ViewContext)" id="two-factor" asp-page="./TwoFactorAuthentication">Two-factor authentication</a></li>
    <li class="nav-item"><a class="nav-link @ManageNavPages.PersonalDataNavClass(ViewContext)" id="personal-data" asp-page="./PersonalData">Personal data</a></li>
</ul>
```

<mark>
The <code>_ManageNav</code> partial uses the <code>GetExternalAuthenticationSchemesAsync</code> method defined by the
<code> SignInManager< IdentityUser></code> class to determine whether the current user has logged in using an external authentication service.
</mark>

#### Defining the New Razor Page
> Add a Razor Page named <code>StoreData.cshtml</code> to the <code>Areas/Identity/Pages/Account/Manage</code> folder.
```Razor
@page
@inject UserManager<IdentityUser> UserManager
@{
    ViewData["ActivePage"] = ManageNavPages.StoreData;
    IdentityUser user = await UserManager.GetUserAsync(User);
}
<h4>Store Data</h4>
<table class="table table-sm table-bordered table-striped">
    <thead>
        <tr><th>Property</th><th>Value</th></tr>
    </thead>
    <tbody>
        <tr>
            <td>Id</td>
            <td>@user.Id</td>
        </tr>
        @foreach (var prop in typeof(IdentityUser).GetProperties())
        {
            if (prop.Name != "Id")
            {
                <tr>
                    <td>@prop.Name</td>
                    <td class="text-truncate" style="max-width:250px">
                        @prop.GetValue(user)
                    </td>
                </tr>
            }
        }
    </tbody>
</table>
```

> Restart the application

![Adding an account management page!](/Images/56.png "Adding an account management page")

### Overriding the Default Layout in an Account Management Page
> The Razor layout that is scaffolded for the account management pages allows Razor Pages to override the default layout by setting a view data property named <code>ParentLayout</code>.

> Add a Razor Layout named <code>_InfoLayout.cshtml</code> to the <code>Pages/Shared</code> folder.
```Razor
<!DOCTYPE html>
<html>
<head>
    <meta name="viewport" content="width=device-width" />
    <title>Identity App</title>
    <link href="/lib/twitter-bootstrap/css/bootstrap.min.css" rel="stylesheet" />
</head>
<body>
    <nav class="navbar navbar-dark bg-info">
        <a class="navbar-brand text-white">IdentityApp</a>
        <div class="text-white"><partial name="_LoginPartial" /></div>
    </nav>
    <partial name="_NavigationPartial" />
    <div class="m-2">
        @RenderBody()
    </div>
    @RenderSection("Scripts", false)
</body>
</html>
```
> Selecting a Top-Level View in the <code>StoreData.cshtml</code> File in the <code>Areas/Identity/Pages/Account/Manage</code> Folder.
```Razor
@page
@inject UserManager<IdentityUser> UserManager
@{
    ViewData["ActivePage"] = ManageNavPages.StoreData;
    ViewData["ParentLayout"] = "_InfoLayout";
    IdentityUser user = await UserManager.GetUserAsync(User);
}
<h4>Store Data</h4>
<table class="table table-sm table-bordered table-striped">
    <thead>
        <tr><th>Property</th><th>Value</th></tr>
    </thead>
    <tbody>
        <tr>
            <td>Id</td>
            <td>@user.Id</td>
        </tr>
        @foreach (var prop in typeof(IdentityUser).GetProperties())
        {
            if (prop.Name != "Id")
            {
                <tr>
                    <td>@prop.Name</td>
                    <td class="text-truncate" style="max-width:250px">
                        @prop.GetValue(user)
                    </td>
                </tr>
            }
        }
    </tbody>
</table>
```

>Restart the application

![Overriding the top-level header!](/Images/57.png "verriding the top-level header")

### Tidying Up the QR Code Support
> Scaffold the Razor Page that displays the QR code and modify it to remove the placeholder content and include the JavaScript files that are required.

#### Scaffold File Shuffle
> When you scaffold the account management files, the scaffolding tool tries to create the <code>ManageNavPages.cs</code> and <code>_ManageNav.cshtml</code> files, even though these files already exist.
The result is an awkward file shuffle, which is most easily accomplished using two PowerShell command prompts.

> Moving Files Before Scaffolding:
```cli
cd Areas\Identity\Pages\Account\Manage
Move-Item -Path ManageNavPages.cs -Destination ManageNavPages.cs.safe
Move-Item -Path _ManageNav.cshtml -Destination _ManageNav.cshtml.safe
```

> Scaffolding the Identity UI Page for Authenticator Setup:
```cli
cd ../../../../../
dotnet aspnet-codegenerator identity --dbContext Microsoft.AspNetCore.Identity.EntityFrameworkCore.IdentityDbContext --files Account.Manage.EnableAuthenticator --no-build --force
```

> Moving back Files After Scaffolding:
```cli
 cd Areas\Identity\Pages\Account\Manage\
 Move-Item -Path ManageNavPages.cs.safe -Destination ManageNavPages.cs -Force
 Move-Item -Path _ManageNav.cshtml.safe -Destination _ManageNav.cshtml -Force
```
> The result of the file shuffle is that the EnableAuthenticator Razor Page has been scaffolded, and the changes made to the ManageNavPages class and the _ManageNav partial view have been preserved.

#### Modifying the Razor Page
> Replace the contents of the <code>EnableAuthenticator.cshtml</code> file with below code to incorporate the QR code directly in the content produced by the page.

> This is a simplification of the HTML in the original page, with additional script elements to generate the QR code.

> Replacing the Contents of the <code>EnableAuthenticator.cshtml</code> File in the <code>Areas/Identity/Pages/Account/Manage</code> Folder:
```Razor
@page
@model EnableAuthenticatorModel
@{
    ViewData["Title"] = "Configure authenticator app";
    ViewData["ActivePage"] = ManageNavPages.TwoFactorAuthentication;
}
<partial name="_StatusMessage" for="StatusMessage" />
<h4>@ViewData["Title"]</h4>
<div>
    <p>To use an authenticator app go through the following steps:</p>
    <ol class="list">
        <li>
            <p>
                Download a two-factor authenticator app like
                Microsoft Authenticator or Google Authenticator.
            </p>
        </li>
        <li>
            <p>
                Scan the QR Code or enter this key <kbd>@Model.SharedKey</kbd>
                into your two factor authenticator app. Spaces
                and casing do not matter.
            </p>
            <div id="qrCode"></div>
            <div id="qrCodeData" data-url="@Html.Raw(@Model.AuthenticatorUri)"></div>
        </li>
        <li>
            <p>
                Once you have scanned the QR code or input the key above,
                your two factor authentication app will provide you
                with a unique code. Enter the code in the confirmation box below.
            </p>
            <div class="row">
                <div class="col-md-6">
                    <form id="send-code" method="post">
                        <div class="form-group">
                            <label asp-for="Input.Code" class="control-label">
                                Verification Code
                            </label>
                            <input asp-for="Input.Code" class="form-control"
                                   autocomplete="off" />
                            <span asp-validation-for="Input.Code"
                                  class="text-danger"></span>
                        </div>
                        <button type="submit" class="btn btn-primary">Verify</button>
                        <div asp-validation-summary="ModelOnly" class="text-danger">
                        </div>
                    </form>
                </div>
            </div>
        </li>
    </ol>
</div>
@section Scripts {
    <partial name="_ValidationScriptsPartial" />
    <script type="text/javascript" src="/lib/qrcode/qrcode.min.js"></script>
    <script type="text/javascript">
        new QRCode(document.getElementById("qrCode"), {
            text: document.getElementById("qrCodeData").getAttribute("data-url"),
            width: 150, height: 150
        });
    </script>
}
```

> Moving the JavaScript code into the Razor Page means that it can be removed from the shared layout.

> Removing JavaScript Code from the <code>_CustomIdentityLayout.cshtml</code> File in the <code>Pages/Shared</code> Folder:
```Razor
<!DOCTYPE html>
<html>
<head>
    <meta name="viewport" content="width=device-width" />
    <title>Identity App</title>
    <link rel="stylesheet" href="/Identity/lib/bootstrap/dist/css/bootstrap.css" />
    <link rel="stylesheet" href="/Identity/css/site.css" />
    <script src="/Identity/lib/jquery/dist/jquery.js"></script>
    <script src="/Identity/lib/bootstrap/dist/js/bootstrap.bundle.js"></script>
    <script src="/Identity/js/site.js" asp-append-version="true"></script>
    <@*script type="text/javascript" src="/lib/qrcode/qrcode.min.js"></script>*@
    <link href="/lib/font-awesome/css/all.min.css" rel="stylesheet" />
</head>
<body>
    <nav class="navbar navbar-dark bg-secondary">
        <a class="navbar-brand text-white">IdentityApp</a>
        <div class="text-white"><partial name="_LoginPartial" /></div>
    </nav>
    <div class="m-2">
        @RenderBody()
        @await RenderSectionAsync("Scripts", required: false)
    </div>

    @*<script type="text/javascript">
        var element = document.getElementById("qrCode");
        if (element !== null) {
            new QRCode(element, {
                text: document.getElementById("qrCodeData").getAttribute("data-url"),
                width: 150, height: 150
            });
            element.previousElementSibling?.remove();
        }
    </script>*@

</body>
</html>
```

> Restart the application and login then go to <code>Manage Account</code> page then click <code>Two-factor authentication</code> Link.

![Tidying up QR code generation!](/Images/57.png "Tidying up QR code generation")

### Using Scaffolding to Disable Features
> Scaffolding can also be used to disable features,that don’t suit your project.

> Removing a Link in the <code>Login.cshtml</code> File in the <code>Areas/Identity/Pages/Account</code> Folder:
```Razor
@page
@model LoginModel

@{
    ViewData["Title"] = "Log in";
}

<h1>@ViewData["Title"]</h1>
<div class="row">
    <div class="col-md-4">
        <section>
            <form id="account" method="post">
                <h2>Use a local account to log in.</h2>
                <hr />
                <div asp-validation-summary="ModelOnly" class="text-danger"></div>
                <div class="form-floating">
                    <input asp-for="Input.Email" class="form-control" autocomplete="username" aria-required="true" />
                    <label asp-for="Input.Email" class="form-label"></label>
                    <span asp-validation-for="Input.Email" class="text-danger"></span>
                </div>
                <div class="form-floating">
                    <input asp-for="Input.Password" class="form-control" autocomplete="current-password" aria-required="true" />
                    <label asp-for="Input.Password" class="form-label"></label>
                    <span asp-validation-for="Input.Password" class="text-danger"></span>
                </div>
                <div>
                    <div class="checkbox">
                        <label asp-for="Input.RememberMe" class="form-label">
                            <input class="form-check-input" asp-for="Input.RememberMe" />
                            @Html.DisplayNameFor(m => m.Input.RememberMe)
                        </label>
                    </div>
                </div>
                <div>
                    <button id="login-submit" type="submit" class="w-100 btn btn-lg btn-primary">Log in</button>
                </div>
                <div>
                   @* <p>
                        <a id="forgot-password" asp-page="./ForgotPassword">Forgot your password?</a>
                    </p>*@
                    <p>
                        <a asp-page="./Register" asp-route-returnUrl="@Model.ReturnUrl">Register as a new user</a>
                    </p>
                    <p>
                        <a id="resend-confirmation" asp-page="./ResendEmailConfirmation">Resend email confirmation</a>
                    </p>
                </div>
            </form>
        </section>
    </div>
    <div class="col-md-6 col-md-offset-2">
        <section>
            <h3>Use another service to log in.</h3>
            <hr />
            @{
                if ((Model.ExternalLogins?.Count ?? 0) == 0)
                {
                    <div>
                        <p>
                            There are no external authentication services configured. See this <a href="https://go.microsoft.com/fwlink/?LinkID=532715">article
                            about setting up this ASP.NET application to support logging in via external services</a>.
                        </p>
                    </div>
                }
                else
                {
                    <form id="external-account" asp-page="./ExternalLogin" asp-route-returnUrl="@Model.ReturnUrl" method="post" class="form-horizontal">
                        <div>
                            <p>
                                @foreach (var provider in Model.ExternalLogins)
                                {
                                    <partial name="_ExternalButtonPartial"model="provider" />
                                }
                            </p>
                        </div>
                    </form>
                }
            }
        </section>
    </div>
</div>

@section Scripts {
    <partial name="_ValidationScriptsPartial" />
}
```

> It isn’t enough to just disable a link because the user can navigate directly to the URL for the password recovery pages.

> Scaffolding the Password Recovery Pages:
```cli
dotnet aspnet-codegenerator identity --dbContext Microsoft.AspNetCore.Identity.EntityFrameworkCore.IdentityDbContext --files Account.ForgotPassword --force
```

> Redefining Methods in the <code>ForgotPassword.cshtml.cs</code> File in the <code>Areas/Identity/Pages/Account</code> Folder to redirect the browser to the <code>Login</code> page:
```C#
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace IdentityApp.Areas.Identity.Pages.Account
{
    public class ForgotPasswordModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IEmailSender _emailSender;

        public ForgotPasswordModel(UserManager<IdentityUser> userManager, IEmailSender emailSender)
        {
            _userManager = userManager;
            _emailSender = emailSender;
        }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        [BindProperty]
        public InputModel Input { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public class InputModel
        {
            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            [Required]
            [EmailAddress]
            public string Email { get; set; }
        }

        //public async Task<IActionResult> OnPostAsync()
        //{
        //    if (ModelState.IsValid)
        //    {
        //        var user = await _userManager.FindByEmailAsync(Input.Email);
        //        if (user == null || !(await _userManager.IsEmailConfirmedAsync(user)))
        //        {
        //            // Don't reveal that the user does not exist or is not confirmed
        //            return RedirectToPage("./ForgotPasswordConfirmation");
        //        }

        //        // For more information on how to enable account confirmation and password reset please
        //        // visit https://go.microsoft.com/fwlink/?LinkID=532713
        //        var code = await _userManager.GeneratePasswordResetTokenAsync(user);
        //        code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
        //        var callbackUrl = Url.Page(
        //            "/Account/ResetPassword",
        //            pageHandler: null,
        //            values: new { area = "Identity", code },
        //            protocol: Request.Scheme);

        //        await _emailSender.SendEmailAsync(
        //            Input.Email,
        //            "Reset Password",
        //            $"Please reset your password by <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>clicking here</a>.");

        //        return RedirectToPage("./ForgotPasswordConfirmation");
        //    }

        //    return Page();
        //}

        public IActionResult OnGet() => RedirectToPage("./Login");
        public IActionResult OnPost() => RedirectToPage("./Login");
    }
}

```

> Restart the application

![Disabling a ForgotPassword feature!](/Images/58.png "Disabling a feature")

## Recap what we did till now
<code>I explained the different ways in which the Identity UI package can be adapted to suit the
needs of a project. I demonstrated how the scaffolding process can be used to bring Razor Pages into the
application where they take precedence over the default pages in the Identity UI package. This feature can
be used to modify the features that the Identity UI package provides, create new features, or remove features
entirely. Although the adaptations provide flexibility, there are limits to the customizations that can be
made.</code>

---