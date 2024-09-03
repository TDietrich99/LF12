using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace LF12.Classes
{
    public static class RouteConfig
    {
        public static void RegisterRoutes(IEndpointRouteBuilder endpoints)
        {
            endpoints.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            // Weitere benutzerdefinierte Routen können hier hinzugefügt werden
            endpoints.MapControllerRoute(
                name: "uploadRoute",
                pattern: "upload",
                defaults: new { controller = "Home", action = "Upload" }
                
                );
        }
    }
}
