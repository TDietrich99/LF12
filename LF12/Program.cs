using LF12.Classes; // Importiere die RouteConfig Klasse
using LF12.Data;    // Importiere den ApplicationDbContext
using Microsoft.EntityFrameworkCore; // Für die DbContext-Konfiguration

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews()
    .AddRazorOptions(options =>
    {
        // Füge den zusätzlichen View-Suchpfad hinzu
        options.ViewLocationFormats.Add("/Views/Pages/{0}.cshtml");
    });

builder.Services.AddRazorPages();



var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.UseEndpoints(endpoints =>
{
    endpoints.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}");
});

app.MapRazorPages();

app.Run();
