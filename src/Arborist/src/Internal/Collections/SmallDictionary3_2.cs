using System.Diagnostics.CodeAnalysis;

namespace Arborist.Internal.Collections;

/// <summary>
/// <see cref="SmallDictionary{K,V}"/> implementation having exactly 3 entries.
/// </summary>
public sealed class SmallDictionary3<K, V> : SmallDictionary<K, V>
    where K : notnull
{
    private readonly KeyValuePair<K, V> _e0;
    private readonly KeyValuePair<K, V> _e1;
    private readonly KeyValuePair<K, V> _e2;

    public SmallDictionary3(
        IEqualityComparer<K> keyComparer,
        KeyValuePair<K, V> e0,
        KeyValuePair<K, V> e1,
        KeyValuePair<K, V> e2
    ) : base(keyComparer) {
        if(keyComparer.Equals(e1.Key, e0.Key))
            throw DuplicateKeyException(e1.Key);
        if(keyComparer.Equals(e2.Key, e1.Key) || keyComparer.Equals(e2.Key, e0.Key))
            throw DuplicateKeyException(e2.Key);

        _e0 = e0;
        _e1 = e1;
        _e2 = e2;
    }

    public override int Count => 3;

    public override bool TryGetValue(K key, [MaybeNullWhen(false)] out V value) {
        if(KeyComparer.Equals(_e0.Key, key)) {
            value = _e0.Value;
            return true;
        } else if(KeyComparer.Equals(_e1.Key, key)) {
            value = _e1.Value;
            return true;
        } else if(KeyComparer.Equals(_e2.Key, key)) {
            value = _e2.Value;
            return true;
        } else {
            value = default;
            return false;
        }
    }

    protected override KeyValuePair<K, V> GetEnumeratedEntry(int index) => index switch {
        0 => _e0,
        1 => _e1,
        2 => _e2,
        _ => throw new InvalidOperationException()
    };

    public override SmallDictionary<K, V> Add(KeyValuePair<K, V> entry) =>
        SmallDictionary.Create(KeyComparer, _e0, _e1, _e2, entry);

    public override SmallDictionary<K, V> AddRange(IEnumerable<KeyValuePair<K, V>> entries) {
        if(entries is IReadOnlyList<KeyValuePair<K, V>> list)
            switch(list.Count) {
                case 0: return this;
                case 1: return SmallDictionary.Create(KeyComparer, _e0, _e1, _e2, list[0]);
            }

        return AddRangeEnumerated(entries);
    }

    public override IEnumerator<KeyValuePair<K, V>> GetEnumerator() =>
        new Enumerator(this);
}
