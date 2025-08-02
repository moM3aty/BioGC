using BioGC.Models;
using System.Collections.Generic;

namespace BioGC.ViewModels
{
    public class ProductListViewModel
    {
        public IEnumerable<Product> Products { get; set; }


        public List<Category> ParentCategories { get; set; }

        public List<Category> SubCategoryTabs { get; set; }

        public int? SelectedParentCategoryId { get; set; }
        public int? SelectedSubCategoryId { get; set; }

        public string SearchTerm { get; set; }
        public string SortBy { get; set; }
    }
}
