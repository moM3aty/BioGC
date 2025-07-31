using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;

namespace BioGC.Areas.Admin.ViewModels
{
    // ViewModel to represent a single user in the list
    public class UserViewModel
    {
        public string Id { get; set; }
        public string UserName { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public IEnumerable<string> Roles { get; set; }
    }

    // ViewModel for the main user index page, including filters
    public class UserIndexViewModel
    {
        public IEnumerable<UserViewModel> Users { get; set; }
        public SelectList Roles { get; set; }
        public string SearchTerm { get; set; }
        public string Role { get; set; }
    }

    // ViewModel for the Manage Roles page for a specific user
    public class ManageUserRolesViewModel
    {
        public string UserId { get; set; }
        public string UserName { get; set; }
        public List<RoleViewModel> Roles { get; set; }
    }

    // Represents a single role checkbox in the Manage Roles view
    public class RoleViewModel
    {
        public string RoleId { get; set; }
        public string RoleName { get; set; }
        public bool IsSelected { get; set; }
    }
}
