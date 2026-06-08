using Hysteria2Dashboard.Domain.Entities;

namespace Hysteria2Dashboard.Domain.Interfaces;

public interface IUserRepository
{
    Task<List<User>> GetAllUsersAsync();
    Task AddUserAsync(User user);
    Task<bool> ExistsAsync(string username);
    Task DeleteUserAsync(string user);
}
