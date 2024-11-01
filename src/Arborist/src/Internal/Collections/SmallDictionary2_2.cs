using System.Diagnostics.CodeAnalysis;

namespace Arborist.Internal.Collections;

/// <summary>
/// <see cref="SmallDictionary{K,V}"/> implementation having exactly 2 entries.
/// </summary>
public sealed class SmallDictionary2<K, V> : SmallDictionary<K, V>
    where K : notnull
{
    private readonly KeyValuePair<K, V> _e0;
    private readonly KeyValuePair<K, V> _e1;

    public SmallDictionary2(
        IEqualityComparer<K> keyComparer,
        KeyValuePair<K, V> e0,
        KeyValuePair<K, V> e1
    ) : base(keyComparer) {
        if(keyComparer.Equals(e1.Key, e0.Key))
            throw DuplicateKeyException(e1.Key);
            
        _e0 = e0;
        _e1 = e1;
    }

    public override int Count => 2;
    
    public override bool TryGetValue(K key, [MaybeNullWhen(false)] out V value) {
        if(KeyComparer.Equals(_e0.Key, key)) {
            value = _e0.Value;
            return true;
        } else if(KeyComparer.Equals(_e1.Key, key)) {
            value = _e1.Value;
            return true;
        } else {
            value = default;
            return false;
        }
    }
    
    protected override KeyValuePair<K, V> GetEnumeratedEntry(int index) => index switch {
        0 => _e0,
        1 => _e1,
        _ => throw new InvalidOperationException()
    };

    public override SmallDictionary<K, V> Add(KeyValuePair<K, V> entry) =>
        SmallDictionary.Create(KeyComparer, _e0, _e1, entry);
        
    public override SmallDictionary<K, V> AddRange(IEnumerable<KeyValuePair<K, V>> entries) {
        if(entries is IReadOnlyList<KeyValuePair<K, V>> list)
            switch(list.Count) {
                case 0: return this;
                case 1: return SmallDictionary.Create(KeyComparer, _e0, _e1, list[0]);
                case 2: return SmallDictionary.Create(KeyComparer, _e0, _e1, list[0], list[1]);
            }
            
        return AddRangeEnumerated(entries);
    }

    public override IEnumerator<KeyValuePair<K, V>> GetEnumerator() =>
        new Enumerator(this);
}
