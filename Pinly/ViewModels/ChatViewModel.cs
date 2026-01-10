using Pinly.Models;

namespace Pinly.ViewModels
{
    public class ChatViewModel
    {
        public List<Group> MyGroups { get; set; }
        public int? CurrentGroupId { get; set; }
        public Group? CurrentGroup { get; set; }
        public List<GroupMessage> Messages { get; set; }
        public string CurrentUserId { get; set; }
    }
}