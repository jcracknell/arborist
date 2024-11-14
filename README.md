# Arborist

[![build](https://github.com/jcracknell/arborist/actions/workflows/build.yml/badge.svg)](https://github.com/jcracknell/arborist/actions/workflows/build.yml)

Arborist is a library for manipulating and combining C# expression trees consumed by the IQueryable
interface and object-relational mappers such as EntityFramework. It provides full expression
interpolation ("quasiquoting") capabilities, allowing you to interpolate expressions in a manner
analagous to string interpolation, as well as a suite of generalized expression manipulation helpers.

Arborist differs from [LINQKit][2] in that it:

  - provides a generalized approach to expression interpolation as compared to
    Expand/AsExpandable,
  - adopts a composable, functional approach to expression manipulation supporting expressions
    in general as compared to mutable PredicateBuilder instances, and
  - is significantly more performant.

## Expression interpolation

One of the many problems with the IQueryable API is an almost total lack of composability.
It is exceedingly difficult to combine and reuse existing queries, predicates, and
projections as the provided APIs for combining and building expression trees make this
monstrously difficult on any meaningful scale.

Arborist provides expression interpolation functionality which makes this easy to achieve,
in a manner that is (mostly) typesafe. As an introductory motivating example, the following
code:

```csharp
var dogPredicate = ExpressionOn<Dog>.Of(d => d.Name == "Odie");

var ownerPredicate = ExpressionOn<Owner>.Interpolate(
    new { dogPredicate },
    static (x, o) => o.Name == "Jon"
    && o.Dogs.Any(x.Splice(x.Data.dogPredicate))
);

var catPredicate = ExpressionOn<Cat>.Interpolate(
    new { ownerPredicate },
    static (x, c) => c.Name == "Garfield"
    && x.SpliceBody(c.Owner, x.Data.ownerPredicate)
);
```

produces the following expression:

```csharp
c => c.Name == "Garfield" && (
    c.Owner.Name == "Jon"
    && c.Owner.Dogs.Any(d => d.Name == "Odie")
)
```

Expression interpolators operate on an input lambda expression where the first parameter is the
*interpolation context* providing access to splicing methods and injected data injected into the
interpolation process, and any additional parameters are those that appear in the expression
resulting from the interpolation process. The interpolation context provides access to the
*splicing methods* used to lower other expression trees into the result expression, as well as
to any data that you want to inject into the interpolation process.

### Splicing methods

Lacking an actual compiler-provided interpolation syntax, the interpolation process works by
analyzing and replacing calls to the splicing methods defined on the interpolation context
provided as the first parameter to an interpolated expressions.
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

ExpressionOnNone.Interpolate(
    static x => Math.Abs(x.Splice<int>(Expression.Constant(2)))
);
// () => Math.Abs(2)
```

#### SpliceBody

Splices the body of the argument lambda expression into the interpolated expression, replacing parameter
references in the spliced expression with the provided argument expressions from the parent expression
tree.

```csharp
ExpressionOn<Cat>.Interpolate(
    static (x, c) => x.SpliceBody(c.Owner, o => o.Name == "Jon")
);
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
    static x => x.SpliceValue(x.Data.value)
);
// () => 42
```

## Predicate helpers

Arborist provide several helpers specifically to assist with manipulating and combining predicate
(bool-returning) expressions.

### And/AndTree

Combines the provided collection of predicate expressions into a boolean AND operation,
returning a true-valued predicate if the collection is empty.

And produces the left-associative expression tree which would result from naiively writing
out the provided predicates, whereas AndTree can be used to produce a result expression
in the form of a balanced tree with depth log2(n). This is an important consideration when
producing expressions to be translated into SQL, as most database engines will enforce a
limit on input expression depth.

```csharp
ExpressionHelper.And([
    ExpressionOn<Cat>.Of(c => c.Name == "Garfield"),
    ExpressionOn<Cat>.Of(c => c.Name == "Nermal"),
    ExpressionOn<Cat>.Of(c => c.Name == "Arlene"),
    ExpressionOn<Cat>.Of(c => c.Name == "Mom")
]);

// c => (((c.Name == "Garfield") && c.Name == "Nermal") && c.Name == "Arlene") && c.Name == "Mom"

ExpressionHelper.AndTree([
    ExpressionOn<Cat>.Of(c => c.Name == "Garfield"),
    ExpressionOn<Cat>.Of(c => c.Name == "Nermal"),
    ExpressionOn<Cat>.Of(c => c.Name == "Arlene"),
    ExpressionOn<Cat>.Of(c => c.Name == "Mom")
]);

// c => (c.Name == "Garfield" && c.Name == "Nermal") && (c.Name == "Arlene" && c.Name == "Mom")

```

### Not

Produces the negated form of the input expression by applying the boolean NOT operator.

```csharp
ExpressionHelper.Not(ExpressionOn<Cat>.Of(c => c.Name == "Garfield"));

// c => !(c.Name == "Garfield")
```

### NotNullAnd

Given a predicate Expression&lt;Func&lt;A,&nbsp;bool&gt;&gt;, creates an
Expression&lt;Func&lt;A?,&nbsp;bool&gt;&gt; asserting that the input value is not null,
and satisfies the provided predicate expression.

```csharp
ExpressionHelper.NotNullAnd(ExpressionOn<int>.Of(i => i % 2 == 0))

// i => i.HasValue && i.Value % 2 == 0
```

### NullOr

Given a predicate Expression&lt;Func&lt;A,&nbsp;bool&gt;&gt;, creates an
Expression&lt;Func&lt;A?,&nbsp;bool&gt;&gt; asserting that the input value is null,
or satisfies the provided predicate expression.

```csharp
ExpressionHelper.NullOr(ExpressionOn<int>.Of(i => i % 2 == 0))

// i => !i.HasValue || i.Value % 2 == 0
```

### Or/OrTree

Combines the provided collection of predicate expressions into a single boolean OR operation,
returning a false-valued predicate if the collection is empty.

Or produces the left-associative expression tree which would result from naiively writing
out the provided predicates, whereas OrTree can be used to produce a result expression
in the form of a balanced tree with depth log2(n). This is an important consideration when
producing expressions to be translated into SQL, as most database engines will enforce a
limit on input expression depth.

```csharp
ExpressionHelper.Or([
    ExpressionOn<Cat>.Of(c => c.Name == "Garfield"),
    ExpressionOn<Cat>.Of(c => c.Name == "Nermal"),
    ExpressionOn<Cat>.Of(c => c.Name == "Arlene"),
    ExpressionOn<Cat>.Of(c => c.Name == "Mom")
]);

// c => (((c.Name == "Garfield") || c.Name == "Nermal") || c.Name == "Arlene") || c.Name == "Mom"

ExpressionHelper.OrTree([
    ExpressionOn<Cat>.Of(c => c.Name == "Garfield"),
    ExpressionOn<Cat>.Of(c => c.Name == "Nermal"),
    ExpressionOn<Cat>.Of(c => c.Name == "Arlene"),
    ExpressionOn<Cat>.Of(c => c.Name == "Mom")
]);

// c => (c.Name == "Garfield" || c.Name == "Nermal") || (c.Name == "Arlene" || c.Name == "Mom")
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
[2]: https://github.com/scottksmith95/LINQKit
