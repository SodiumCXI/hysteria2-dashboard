using Hysteria2Dashboard.Domain.Entities;

namespace Hysteria2Dashboard.Domain.Interfaces;

public interface IUserRepository
{
    Task<List<User>> GetAllUsersAsync();
    Task AddUserAsync(User user);
    Task DeleteUserAsync(string user);
}
