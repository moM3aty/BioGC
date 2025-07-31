using BioGC.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;

namespace BioGC.ViewModels
{
    public class HomeViewModel
    {
        public List<Product> FeaturedProducts { get; set; }

    }
}
