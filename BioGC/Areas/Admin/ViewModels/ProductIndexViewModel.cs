using BioGC.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;

namespace BioGC.Areas.Admin.ViewModels
{

    public class ProductIndexViewModel
    {
        public IEnumerable<Product> Products { get; set; }
        public SelectList Categories { get; set; }

        public string SearchTerm { get; set; }
        public int? CategoryId { get; set; }
    }
}
