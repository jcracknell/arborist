using System.Collections.Immutable;

namespace Arborist.Internal.Collections;

public class SmallDictionaryTests {
    private static SmallDictionary0<K, V> SmallDictionary0Factory<K, V>(
        IEqualityComparer<K> keyComparer,
        IReadOnlyList<KeyValuePair<K, V>> entries
    ) where K : notnull =>
        new SmallDictionary0<K, V>(keyComparer);
        
    private static SmallDictionary1<K, V> SmallDictionary1Factory<K, V>(
        IEqualityComparer<K> keyComparer,
        IReadOnlyList<KeyValuePair<K, V>> entries
    ) where K : notnull =>
        new SmallDictionary1<K, V>(keyComparer, entries[0]);
        
    private static SmallDictionary2<K, V> SmallDictionary2Factory<K, V>(
        IEqualityComparer<K> keyComparer,
        IReadOnlyList<KeyValuePair<K, V>> entries
    ) where K : notnull =>
        new SmallDictionary2<K, V>(keyComparer, entries[0], entries[1]);
        
    private static SmallDictionary3<K, V> SmallDictionary3Factory<K, V>(
        IEqualityComparer<K> keyComparer,
        IReadOnlyList<KeyValuePair<K, V>> entries
    ) where K : notnull =>
        new SmallDictionary3<K, V>(keyComparer, entries[0], entries[1], entries[2]);
        
    private static SmallDictionary4<K, V> SmallDictionary4Factory<K, V>(
        IEqualityComparer<K> keyComparer,
        IReadOnlyList<KeyValuePair<K, V>> entries
    ) where K : notnull =>
        new SmallDictionary4<K, V>(keyComparer, entries[0], entries[1], entries[2], entries[3]);
        
    private static SmallDictionaryN<K, V> SmallDictionaryNFactory<K, V>(
        IEqualityComparer<K> keyComparer,
        IReadOnlyList<KeyValuePair<K, V>> entries
    ) where K : notnull =>
        new SmallDictionaryN<K, V>(ImmutableDictionary.CreateRange(keyComparer, entries));

    [Fact]
    public void Create_with_0_should_return_the_empty_instance() {
        var instance = SmallDictionary.Create<string, string>();
        
        Assert.Same(SmallDictionary<string, string>.Empty, instance);
    }
    
    [Fact]
    public void Create_with_keyComparer_and_0_should_create_SmallDictionary0() {
        var instance = SmallDictionary.Create<string, string>(EqualityComparer<string>.Default);
        
        Assert.IsType<SmallDictionary0<string, string>>(instance);
    }
    
    [Fact]
    public void Create_with_keyComparer_and_1_should_create_SmallDictionary1() {
        var instance = SmallDictionary.Create(
            EqualityComparer<string>.Default,
            KeyValuePair.Create("a", "a")
        );
        
        Assert.IsType<SmallDictionary1<string, string>>(instance);
    }
    
    [Fact]
    public void Create_with_keyComparer_and_2_should_create_SmallDictionary2() {
        var instance = SmallDictionary.Create(
            EqualityComparer<string>.Default,
            KeyValuePair.Create("a", "a"),
            KeyValuePair.Create("b", "b")
        );
        
        Assert.IsType<SmallDictionary2<string, string>>(instance);
    }
    
    [Fact]
    public void Create_with_keyComparer_and_3_should_create_SmallDictionary3() {
        var instance = SmallDictionary.Create(
            EqualityComparer<string>.Default,
            KeyValuePair.Create("a", "a"),
            KeyValuePair.Create("b", "b"),
            KeyValuePair.Create("c", "c")
        );
        
        Assert.IsType<SmallDictionary3<string, string>>(instance);
    }
    
