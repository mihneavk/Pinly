using Pinly.Models;

namespace Pinly.ViewModels
{
    public class ProfileViewModel
    {
        public ApplicationUser User { get; set; }
        public List<Pin> Pins { get; set; }

        public bool IsOwner { get; set; }

        // Relatii follow
        public bool IsFollowing { get; set; } // E acceptat?
        public bool HasPendingRequest { get; set; } // E in asteptare?

        public bool IsProfileAdmin { get; set; }

        public List<ApplicationUser> Followers { get; set; }
        public List<ApplicationUser> Following { get; set; }

        // Lista cereri (doar pt owner)
        public List<ApplicationUser> PendingRequests { get; set; }
    }
}