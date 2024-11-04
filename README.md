# Arborist

[![build](https://github.com/jcracknell/arborist/actions/workflows/build.yml/badge.svg)](https://github.com/jcracknell/arborist/actions/workflows/build.yml)

## Orderings

The Arborist.Orderings namespace defines the Ordering&lt;TSelector&gt; type, which is a collection of
OrderingTerm&lt;TSelector&gt; instances. An OrderingTerm&lt;TSelector&gt; is defined by the combination
of a selector value identifying a sort of some kind, and an OrderingDirection - one of Ascending or Descending.

At a glance this might seem like a departure from the expression tooling goals of this library, however in practice
generating IQueryable&lt;T&gt; orderings is a common pain point. As such, extension methods are provided which enable
the application of an Ordering&lt;Expression&lt;Func&lt;E, V&gt;&gt;&gt; to an IQueryable&lt;E&gt; instance.

Generating such an ordering would typically be accomplished by using the monadic extensions (Select or SelectMany) to
map a domain model selector type to the appropriate expression-based ordering, likely using expression
helpers to recursively splice in orderings defined on other related entities.

System.Text.Json converters are provided for orderings and related types per the following simple grammar (recall
an ordering is a list of terms):

```
         Ordering ::= [ OrderingTerm* ]

     OrderingTerm ::= [ TSelector, OrderingDirection ]

OrderingDirection ::= "a[scending]"
                    | "d[escending]"
```

As such the following ordering:

```csharp
Ordering.By(
  OrderingTerm.Ascending(0),
  OrderingTerm.Descending(1)
)
```

has the following JSON representation:

```json
[[0, "asc"], [1, "desc"]]
```
