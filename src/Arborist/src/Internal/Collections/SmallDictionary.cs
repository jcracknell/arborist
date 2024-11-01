using System.Collections.Immutable;

namespace Arborist.Internal.Collections;

/// <summary>
/// Static factory methods for <see cref="SmallDictionary{K,V}"/> instances.
/// </summary>
/// <seealso cref="SmallDictionary{K,V}"/>
public static class SmallDictionary {
    /// <summary>
    /// Creates a <see cref="SmallDictionary{K,V}"/> instance from the provided entries.
    /// </summary>
    public static SmallDictionary<K, V> Create<K, V>(
        KeyValuePair<K, V> e0
    ) where K : notnull =>
        Create(EqualityComparer<K>.Default, e0);

    /// <summary>
    /// Creates a <see cref="SmallDictionary{K,V}"/> instance from the provided entries.
    /// </summary>
    public static SmallDictionary<K, V> Create<K, V>(
        KeyValuePair<K, V> e0,
        KeyValuePair<K, V> e1
    ) where K : notnull =>
        Create(EqualityComparer<K>.Default, e0, e1);

    /// <summary>
    /// Creates a <see cref="SmallDictionary{K,V}"/> instance from the provided entries.
    /// </summary>
    public static SmallDictionary<K, V> Create<K, V>(
        KeyValuePair<K, V> e0,
        KeyValuePair<K, V> e1,
        KeyValuePair<K, V> e2
    ) where K : notnull =>
        Create(EqualityComparer<K>.Default, e0, e1, e2);

    /// <summary>
    /// Creates a <see cref="SmallDictionary{K,V}"/> instance from the provided entries.
    /// </summary>
    public static SmallDictionary<K, V> Create<K, V>(
        KeyValuePair<K, V> e0,
        KeyValuePair<K, V> e1,
        KeyValuePair<K, V> e2,
        KeyValuePair<K, V> e3
    ) where K : notnull =>
        Create(EqualityComparer<K>.Default, e0, e1, e2, e3);

    /// <summary>
    /// Creates an empty <see cref="SmallDictionary{K,V}"/> instance using the provided
    /// <paramref name="keyComparer"/>.
    /// </summary>
    public static SmallDictionary<K, V> Create<K, V>(IEqualityComparer<K> keyComparer) where K : notnull =>
        ReferenceEquals(SmallDictionary<K, V>.Empty.KeyComparer, keyComparer)
        ? SmallDictionary<K, V>.Empty
        : new SmallDictionary0<K, V>(keyComparer);

    /// <summary>
    /// Creates a <see cref="SmallDictionary{K,V}"/> instance using the provided <paramref name="keyComparer"/>
    /// and entries.
    /// </summary>
    public static SmallDictionary<K, V> Create<K, V>(
        IEqualityComparer<K> keyComparer,
        KeyValuePair<K, V> e0
    ) where K : notnull =>
        new SmallDictionary1<K, V>(keyComparer, e0);

    /// <summary>
    /// Creates a <see cref="SmallDictionary{K,V}"/> instance using the provided <paramref name="keyComparer"/>
    /// and entries.
    /// </summary>
    public static SmallDictionary<K, V> Create<K, V>(
        IEqualityComparer<K> keyComparer,
        KeyValuePair<K, V> e0,
        KeyValuePair<K, V> e1
    ) where K : notnull =>
        new SmallDictionary2<K, V>(keyComparer, e0, e1);

    /// <summary>
    /// Creates a <see cref="SmallDictionary{K,V}"/> instance using the provided <paramref name="keyComparer"/>
    /// and entries.
    /// </summary>
    public static SmallDictionary<K, V> Create<K, V>(
        IEqualityComparer<K> keyComparer,
        KeyValuePair<K, V> e0,
        KeyValuePair<K, V> e1,
        KeyValuePair<K, V> e2
    ) where K : notnull =>
        new SmallDictionary3<K, V>(keyComparer, e0, e1, e2);

