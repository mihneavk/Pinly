using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Pinly.Models
{
    // Clasa Reaction implementează tabela de legătură N:M
    public class Reaction
    {
        // Cheile primare compuse vor fi definite în ApplicationDbContext
        
        // FK către Pin (Pinul apreciat)
        public int PinId { get; set; } 
        public virtual Pin? Pin { get; set; }

        // FK către ApplicationUser (Utilizatorul care a dat like)
        public string ApplicationUserId { get; set; } 
        public virtual ApplicationUser? ApplicationUser { get; set; }

        public DateTime CreatedDate { get; set; }
    }
}

//Sa fortam un singur like per utilizator per postare:
//Modificam in ApplicationDbContext.cs