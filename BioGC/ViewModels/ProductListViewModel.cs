using BioGC.Models;
using System.Collections.Generic;

namespace BioGC.ViewModels
{
    public class ProductListViewModel
    {
        public IEnumerable<Product> Products { get; set; }
        public List<Category> AllCategories { get; set; } 
        public Category CurrentCategory { get; set; } 
        public string SearchTerm { get; set; }
        public string SortBy { get; set; }

    }
}
