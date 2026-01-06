using System.ComponentModel.DataAnnotations;

namespace Pinly.ViewModels
{
    public class PinCreateViewModel
    {
        [Required(ErrorMessage = "Titlul este obligatoriu")]
        public string Title { get; set; }

        public string? Description { get; set; }

        [Required(ErrorMessage = "Te rog să încarci o imagine")]
        [Display(Name = "Imagine Pin")]
        public IFormFile Image { get; set; } // Proprietate pentru upload fișier
    }
}