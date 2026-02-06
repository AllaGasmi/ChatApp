namespace ChatAppProj.RepositoryContracts;
using ChatAppProj.Models;

public interface IFriendshipRepository : IGenericRepository<Friendship> {
    Friendship? GetFriendship(int user1, int user2);
    List<Friendship> GetAllFriendships(int user);
    List<ApplicationUser> GetAllFriends(int user);
    List<ApplicationUser> GetAllBlocked(int user);
    List<Friendship> GetAllPending(int user);
}