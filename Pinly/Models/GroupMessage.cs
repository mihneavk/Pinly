using System.ComponentModel.DataAnnotations;

namespace Pinly.Models
{
    public class GroupMessage
    {
        public int Id { get; set; }

        [Required]
        public string Content { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public bool IsEdited { get; set; } = false;
        public bool IsDeleted { get; set; } = false;

        public string SenderId { get; set; }
        public virtual ApplicationUser Sender { get; set; }

        public int GroupId { get; set; }
        public virtual Group Group { get; set; }
    }
}