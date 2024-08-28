using LF12.Classes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Hosting;

namespace LF12.Pages
{
    public class TestModel : BaseModel
    {
        protected string Lol = "LOLOLOLOLOL";
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
        public string penis()
        {
            return Lol;
        }
        public async Task OnPostAsync()
        {
            this.Lol = "nö";
            var x = 4;
            return;
        }
        protected override string GetTitle()
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
