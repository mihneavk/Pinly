using System;
using System.ComponentModel.DataAnnotations;

namespace Pinly.Models
{
    public class Notification
    {
        public int Id { get; set; }

        public string Text { get; set; }
        public string Type { get; set; } // "Like", "Follow", "Request"
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public bool IsRead { get; set; } = false;

        // Cine a generat notificarea (ex: cel care a dat like)
        public string SenderId { get; set; }
        public virtual ApplicationUser Sender { get; set; }

        // Cine primeste notificarea
        public string RecipientId { get; set; }
        public virtual ApplicationUser Recipient { get; set; }

        // Optional: Pin-ul legat de notificare (pt Like)
        public int? PinId { get; set; }
        public virtual Pin Pin { get; set; }
    }
}