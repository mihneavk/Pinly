using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Pinly.Models
{
    public class CommentLike
    {
        public int Id { get; set; }

        public int CommentId { get; set; }
        public virtual Comment Comment { get; set; }

        public string ApplicationUserId { get; set; }
        public virtual ApplicationUser ApplicationUser { get; set; }
    }
}