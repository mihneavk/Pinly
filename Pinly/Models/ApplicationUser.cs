using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace Pinly.Models
{
    public class ApplicationUser : IdentityUser
    {
        [Required(ErrorMessage = "Numele complet este obligatoriu")]
        [StringLength(100, ErrorMessage = "Numele nu poate depăși 100 de caractere")]
        public string FullName { get; set; }

        [StringLength(500, ErrorMessage = "Descrierea nu poate depăși 500 de caractere")]
        public string? Description { get; set; }

        public string? ProfilePicturePath { get; set; } // calea catre imaginea din wwwroot

        public bool IsPublic { get; set; } = true;
    }
}