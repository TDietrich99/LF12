using Emgu.CV;
using Emgu.CV.CvEnum;
using LF12.Classes.Classes;
using LF12.Classes.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using Tesseract;

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

        [HttpGet]
        public IActionResult SingleQuestions()
        {
            return View(new SingleQuestionsModel()); 
        }

        [HttpPost]
        public IActionResult SolveRiddle(SingleQuestionsModel model)
        {
                string question = model.Question;
                int answerLength = model.AnswerLength;

                string answer = FindAnswer(question, answerLength);
                model.Answer = answer;
            
            return View("SingleQuestions", model); // Rückgabe an die SingleQuestion-View
        }


        private string FindAnswer(string question, int answerLength)
        {
            return "Beispielantwort";
        }
    }
}
