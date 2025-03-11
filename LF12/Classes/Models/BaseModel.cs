using Microsoft.AspNetCore.Mvc.RazorPages;

namespace LF12.Classes.Models
{
    public class BaseModel : PageModel
    {
        public virtual string GetTitle()
        {
            return "Basistitel";
        }
    }
}
