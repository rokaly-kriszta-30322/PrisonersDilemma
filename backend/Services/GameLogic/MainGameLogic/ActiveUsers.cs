public class ActiveUsers
{
    private readonly Dictionary<int, bool> _activeUsers = new();
    private readonly object _lock = new();
    private readonly HashSet<int> _busyUsers = new();
    private readonly Dictionary<int, bool> _botInitiationMode = new();
    private readonly Dictionary<int, bool> _botOpponentMode = new();
    private readonly Dictionary<int, int> _botTargetIndex = new();


    public int? GetNextOpponentInOrder(int botId, List<int> sortedCandidateIds)
    {
        lock (_lock)
        {
            if (!sortedCandidateIds.Any())
                return null;

            int lastOpponent = _botTargetIndex.ContainsKey(botId) ? _botTargetIndex[botId] : -1;

            var next = sortedCandidateIds.FirstOrDefault(id => id > lastOpponent);

            if (next == 0)
                next = sortedCandidateIds.First();

            _botTargetIndex[botId] = next;

            return next;
        }
    }

    public void SetBotBehavior(int userId, bool activeMode, bool chaosMode)
    {
        lock (_lock)
        {
            _botInitiationMode[userId] = activeMode;
            _botOpponentMode[userId] = chaosMode;
        }
    }

    public bool IsBotActiveMode(int userId)
    {
        lock (_lock)
        {
            return _botInitiationMode.TryGetValue(userId, out bool active) && active;
        }
    }

    public bool IsBotChaosMode(int userId)
    {
        lock (_lock)
        {
            return _botOpponentMode.TryGetValue(userId, out bool chaos) && chaos;
        }
    }

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
            bool isBot = _activeUsers.TryGetValue(userId, out var botFlag) && botFlag;

            _activeUsers.Remove(userId);
            _busyUsers.Remove(userId);
            if (isBot)
            {
                _botTargetIndex.Remove(userId);
            }
        }
    }

    public List<int> GetActiveUserIds()
    {
        lock (_lock)
        {
            return _activeUsers.Keys.ToList();
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
