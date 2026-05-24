using Hysteria2Dashboard.Application.DTOs;

namespace Hysteria2Dashboard.Application.Services.Interfaces;

public interface IUserService
{
    Task<List<UserDto>> GetUsersAsync();
    Task<UserDto> CreateUserAsync(CreateUserDto dto);
    Task RemoveUserAsync(string username);
}
