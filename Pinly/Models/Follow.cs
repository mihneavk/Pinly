using System.ComponentModel.DataAnnotations.Schema;

namespace Pinly.Models
{
    public class Follow
    {
        public string FollowerId { get; set; }
        public virtual ApplicationUser Follower { get; set; }

        public string FolloweeId { get; set; }
        public virtual ApplicationUser Followee { get; set; }

        // True daca e acceptat, False daca e in asteptare
        public bool IsAccepted { get; set; } = true;
    }
}