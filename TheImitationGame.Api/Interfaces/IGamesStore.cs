using TheImitationGame.Api.Models;

public interface IGamesStore
{
    bool TryAdd(string key, Game value);
    bool TryRemove(string key, out Game? value);
    bool TryUpdate(string key, Game newValue, Game comparisonValue);
    bool TryGetValue(string key, out Game? value);
    bool Any(Func<KeyValuePair<string, Game>, bool> predicate);
    KeyValuePair<string, Game> FirstOrDefault(Func<KeyValuePair<string, Game>, bool> predicate);
    void Clear();
}