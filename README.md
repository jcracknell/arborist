# Arborist

[![build](https://github.com/jcracknell/arborist/actions/workflows/build.yml/badge.svg)](https://github.com/jcracknell/arborist/actions/workflows/build.yml)

## Expression interpolation

One of the many problems with EntityFramework (and other IQueryable-based ORMs) is an almost total lack
of composability. It is exceedingly difficult to combine and reuse existing queries, predicates, and
projections as the provided APIs for combining and building expression trees make this monstrously
difficult to achieve on any meaningful scale.

Arborist provides expression interpolation functionality which makes it easy to combine and reuse
expression trees in a manner that is (mostly) typesafe. As an introductory motivating example, the
following code:

```csharp
var dogPredicate = ExpressionOn<Dog>.Of(d => d.Name == "Odie");

var ownerPredicate = ExpressionOn<Owner>.Interpolate(
    new { dogPredicate },
    static (x, o) => o.Name == "Jon" && o.Dogs.Any(x.Splice(x.Data.dogPredicate))
);

var catPredicate = ExpressionOn<Cat>.Interpolate(
    new { ownerPredicate },
    static (x, c) => c.Name == "Garfield" && x.SpliceBody(c.Owner, x.Data.ownerPredicate)
);
```

produces the following expression:

```csharp
c => c.Name == "Garfield"
&& (c.Owner.Name == "Jon" && c.Owner.Dogs.Any(d => d.Name == "Odie"))
```

Expression interpolators operate on an input lambda expression where the first parameter is the
*interpolation context* providing access to splicing methods and injected data injected into the
interpolation process, and any subsequent parameters are those that are provided to the expression
resulting from the interpolation process.

### Splicing methods

Lacking an actual compiler-provided interpolation syntax, the interpolation process works by
analyzing and replacing calls to the splicing methods defined on the interpolation context
provided as the first parameter to interpolated expressions.
If we were to map expression interpolation to the most obvious analog with which most readers
will be familiar; then the interpolator methods are equivalent to an interpolated string literal
`$"..."`, and the splicing methods are then the interpolated substrings `{...}` in that they
splice/lower/unquote their argument expressions into the enclosing "quoted" expression tree.

Expressions are not strings, and as such multiple splicing methods are provided with different
behaviors as detailed in the following sections.

#### Splice

Splices the argument expression into the tree. Can be used to splice an Expression&lt;T&gt; as a T, or
any arbitrary expression node provided it does not capture any parameter references from a source
expression.

```csharp
var projection = ExpressionOn<string>.Of(v => v.Length);

ExpressionOn<IEnumerable<string>>.Interpolate(
    new { projection },
    static (x, a) => a.Select(x.Splice(x.Data.projection))
);
// a => a.Select(v => v.Length);

ExpressionOnNone.Interpolate(x => Math.Abs(x.Splice<int>(Expression.Constant(2))));
// () => Math.Abs(2)
```

#### SpliceBody

Splices the body of the argument lambda expression into the interpolated expression, replacing parameter
references in the spliced expression with the provided argument expressions from the parent expression
tree.

```csharp
ExpressionOn<Cat>.Interpolate((x, c) => x.SpliceBody(c.Owner, o => o.Name == "Jon"));
// c => c.Owner.Name == "Jon"
```

#### SpliceQuoted

Splices the argument expression into the resulting expression tree as a quoted (literal)
LambdaExpression using [Expression.Quote][1]. This method produces an inlined
Expression&lt;TDelegate&gt; instead of the TDelegate resulting from Splice, and is important in
scenarios where you need to splice an analyzable expression tree into a method on an
IQueryable&lt;T&gt; instance (typically when writing a manual join or union on EntityFramework
DbSets).

#### SpliceValue

Splices the value of the provided argument into the expression tree as a constant value using
[Expression.Constant][0], which is somewhat unfortunately named as it is easily confused with 
the concept of `const` in C#, but is also used to represent "constant references".

```csharp
ExpressionOnNone.Interpolate(
    new { value = 42 },
    x => x.SpliceValue(x.Data.value)
);
// () => 42
```

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

[0]: https://learn.microsoft.com/en-us/dotnet/api/system.linq.expressions.expression.constant
[1]: https://learn.microsoft.com/en-us/dotnet/api/system.linq.expressions.expression.quote
