using System.Diagnostics.CodeAnalysis;

namespace Arborist.Internal.Collections;

/// <summary>
/// <see cref="SmallDictionary{K,V}"/> implementation having exactly 1 entries.
/// </summary>
public sealed class SmallDictionary1<K, V> : SmallDictionary<K, V>
    where K : notnull
{
    private readonly KeyValuePair<K, V> _e0;

    public SmallDictionary1(
        IEqualityComparer<K> keyComparer,
        KeyValuePair<K, V> e0
    ) : base(keyComparer) {
        _e0 = e0;
    }

    public override int Count => 1;
    
    public override bool TryGetValue(K key, [MaybeNullWhen(false)] out V value) {
        if(KeyComparer.Equals(_e0.Key, key)) {
            value = _e0.Value;
            return true;
        } else {
            value = default;
            return false;
        }
    }

    protected override KeyValuePair<K, V> GetEnumeratedEntry(int index) => index switch {
        0 => _e0,
        _ => throw new InvalidOperationException()
    };

    public override SmallDictionary<K, V> Add(KeyValuePair<K, V> entry) =>
        SmallDictionary.Create(KeyComparer, _e0, entry);
        
    public override SmallDictionary<K, V> AddRange(IEnumerable<KeyValuePair<K, V>> entries) {
        if(entries is IReadOnlyList<KeyValuePair<K, V>> list)
            switch(list.Count) {
                case 0: return this;
                case 1: return SmallDictionary.Create(KeyComparer, _e0, list[0]);
                case 2: return SmallDictionary.Create(KeyComparer, _e0, list[0], list[1]);
                case 3: return SmallDictionary.Create(KeyComparer, _e0, list[0], list[1], list[2]);
            }
        
        return AddRangeEnumerated(entries);
    }

    public override IEnumerator<KeyValuePair<K, V>> GetEnumerator() =>
        new Enumerator(this);
}
