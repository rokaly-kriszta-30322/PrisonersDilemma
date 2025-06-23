using Microsoft.AspNetCore.SignalR;

public class GameHub : Hub
{
    public async Task SendUpdate(string userId)
    {
        await Clients.User(userId).SendAsync("ReceiveUpdate");
    }
}