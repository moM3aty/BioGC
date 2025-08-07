using BioGC.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;

namespace BioGC.Areas.Admin.ViewModels
{

    public class OrderIndexViewModel
    {
        public IEnumerable<Order> Orders { get; set; }
        public SelectList Statuses { get; set; }

        public string SearchTerm { get; set; }
        public string Status { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }
  
}