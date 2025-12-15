using System.ComponentModel.DataAnnotations;

namespace Pinly.Models
{
    // Clasa GroupMembership implementează tabela de legatura N:M
    public class GroupMembership
    {
        // Cheile primare compuse vor fi definite în AppDbContext
        
        // FK către Group
        public int GroupId { get; set; } 
        public virtual Group? Group { get; set; }

        // FK către ApplicationUser
        public string ApplicationUserId { get; set; } 
        public virtual ApplicationUser? ApplicationUser { get; set; }

        public DateTime JoinedAt { get; set; }
        
        // Camp necesar pentru a gestiona acceptarea/respingerea cererilor de aderare
        public bool IsAccepted { get; set; } = false; 
    }
}