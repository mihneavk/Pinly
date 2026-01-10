using System.ComponentModel.DataAnnotations;

namespace Pinly.Models
{
    public class Notification
    {
        public int Id { get; set; }
        public string Text { get; set; }
        public string Type { get; set; } // "Like", "Comment", "Follow", "Request", "Message", "Group"
        public bool IsRead { get; set; } = false;
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public string SenderId { get; set; }
        public virtual ApplicationUser Sender { get; set; }

        public string RecipientId { get; set; }
        public virtual ApplicationUser Recipient { get; set; }

        public int? PinId { get; set; }
        public virtual Pin? Pin { get; set; }
        public int? GroupId { get; set; }
        public virtual Group? Group { get; set; }
    }
}