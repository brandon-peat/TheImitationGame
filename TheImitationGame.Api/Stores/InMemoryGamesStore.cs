using System.Collections.Concurrent;

public class InMemoryGamesStore : IGamesStore
{
    private readonly ConcurrentDictionary<string, string?> _games = new();

    public bool TryAdd(string key, string? value) => _games.TryAdd(key, value);
    public bool TryRemove(string key, out string? value) => _games.TryRemove(key, out value);
    public bool TryUpdate(string key, string? newValue, string? comparisonValue) => _games.TryUpdate(key, newValue, comparisonValue);
    public bool TryGetValue(string key, out string? value) => _games.TryGetValue(key, out value);
    public bool Any(Func<KeyValuePair<string, string?>, bool> predicate) => _games.Any(predicate);
    public KeyValuePair<string, string?> FirstOrDefault(Func<KeyValuePair<string, string?>, bool> predicate) => _games.FirstOrDefault(predicate);
    public void Clear() => _games.Clear();
}
