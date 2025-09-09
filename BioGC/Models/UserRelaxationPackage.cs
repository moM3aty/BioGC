namespace BioGC.Models
{
    /// <summary>
    /// Represents the join table for the many-to-many relationship
    /// between users and the relaxation packages they have purchased/subscribed to.
    /// </summary>
    public class UserRelaxationPackage
    {
        public string ApplicationUserId { get; set; }
        public virtual ApplicationUser User { get; set; }

        public int RelaxationPackageId { get; set; }
        public virtual RelaxationPackage Package { get; set; }
    }
}
