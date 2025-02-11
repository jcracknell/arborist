using System.Diagnostics.CodeAnalysis;

namespace Arborist.Internal.Collections;

/// <summary>
/// <see cref="SmallDictionary{K,V}"/> implementation having exactly 0 entries.
/// </summary>
public sealed class SmallDictionary0<K, V> : SmallDictionary<K, V>
    where K : notnull
{
    internal static SmallDictionary0<K, V> Instance { get; } = new(EqualityComparer<K>.Default);

    private static Enumerator EnumeratorInstance { get; } = new(Instance);

    public SmallDictionary0(IEqualityComparer<K> keyComparer)
        : base(keyComparer)
    { }

    public override IEnumerator<KeyValuePair<K, V>> GetEnumerator() =>
        EnumeratorInstance;

    public override int Count => 0;

    public override bool TryGetValue(K key, [MaybeNullWhen(false)] out V value) {
        value = default;
        return false;
    }

    protected override KeyValuePair<K, V> GetEnumeratedEntry(int index) =>
        throw new InvalidOperationException();

    public override SmallDictionary<K, V> Add(KeyValuePair<K, V> entry) =>
        SmallDictionary.Create(KeyComparer, entry);

    public override SmallDictionary<K, V> AddRange(IEnumerable<KeyValuePair<K, V>> entries) {
        if(entries is IReadOnlyList<KeyValuePair<K, V>> list)
            switch(list.Count) {
                case 0: return this;
                case 1: return SmallDictionary.Create(KeyComparer, list[0]);
                case 2: return SmallDictionary.Create(KeyComparer, list[0], list[1]);
                case 3: return SmallDictionary.Create(KeyComparer, list[0], list[1], list[2]);
                case 4: return SmallDictionary.Create(KeyComparer, list[0], list[1], list[2], list[3]);
            }

        return AddRangeEnumerated(entries);
    }
}
