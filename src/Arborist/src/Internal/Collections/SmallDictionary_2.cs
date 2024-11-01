using System.Collections;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace Arborist.Internal.Collections;

/// <summary>
/// <see cref="IReadOnlyDictionary{TKey,TValue}"/> implementation using linear searching requiring
/// only a single allocation for small numbers of entries.
/// </summary>
/// <seealso cref="SmallDictionary"/>
public abstract class SmallDictionary<K, V> : IReadOnlyDictionary<K, V>
    where K : notnull
{
    protected static ArgumentException DuplicateKeyException(K key) =>
        new($"An element with the same key already exists. Key: '{key}'");

    public static SmallDictionary<K, V> Empty { get; } = SmallDictionary0<K, V>.Instance;

    protected SmallDictionary(IEqualityComparer<K> keyComparer) {
        KeyComparer = keyComparer;
    }

    public IEqualityComparer<K> KeyComparer { get; }
    public abstract int Count { get; }
    public abstract bool TryGetValue(K key, [MaybeNullWhen(false)] out V value);
    public abstract SmallDictionary<K, V> Add(KeyValuePair<K, V> entry);
    public abstract SmallDictionary<K, V> AddRange(IEnumerable<KeyValuePair<K, V>> entries);
    public abstract IEnumerator<KeyValuePair<K, V>> GetEnumerator();

    public bool ContainsKey(K key) => TryGetValue(key, out _);

    public V this[K key] => TryGetValue(key, out var value) switch {
        true => value,
        false => throw new KeyNotFoundException()
    };

    protected abstract KeyValuePair<K, V> GetEnumeratedEntry(int index);

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    public IEnumerable<K> Keys => this.Select(kvp => kvp.Key);
    public IEnumerable<V> Values => this.Select(kvp => kvp.Value);

    protected SmallDictionary<K, V> AddRangeEnumerated(IEnumerable<KeyValuePair<K, V>> entries) {
        var builder = ImmutableDictionary.CreateBuilder<K, V>(KeyComparer);
        builder.AddRange(this);
        builder.AddRange(entries);

        return new SmallDictionaryN<K, V>(builder.ToImmutable());
    }

    protected sealed class Enumerator : IEnumerator<KeyValuePair<K, V>>
    {
        private readonly SmallDictionary<K, V> _dict;
        private int _index = -1;

        public Enumerator(SmallDictionary<K, V> dict) {
            _dict = dict;
        }

        public KeyValuePair<K, V> Current => _dict.GetEnumeratedEntry(_index);

        object IEnumerator.Current => Current;

        public void Dispose() { }

        public bool MoveNext() {
            return (_index += 1) < _dict.Count;
        }

        public void Reset() {
            _index = -1;
        }
    }
}
