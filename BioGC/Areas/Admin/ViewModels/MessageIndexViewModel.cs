using BioGC.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BioGC.Areas.Admin.ViewModels
{
    public class MessageIndexViewModel
    {
        public IEnumerable<ContactMessage> Messages { get; set; }
        public SelectList SourcePages { get; set; }

        public string SearchTerm { get; set; }
        public string SourcePage { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }
}
