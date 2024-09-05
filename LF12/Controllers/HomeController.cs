using LF12.Classes.Classes;
using LF12.Classes.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Threading.Tasks;

namespace LF12.Controllers
{
    public class HomeController : Controller
    {
        private readonly IWebHostEnvironment _hostEnvironment;

        public HomeController(IWebHostEnvironment hostEnvironment)
        {
            _hostEnvironment = hostEnvironment;
        }

        [HttpGet]
        public IActionResult Index()
        {
            bool skip = true;
            if(!skip)
                return View(new ImageUploadModel());
            string guid = "D1D22F07-6C3F-42B7-B092-B81B8613059D";
            var json = System.IO.File.ReadAllText(Path.Combine("Jsons", guid, $"{guid}_RAW.json"));
            var cg = CrossGrid.FromJson(json);
            if (cg != null)
            {
                cg.Solve();
            }
            return View(new ImageUploadModel());
        }

        [HttpPost]
        public async Task<IActionResult> Upload(IFormFile uploadedImage)
        {
            var model = new ImageUploadModel();

            if (uploadedImage != null && uploadedImage.Length > 0)
            {

                var uploadPath = Path.Combine(_hostEnvironment.ContentRootPath, "Images","Uploaded");
                if (!Directory.Exists(uploadPath))
                {
                    Directory.CreateDirectory(uploadPath);
                }
                var crossgrid = new CrossGrid();
                string uniqueId = crossgrid.Id.ToString().ToUpper();
                var fileName = uniqueId + "_RAW.png";
                var filePath = Path.Combine(uploadPath, fileName);

                // Speichere das Bild im Ordner "Images/Uploaded"
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await uploadedImage.CopyToAsync(fileStream);
                }

                // Bereite das Bild fürs Lösen vor und speichere
                crossgrid.SetData();
                model.FileName = fileName;
                model.Message = "Bild erfolgreich hochgeladen!";
                model.FilePath = filePath;
            }
            else
            {
                model.Message = "Es wurde kein Bild hochgeladen.";
            }

            // Rückgabe an die Index-View
            return View("Index", model);
        }
    }
}
