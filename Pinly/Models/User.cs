using System.ComponentModel.DataAnnotations;
using System.Net.NetworkInformation;

namespace Pinly.Models
{
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Numele de utilizator este obligatoriu")]
        public string Username { get; set; }

        [Required(ErrorMessage = "Email-ul este obligatoriu")]
        public string Email { get; set; }

        public virtual ICollection<Pin>? Pins { get; set; }
    }
}