using System.ComponentModel.DataAnnotations.Schema;

namespace Pinly.Models
{
    public class Follow
    {
        // Userul care dă follow (Urmăritorul)
        public string FollowerId { get; set; }
        public virtual ApplicationUser Follower { get; set; }

        // Userul care este urmărit
        public string FolloweeId { get; set; }
        public virtual ApplicationUser Followee { get; set; }
    }
}