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
```html&c#
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

