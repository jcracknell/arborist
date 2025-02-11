using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace Arborist.Internal.Collections;

/// <summary>
/// <see cref="SmallDictionary{K,V}"/> implementation having any number of entries.
/// </summary>
public sealed class SmallDictionaryN<K, V> : SmallDictionary<K, V>
    where K : notnull
{
    private readonly ImmutableDictionary<K, V> _dictionary;

    public SmallDictionaryN(ImmutableDictionary<K, V> dictionary)
        : base(dictionary.KeyComparer)
    {
        _dictionary = dictionary;
    }

    public override int Count => _dictionary.Count;

    public override bool TryGetValue(K key, [MaybeNullWhen(false)] out V value) =>
        _dictionary.TryGetValue(key, out value);

    protected override KeyValuePair<K, V> GetEnumeratedEntry(int index) =>
        throw new NotImplementedException($"{typeof(SmallDictionaryN<K, V>).Name}.{nameof(GetEnumeratedEntry)}");

    public override SmallDictionary<K, V> Add(KeyValuePair<K, V> entry) =>
        new SmallDictionaryN<K, V>(_dictionary.Add(entry.Key, entry.Value));

    public override SmallDictionary<K, V> AddRange(IEnumerable<KeyValuePair<K, V>> entries) =>
        new SmallDictionaryN<K, V>(_dictionary.AddRange(entries));

    public override IEnumerator<KeyValuePair<K, V>> GetEnumerator() =>
        _dictionary.GetEnumerator();
}
