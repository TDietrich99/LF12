using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace LF12.Pages
{
    public class TestModel : PageModel
    {
        private readonly ILogger<TestModel> _logger;

        public TestModel(ILogger<TestModel> logger)
        {
            _logger = logger;
        }
        public Uber[] GetUbers(int num = 10)
        {
            var ret = new Uber[num];
            for (int i = 0; i < num; i++)
            {
                ret[i] = new Uber($"Uber Nummer {i + 1}", $"{i+1}. Beschreibung");
            }
            return ret;
        }

        public string GetTitle()
        {
            return "Test";
        }
        
        public void OnGet()
        {

        }
    }
    public class Uber
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public Uber(string t, string d)
        {
            this.Title = t; this.Description = d;
        }
    }
}
