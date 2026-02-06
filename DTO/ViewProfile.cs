namespace ChatAppProj.DTO
{
    public class ViewProfileDto
    {
        public string UserId { get; set; }
        public string DisplayName { get; set; }
        public string? Email { get; set; }
        public string ProfilePicture { get; set; }
        public string? Bio { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string Status { get; set; }
        public bool IsFriend { get; set; }
        public bool HasBlocked { get; set; }
        public bool IsBlocked { get; set; }
        public bool HasPendingRequest { get; set; }
        public bool CanSendFriendRequest { get; set; }
    }
}