    [Fact]
    public void Create_with_keyComparer_and_4_should_create_SmallDictionary4() {
        var instance = SmallDictionary.Create(
            EqualityComparer<string>.Default,
            KeyValuePair.Create("a", "a"),
            KeyValuePair.Create("b", "b"),
            KeyValuePair.Create("c", "c"),
            KeyValuePair.Create("d", "d")
        );
        
        Assert.IsType<SmallDictionary4<string, string>>(instance);
    }
    
    [Fact]
    public void Create_with_keyComparer_and_5_should_create_SmallDictionaryN() {
        var instance = SmallDictionary.Create(
            EqualityComparer<string>.Default,
            KeyValuePair.Create("a", "a"),
            KeyValuePair.Create("b", "b"),
            KeyValuePair.Create("c", "c"),
            KeyValuePair.Create("d", "d"),
            KeyValuePair.Create("e", "e")
        );
        
        Assert.IsType<SmallDictionaryN<string, string>>(instance);
    }
    
    [Fact]
    public void SmallDictionary0_TryGetValue_should_work_as_expected() {
        var instance = new SmallDictionary0<string, string>(
            EqualityComparer<string>.Default
        );
        
        Assert.False(instance.TryGetValue("a", out _));
    }
    
    [Fact]
    public void SmallDictionary1_TryGetValue_should_work_as_expected() {
        var instance = new SmallDictionary1<string, string>(
            EqualityComparer<string>.Default,
            KeyValuePair.Create("a", "a")
        );
        
        Assert.True(instance.TryGetValue("a", out _));
        Assert.False(instance.TryGetValue("b", out _));
    }
    
    [Fact]
    public void SmallDictionary2_TryGetValue_should_work_as_expected() {
        var instance = new SmallDictionary2<string, string>(
            EqualityComparer<string>.Default,
            KeyValuePair.Create("a", "a"),
            KeyValuePair.Create("b", "b")
        );
        
        Assert.True(instance.TryGetValue("a", out _));
        Assert.True(instance.TryGetValue("b", out _));
        Assert.False(instance.TryGetValue("c", out _));
    }
    
    [Fact]
    public void SmallDictionary3_TryGetValue_should_work_as_expected() {
        var instance = new SmallDictionary3<string, string>(
            EqualityComparer<string>.Default,
            KeyValuePair.Create("a", "a"),
            KeyValuePair.Create("b", "b"),
            KeyValuePair.Create("c", "c")
        );
        
        Assert.True(instance.TryGetValue("a", out _));
        Assert.True(instance.TryGetValue("b", out _));
        Assert.True(instance.TryGetValue("c", out _));
        Assert.False(instance.TryGetValue("d", out _));
    }
    
    [Fact]
    public void SmallDictionary4_TryGetValue_should_work_as_expected() {
        var instance = new SmallDictionary4<string, string>(
            EqualityComparer<string>.Default,
            KeyValuePair.Create("a", "a"),
            KeyValuePair.Create("b", "b"),
            KeyValuePair.Create("c", "c"),
            KeyValuePair.Create("d", "d")
        );
        
        Assert.True(instance.TryGetValue("a", out _));
        Assert.True(instance.TryGetValue("b", out _));
        Assert.True(instance.TryGetValue("c", out _));
        Assert.True(instance.TryGetValue("d", out _));
        Assert.False(instance.TryGetValue("e", out _));
    }
    
    [Fact]
    public void SmallDictionaryN_TryGetValue_should_work_as_expected() {
        var instance = new SmallDictionaryN<string, string>(
            ImmutableDictionary.CreateRange(EqualityComparer<string>.Default, new[] {
                KeyValuePair.Create("a", "a"),
                KeyValuePair.Create("b", "b"),
                KeyValuePair.Create("c", "c"),
                KeyValuePair.Create("d", "d"),
                KeyValuePair.Create("e", "e")
            })
        );
        
        Assert.True(instance.TryGetValue("a", out _));
        Assert.True(instance.TryGetValue("b", out _));
        Assert.True(instance.TryGetValue("c", out _));
        Assert.True(instance.TryGetValue("d", out _));
        Assert.True(instance.TryGetValue("e", out _));
        Assert.False(instance.TryGetValue("f", out _));
    }
    
