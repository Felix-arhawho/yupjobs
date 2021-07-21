using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace StaticPages.Pages
{
    public class PostPageModel : PageModel
    {
        public string Id = string.Empty;
        public void OnGet(string postId)
        {
            Id = postId;
        }
    }
}
