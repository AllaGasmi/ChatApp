namespace ChatAppProj.Models;

public class UserConfiguration {
    public int Id { get; set; }
    public bool AllowRequest { get; set; }
    public bool AllowBeingAddedToGroup { get; set; }
    public bool AllowOnlyFriendsChat { get; set; }
}