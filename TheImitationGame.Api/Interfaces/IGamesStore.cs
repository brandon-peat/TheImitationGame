public interface IGamesStore
{
    bool TryAdd(string key, string? value);
    bool TryRemove(string key, out string? value);
    bool TryUpdate(string key, string? newValue, string? comparisonValue);
    bool TryGetValue(string key, out string? value);
    bool Any(Func<KeyValuePair<string, string?>, bool> predicate);
    KeyValuePair<string, string?> FirstOrDefault(Func<KeyValuePair<string, string?>, bool> predicate);
    void Clear();
}