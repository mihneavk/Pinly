using Pinly.Models;

namespace Pinly.ViewModels
{
    public class ProfileViewModel
    {
        public ApplicationUser User { get; set; }
        public List<Pin> Pins { get; set; }
        public bool IsOwner { get; set; } // Verificăm dacă eu sunt proprietarul profilului
    }
}