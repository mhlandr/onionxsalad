using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;

namespace onion.Views.Home
{
    public class ForensicsToolsModel : PageModel
    {
        [BindProperty]
        public string WebsiteUrl { get; set; }
        public bool IsPost { get; set; }

        public void OnGet()
        {
            IsPost = false;
        }

        public void OnPostSubmit()
        {
            IsPost = true;
            // You can add preliminary processing here if needed
        }
    }
}