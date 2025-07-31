using BioGC.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;

namespace BioGC.Areas.Admin.ViewModels
{
    public class ReviewIndexViewModel
    {
        public IEnumerable<Review> Reviews { get; set; }
        public SelectList Statuses { get; set; }

        public string SearchTerm { get; set; }
        public string Status { get; set; }
    }
}