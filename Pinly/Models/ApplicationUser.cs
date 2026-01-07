using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace Pinly.Models
{
    public class ApplicationUser : IdentityUser
    {
        [Required(ErrorMessage = "Numele complet este obligatoriu")]
        [StringLength(100, ErrorMessage = "Numele nu poate depasi 100 de caractere")]
        public string FullName { get; set; }

        [StringLength(500, ErrorMessage = "Descrierea nu poate depasi 500 de caractere")]
        public string? Description { get; set; }

        public string? ProfilePicturePath { get; set; }

        // Default e public
        public bool IsPrivate { get; set; } = false;

        // Proprietate veche (poti sa o stergi daca nu o mai folosesti, sau o lasi)
        public bool IsPublic { get; set; } = true;

        public virtual ICollection<Pin>? Pins { get; set; }
        public virtual ICollection<Comment>? Comments { get; set; }
        public virtual ICollection<GroupMessage>? GroupMessages { get; set; }
        public virtual ICollection<Group>? ModeratedGroups { get; set; }
        public virtual ICollection<Reaction>? Reactions { get; set; }
        public virtual ICollection<GroupMembership>? Memberships { get; set; }
        public virtual ICollection<Follow>? Followers { get; set; }
        public virtual ICollection<Follow>? Following { get; set; }
    }
}