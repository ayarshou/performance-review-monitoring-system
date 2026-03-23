using PerformanceReviewApi.Models;

namespace PerformanceReviewApi.Repositories;

public interface IUserRepository
{
    Task<User?> GetByUsernameAsync(string username);
    Task<User> CreateAsync(User user);
}
