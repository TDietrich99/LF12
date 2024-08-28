using Microsoft.AspNetCore.Mvc.RazorPages;

namespace LF12.Classes
{
    public class BaseModel:PageModel
    {
        protected virtual string GetTitle()
        {
            return "Basistitel";
        }
    }
}