    [Fact]
    public void SmallDictionary0_should_enumerate_as_expected() {
        Assert.Equal(
            expected: Array.Empty<KeyValuePair<string, string>>(),
            actual: new SmallDictionary0<string, string>(
                EqualityComparer<string>.Default
            )
        );
    }
    
    [Fact]
    public void SmallDictionary1_should_enumerate_as_expected() {
        Assert.Equal(
            expected: new[] {
                KeyValuePair.Create("a", "a")
            },
            actual: new SmallDictionary1<string, string>(
                EqualityComparer<string>.Default,
                KeyValuePair.Create("a", "a")
            )
        );
    }
    
    [Fact]
    public void SmallDictionary2_should_enumerate_as_expected() {
        Assert.Equal(
            expected: new[] {
                KeyValuePair.Create("a", "a"),
                KeyValuePair.Create("b", "b")
            },
            actual: new SmallDictionary2<string, string>(
                EqualityComparer<string>.Default,
                KeyValuePair.Create("a", "a"),
                KeyValuePair.Create("b", "b")
            )
        );
    }
    
    [Fact]
    public void SmallDictionary3_should_enumerate_as_expected() {
        Assert.Equal(
            expected: new[] {
                KeyValuePair.Create("a", "a"),
                KeyValuePair.Create("b", "b"),
                KeyValuePair.Create("c", "c")
            },
            actual: new SmallDictionary3<string, string>(
                EqualityComparer<string>.Default,
                KeyValuePair.Create("a", "a"),
                KeyValuePair.Create("b", "b"),
                KeyValuePair.Create("c", "c")
            )
        );
    }
    
    [Fact]
    public void SmallDictionary4_should_enumerate_as_expected() {
        Assert.Equal(
            expected: new[] {
                KeyValuePair.Create("a", "a"),
                KeyValuePair.Create("b", "b"),
                KeyValuePair.Create("c", "c"),
                KeyValuePair.Create("d", "d")
            },
            actual: new SmallDictionary4<string, string>(
                EqualityComparer<string>.Default,
                KeyValuePair.Create("a", "a"),
                KeyValuePair.Create("b", "b"),
                KeyValuePair.Create("c", "c"),
                KeyValuePair.Create("d", "d")
            )
        );
    }
    
    [Fact]
    public void SmallDictionary2_construtor_throws_ArgumentException_on_duplicate_key() {
        ThrowsExceptionOnDuplicateKey(2, SmallDictionary2Factory);
    }
    
    [Fact]
    public void SmallDictionary3_construtor_throws_ArgumentException_on_duplicate_key() {
        ThrowsExceptionOnDuplicateKey(3, SmallDictionary3Factory);
    }
    
    [Fact]
    public void SmallDictionary4_construtor_throws_ArgumentException_on_duplicate_key() {
        ThrowsExceptionOnDuplicateKey(4, SmallDictionary4Factory);
    }
    
    private void ThrowsExceptionOnDuplicateKey(
        int capacity,
        Func<
            IEqualityComparer<string>,
            IReadOnlyList<KeyValuePair<string, string>>,
            SmallDictionary<string, string>
        > factory
    ) {
        var templateEntries = new[] {
            KeyValuePair.Create("a", "a"),
            KeyValuePair.Create("b", "b"),
            KeyValuePair.Create("c", "c"),
            KeyValuePair.Create("d", "d"),
            KeyValuePair.Create("e", "e")
        };
        
        for(int i = 1; i < capacity; i++) {
            for(var j = 0; j < i; j++) {
                var entries = templateEntries.Take(capacity).ToList();
                entries[j] = templateEntries[i];
                
                Assert.Throws<ArgumentException>(() => factory(EqualityComparer<string>.Default, entries));
            }
        }
    }
}