    /// <summary>
    /// Creates a <see cref="SmallDictionary{K,V}"/> instance using the provided <paramref name="keyComparer"/>
    /// and entries.
    /// </summary>
    public static SmallDictionary<K, V> Create<K, V>(
        IEqualityComparer<K> keyComparer,
        KeyValuePair<K, V> e0,
        KeyValuePair<K, V> e1,
        KeyValuePair<K, V> e2,
        KeyValuePair<K, V> e3
    ) where K : notnull =>
        new SmallDictionary4<K, V>(keyComparer, e0, e1, e2, e3);

    /// <summary>
    /// Creates a <see cref="SmallDictionary{K,V}"/> instance from the provided <paramref name="entries"/>.
    /// </summary>
    public static SmallDictionary<K, V> Create<K, V>(params KeyValuePair<K, V>[] entries)
        where K : notnull =>
        CreateRange(EqualityComparer<K>.Default, entries);

    /// <summary>
    /// Creates a <see cref="SmallDictionary{K,V}"/> instance using the provided <paramref name="keyComparer"/>
    /// and <paramref name="entries"/>.
    /// </summary>
    public static SmallDictionary<K, V> Create<K, V>(
        IEqualityComparer<K> keyComparer,
        params KeyValuePair<K, V>[] entries
    ) where K : notnull =>
        CreateRange(keyComparer, entries);

    /// <summary>
    /// Creates a <see cref="SmallDictionary{K,V}"/> instance from the provided <paramref name="entries"/>.
    /// </summary>
    public static SmallDictionary<K, V> CreateRange<K, V>(IEnumerable<KeyValuePair<K, V>> entries)
        where K : notnull =>
        CreateRange(EqualityComparer<K>.Default, entries);

    /// <summary>
    /// Creates a <see cref="SmallDictionary{K,V}"/> instance using the provided <paramref name="keyComparer"/>
    /// and <paramref name="entries"/>.
    /// </summary>
    public static SmallDictionary<K, V> CreateRange<K, V>(
        IEqualityComparer<K> keyComparer,
        IEnumerable<KeyValuePair<K, V>> entries
    ) where K : notnull =>
        entries is IReadOnlyList<KeyValuePair<K, V>> list
        ? CreateList(keyComparer, list)
        : CreateEnumerated(keyComparer, entries);

    private static SmallDictionary<K, V> CreateList<K, V>(
        IEqualityComparer<K> keyComparer,
        IReadOnlyList<KeyValuePair<K, V>> entries
    ) where K : notnull =>
        entries.Count switch {
            0 => Create<K, V>(keyComparer),
            1 => Create(keyComparer, entries[0]),
            2 => Create(keyComparer, entries[0], entries[1]),
            3 => Create(keyComparer, entries[0], entries[1], entries[2]),
            4 => Create(keyComparer, entries[0], entries[1], entries[2], entries[3]),
            _ => new SmallDictionaryN<K, V>(ImmutableDictionary.CreateRange(keyComparer, entries))
        };

    private static SmallDictionary<K, V> CreateEnumerated<K, V>(
        IEqualityComparer<K> keyComparer,
        IEnumerable<KeyValuePair<K, V>> entries
    ) where K : notnull {
        using var enumerator = entries.GetEnumerator();

        if(!enumerator.MoveNext())
            return Create<K, V>(keyComparer);

        var e0 = enumerator.Current;
        if(!enumerator.MoveNext())
            return Create(keyComparer, e0);

        var e1 = enumerator.Current;
        if(!enumerator.MoveNext())
            return Create(keyComparer, e0, e1);

        var e2 = enumerator.Current;
        if(!enumerator.MoveNext())
            return Create(keyComparer, e0, e1, e2);

        var e3 = enumerator.Current;
        if(!enumerator.MoveNext())
            return Create(keyComparer, e0, e1, e2, e3);

        var builder = ImmutableDictionary.CreateBuilder<K, V>(keyComparer);
        builder.Add(e0);
        builder.Add(e1);
        builder.Add(e2);
        builder.Add(e3);

        do {
            builder.Add(enumerator.Current);
        } while(enumerator.MoveNext());

        return new SmallDictionaryN<K, V>(builder.ToImmutable());
    }
}
