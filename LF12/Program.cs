using LF12.Classes; // Importiere die RouteConfig Klasse

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews()
    .AddRazorOptions(options =>
    {
        // Füge den zusätzlichen View-Suchpfad hinzu
        options.ViewLocationFormats.Add("/Views/Pages/{0}.cshtml");
        // Weitere Pfade können bei Bedarf hinzugefügt werden
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

// Routen in separater Datei registrieren
app.UseEndpoints(endpoints =>
{
    RouteConfig.RegisterRoutes(endpoints); // Aufruf der RegisterRoutes Methode
});

app.MapRazorPages();

app.Run();
