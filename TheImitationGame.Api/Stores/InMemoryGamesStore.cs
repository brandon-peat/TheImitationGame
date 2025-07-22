using System.Collections.Concurrent;
using TheImitationGame.Api.Models;

namespace TheImitationGame.Api.Stores
{
    public class InMemoryGamesStore : IGamesStore
    {
        private readonly ConcurrentDictionary<string, Game> _games = new();

        public bool TryAdd(string key, Game value) => _games.TryAdd(key, value);
        public bool TryRemove(string key, out Game? value) => _games.TryRemove(key, out value);
        public bool TryUpdate(string key, Game newValue, Game comparisonValue) => _games.TryUpdate(key, newValue, comparisonValue);
        public bool TryGetValue(string key, out Game? value) => _games.TryGetValue(key, out value);
        public bool Any(Func<KeyValuePair<string, Game>, bool> predicate) => _games.Any(predicate);
        public KeyValuePair<string, Game> FirstOrDefault(Func<KeyValuePair<string, Game>, bool> predicate) => _games.FirstOrDefault(predicate);
        public void Clear() => _games.Clear();
    }
}