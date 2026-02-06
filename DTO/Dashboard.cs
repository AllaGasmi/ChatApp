namespace ChatAppProj.Models;

public class DashboardStatsDto
{
    public int NewMessages { get; set; }
    public int ActiveChats { get; set; }
    public int UnreadMessages { get; set; }
    public int FriendsOnline { get; set; }
}

public class RecentConversationDto
{
    public int ConversationId { get; set; }
    public string Name { get; set; }
    public string AvatarUrl { get; set; }
    public string LastMessage { get; set; }
    public string TimeAgo { get; set; }
    public int UnreadCount { get; set; }
    public bool IsOnline { get; set; }
}