using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

public class RoleHandler
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly MyDbContext _dbContext;

    public RoleHandler(IHttpContextAccessor httpContextAccessor, MyDbContext dbContext)
    {
        _httpContextAccessor = httpContextAccessor;
        _dbContext = dbContext;
    }

    public async Task<(int?, int?)> ResolveIdsAsync(string userName2, string userName1)
    {
        var user = await _dbContext.user_data.FirstOrDefaultAsync(u => u.UserName == userName1);
        var target = await _dbContext.user_data.FirstOrDefaultAsync(u => u.UserName == userName2);

        return (user?.UserId, target?.UserId);
    }

}
