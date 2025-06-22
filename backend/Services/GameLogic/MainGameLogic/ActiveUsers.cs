public class ActiveUsers
{
    private readonly Dictionary<int, bool> _activeUsers = new();
    private readonly object _lock = new();
    private readonly HashSet<int> _busyUsers = new();

    public void AddUser(int userId, bool isBot)
    {
        lock (_lock)
        {
            _activeUsers[userId] = isBot;
        }
    }

    public void RemoveUser(int userId)
    {
        lock (_lock)
        {
            _activeUsers.Remove(userId);
            _busyUsers.Remove(userId);
        }
    }

    public List<int> GetActiveUserIds()
    {
        lock (_lock)
        {
            return _activeUsers.Keys.ToList();
        }
    }

    public List<int> GetActiveBotIds()
    {
        lock (_lock)
        {
            return _activeUsers.Where(kv => kv.Value).Select(kv => kv.Key).ToList();
        }
    }

    public List<int> GetActivePlayerIds()
    {
        lock (_lock)
        {
            return _activeUsers.Where(kv => !kv.Value).Select(kv => kv.Key).ToList();
        }
    }

    public bool IsUserBot(int userId)
    {
        lock (_lock)
        {
            return _activeUsers.TryGetValue(userId, out var isBot) && isBot;
        }
    }

    public bool IsUserActive(int userId)
    {
        lock (_lock)
        {
            return _activeUsers.ContainsKey(userId);
        }
    }

    public bool IsUserBusy(int userId)
    {
        lock (_lock)
        {
            return _busyUsers.Contains(userId);
        }
    }

    public void SetUserBusy(int userId)
    {
        lock (_lock)
        {
            _busyUsers.Add(userId);
        }
    }

    public void SetUserFree(int userId)
    {
        lock (_lock)
        {
            _busyUsers.Remove(userId);
        }
    }
}
