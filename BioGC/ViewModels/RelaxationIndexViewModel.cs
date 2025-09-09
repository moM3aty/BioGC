using BioGC.Models;
using System.Collections.Generic;
using System.Linq;

namespace BioGC.ViewModels
{
    public class RelaxationIndexViewModel
    {
        public IEnumerable<IGrouping<Category, RelaxationPackage>> AllPackages { get; set; }

        /// <summary>
        /// A set of IDs for packages that the user has been approved for.
        /// </summary>
        public HashSet<int> ApprovedPackageIds { get; set; } = new HashSet<int>();

        /// <summary>
        /// A set of IDs for packages that the user has paid for but are awaiting admin approval.
        /// </summary>
        public HashSet<int> PendingPackageIds { get; set; } = new HashSet<int>();
    }
}

