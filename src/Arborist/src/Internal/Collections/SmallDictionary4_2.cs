using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace Arborist.Internal.Collections;

/// <summary>
/// <see cref="SmallDictionary{K,V}"/> implementation having exactly 4 entries.
/// </summary>
public sealed class SmallDictionary4<K, V> : SmallDictionary<K, V>
    where K : notnull
{
    private readonly KeyValuePair<K, V> _e0;
    private readonly KeyValuePair<K, V> _e1;
    private readonly KeyValuePair<K, V> _e2;
    private readonly KeyValuePair<K, V> _e3;

    public SmallDictionary4(
        IEqualityComparer<K> keyComparer,
        KeyValuePair<K, V> e0,
        KeyValuePair<K, V> e1,
        KeyValuePair<K, V> e2,
        KeyValuePair<K, V> e3
    ) : base(keyComparer) {
        if(keyComparer.Equals(e1.Key, e0.Key))
            throw DuplicateKeyException(e1.Key);
        if(keyComparer.Equals(e2.Key, e1.Key) || keyComparer.Equals(e2.Key, e0.Key))
            throw DuplicateKeyException(e2.Key);
        if(keyComparer.Equals(e3.Key, e2.Key) || keyComparer.Equals(e3.Key, e1.Key) || keyComparer.Equals(e3.Key, e0.Key))
            throw DuplicateKeyException(e3.Key);
            
        _e0 = e0;
        _e1 = e1;
        _e2 = e2;
        _e3 = e3;
    }

    public override int Count => 4;
    
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
        } else if(KeyComparer.Equals(_e3.Key, key)) {
            value = _e3.Value;
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
        3 => _e3,
        _ => throw new InvalidOperationException()
    };

    public override SmallDictionary<K, V> Add(KeyValuePair<K, V> entry) {
        var builder = ImmutableDictionary.CreateBuilder<K, V>(KeyComparer);
        builder.AddRange(this);
        builder.Add(entry);
        return new SmallDictionaryN<K, V>(builder.ToImmutable());
    }
    
    public override SmallDictionary<K, V> AddRange(IEnumerable<KeyValuePair<K, V>> entries) {
        if(entries is IReadOnlyCollection<KeyValuePair<K, V>> { Count: 0 })
            return this;
        
        return AddRangeEnumerated(entries);
    }

    public override IEnumerator<KeyValuePair<K, V>> GetEnumerator() =>
        new Enumerator(this);
}
