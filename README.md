# Arborist

[![build](https://github.com/jcracknell/arborist/actions/workflows/build.yml/badge.svg)](https://github.com/jcracknell/arborist/actions/workflows/build.yml)
[![NuGet Version](https://img.shields.io/nuget/v/Arborist)](https://www.nuget.org/packages/Arborist)

Arborist is a library for manipulating and combining C# expression trees consumed by the IQueryable
interface and object-relational mappers such as EntityFramework. It provides full expression
interpolation ("quasiquoting") capabilities, allowing you to interpolate expressions in a manner
analagous to string interpolation, as well as a suite of generalized expression helpers for
manipulating and combining expression trees.

## Contents

  - [Expression interpolation](#expression-interpolation)
      - [Splicing methods](#splicing-methods)
          - [Splice](#splice)
          - [SpliceBody](#splicebody)
          - [SpliceConstant](#spliceconstant)
          - [SpliceQuoted](#splicequoted)
      - [Interpolating IQueryable&lt;T&gt; extension methods](#interpolating-iqueryablet-extension-methods)
      - [Interpolation and LINQ query syntax](#interpolation-and-linq-query-syntax)
      - [Interpolation performance considerations](#interpolation-performance-considerations)
  - [Predicate helpers](#predicate-helpers)
      - [And/AndTree](#andandtree)
      - [Not](#not)
      - [NotNullAnd](#notnulland)
      - [NullOr](#nullor)
      - [Or/OrTree](#orortree)
  - [Orderings](#orderings)
      - [Composable orderings for EntityFramework](#composable-orderings-for-entityframework)
      - [Ordering simplification](#ordering-simplification)
      - [Ordering JSON serialization](#ordering-json-serialization)
  
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
ExpressionOn<IEnumerable<string>>.Interpolate(
    new { Projection = ExpressionOn<string>.Of(v => v.Length) },
    static (x, e) => e.Select(x.Splice(x.Data.Projection))
);
// e => e.Select(v => v.Length);

ExpressionOnNone.Interpolate(
    new { Expr = Expression.Constant(42) },
    static x => Math.Abs(x.Splice<int>(x.Data.Expr))
);
// () => Math.Abs(42)
```

#### SpliceBody

Splices the body of the argument lambda expression into the interpolated expression, replacing parameter
references in the spliced expression with the provided argument expressions from the parent expression
tree.

```csharp
ExpressionOn<Cat>.Interpolate(
    new { Predicate = ExpressionOn<Owner>.Of(o => o.Name == "Jon") },
    static (x, c) => x.SpliceBody(c.Owner, x.Data.Predicate)
);
// c => c.Owner.Name == "Jon"
```

#### SpliceConstant

Splices the value of the provided argument into the expression tree as a constant value using
[Expression.Constant][0]. This is partially related to the `const` keyword as it represents
a value that is constant *in the context of the expression*, however it can also be used to
capture constant references to non-primitive types.

```csharp
ExpressionOnNone.Interpolate(
    new { Value = 42 },
    static x => x.SpliceConstant(x.Data.Value)
);
// () => 42
```

> **⚠️ Caution:** A value spliced as a constant within an expression is typically translated by
> EntityFramework into a literal SQL value instead of a query parameter. This could prevent your
> database from caching and reusing an execution plan for your query in the event that the
> value changes.

To avoid embedding a value as a constant in a scenario where the value is passed in via the data
parameter, you should wrap the value in a containing type, and then access it via a spliced constant
reference to the container. This approach emulates a captured local variable, which is represented
in an expression tree as a field with the same name accessed via a constant reference to the 
compiler-generated "display class" representing the captured scope variables.

```csharp
ExpressionOnNone.Interpolate(
    new { Value = 42 },
    static x => x.SpliceConstant(x.Data).Value
)
// () => { Value = 42 }.Value
```

EntityFramework 9.0+ also offers the [EF.Parameter][5] helper, which forces SQL parametrization
of its argument value.

#### SpliceQuoted

Splices the argument expression into the resulting expression tree as a quoted (literal)
LambdaExpression using [Expression.Quote][1]. This method produces an inlined
Expression&lt;TDelegate&gt; instead of the TDelegate resulting from Splice, and is important in
scenarios where you need to splice an analyzable expression tree into a method on an
IQueryable&lt;T&gt; instance (typically when writing a manual join or union on EntityFramework
DbSets).

```csharp
ExpressionOn<IQueryable<Cat>>.Interpolate(
    new { Predicate = ExpressionOn<Cat>.Of(c => c.Age == 8) },
    static (x, q) =>
        q.Any(x.SpliceQuoted(x.Data.Predicate))
        && q.Any(x.Splice(x.Data.Predicate))
);
// q => Queryable.Any(q, c => c.Age == 8)
// && Enumerable.Any(q, c => c.Age == 8)
```

### Interpolating IQueryable&lt;T&gt; extension methods

Arborist provides a suite of extension methods on IQueryable&lt;T&gt; instances providing easy
support for interpolation with type inferral.

For every extension method defined by System.Linq.Queryable accepting an expression as an argument,
a source generator is used to define an equivalent interpolating extension method accepting every
possible combination of interpolated and uninterpolated expressions as determined by the inclusion
of an optional leading IInterpolationContext parameter. 
In addition, overloads are generated accepting a data parameter immediately preceding the initial
expression argument (the data parameter follows the input collection argument to the joining and
set operations).

```csharp
dbContext.Cats.SelectManyInterpolated(
    // Optional data argument precedes the initial expression
    new { dogPredicate },
    // Include an IInterpolationContext parameter for interpolation
    (x, c) => c.Owner.Dogs.Where(x.Splice(x.Data.dogPredicate)),
    // Or omit it to skip interpolation of a given expression
    (c, d) => new { c, d }
);
```

There are a few exceptions to this rule:

  - Overloads accepting a supplementary index parameter (as in the case of Select and SelectMany)
    are not mirrored, as the supplementary parameter would prevent resolution of the appropriate
    interpolating overload.
  
  - A data-accepting overload is not generated for methods where the type of the argument preceding
    the initial expression is a generic type parameter (as in the case of Aggregate).

### Interpolation and LINQ query syntax

Arborist does not provide a way to apply interpolation to all expressions processed by a given
IQueryable&lt;T&gt; instance (i.e. an equivalent to LINQKit's AsExpandable extension method),
because this feature is wholly incompatible with the design goal of having explicit, limited
scopes within which interpolation can occur.

Requiring splicing operations to be conducted on an IInterpolationContext provided by the explicit
invocation of an interpolation method prevents accidental omission of the interpolation process
(as is the case with LINQKit's Expand/AsExpandable and its "misappropriated" splicing methods).
It also permits us to ship a syntax analyzer to help you identify cases where interpolation is
incorrectly applied at compile time.

A consequence of this design is that there is no way to apply interpolation to a query expressed
purely using LINQ query syntax (from ...), because there is no reasonable way to establish a
delimiting scope within which interpolation can occur. This is not a huge loss, as there are
numerous reasons why you should not use LINQ query syntax:

  - it is only capable of expressing a small subset of the available LINQ operations;
  - it resembles SQL but is actually a misrepresented monadic comprehension and has
    no bearing on the actual SQL which is generated and executed;
  - you are *required* to switch to method syntax to materialize query results; and
  - it obscures the usage of Expression&lt;T&gt; instances when assembling queries - the
    fundamental currency of composability and reusability for the IQueryable&lt;T&gt; API and
    your presumable reason for applying interpolation in the first place.

That being said it is possible to wrap query syntax within a top level SelectMany by pulling the
initial input up out of the query syntax - with the caveat that such usage requires at least two
input clauses:

```csharp
dbContext.Owner.SelectManyInterpolated(
    (x, o) =>
        from d in o.Dogs
        where x.SpliceBody(d, dogPredicate)
        select d
);
```

### Interpolation performance considerations

Most of the performance cost associated with expression interpolation is associated with the
evaluation of expression subtrees provided as arguments to evaluated splice parameters
(annotated with EvaluatedSpliceParameterAttribute). Interpolation becomes expensive in the
event that expression compilation is required to evaluate such subtrees.

Arborist will first attempt to evaluate these expressions using a reflective approach which
is capable of interpreting most *basic* expression trees; i.e. member accesses, method
invocations, and some basic type conversions. Despite the use of the reflection API, evaluating
expressions this way is *significantly* faster than the compilation-based approach used as a
fallback in the event that it fails.

> **💡 Hint:** You should try to pre-evaluate your spliced values to simplify the subtrees
> requiring evaluation by the interpolation process.

Interpolation always requires construction of new expression trees (to eliminate the
IInterpolationContext parameter), however you can reduce the number of allocations per
call by using the interpolation data parameter, which lets you make the input expression a
static declaration.

A simple comparison of basic expression interpolation performance between Arborist 0.2.0 and
LinqKit 1.3.8 yields the following results:

| Method                        | Mean      | Error     | StdDev    | Allocated |
|------------------------------ |----------:|----------:|----------:|----------:|
| Arborist_Interpolate_Dynamic  |  4.178 us | 0.0065 us | 0.0054 us |   2.87 KB |
| Arborist_Interpolate_Static   |  3.574 us | 0.0080 us | 0.0071 us |   2.57 KB |
| Arborist_Interpolate_Compiled |  6.799 us | 0.0146 us | 0.0129 us |   4.86 KB |
| LinqKit_Expand_Dynamic        | 46.713 us | 0.1502 us | 0.1331 us |   5.71 KB |
| LinqKit_Expand_Static         | 45.112 us | 0.1357 us | 0.1133 us |   5.71 KB |

Here the Compiled case represents an uncommon, worst-case scenario where Arborist
is obligated to compile the expression providing the spliced expression tree in order to
evaluate it.

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
Has overloads handling both class and struct types.

```csharp
ExpressionHelper.NotNullAnd(ExpressionOn<string>.Of(s => s.Length == 4))
// s => s != null && s.Length == 4

ExpressionHelper.NotNullAnd(ExpressionOn<int>.Of(i => i % 2 == 0))
// i => i.HasValue && i.Value % 2 == 0
```

### NullOr

Given a predicate Expression&lt;Func&lt;A,&nbsp;bool&gt;&gt;, creates an
Expression&lt;Func&lt;A?,&nbsp;bool&gt;&gt; asserting that the input value is null,
or satisfies the provided predicate expression.
Has overloads handling both class and struct types.

```csharp
ExpressionHelper.NullOr(ExpressionOn<string>.Of(s => s.Length == 4))
// s => s == null || s.Length == 4

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
composing expression-based IQueryable&lt;T&gt; orderings is a common pain point. As such, extension methods are
provided which make it easy to apply an Ordering&lt;Expression&lt;Func&lt;E, A&gt;&gt;&gt; to an
IQueryable&lt;E&gt; instance.

**An Ordering&lt;TSelector&gt; is an immutable singly-linked list.** All operations performed on an
ordering return a new instance reflecting the changes resulting from the operation, providing a natural "fluent"
API aligned with the IEnumerable&lt;T&gt; based extension methods.

The ThenBy family of extension methods make it easy to combine an ordering with additional terms or even
other orderings:

```csharp
public static Ordering<TSelector> ThenBy<TSelector>(
    this Ordering<TSelector> ordering,
    TSelector selector,
    OrderingDirection direction
);

public static Ordering<TSelector> ThenBy<TSelector>(
    this Ordering<TSelector> ordering,
    OrderingTerm<TSelector> term
);

public static Ordering<TSelector> ThenBy<TSelector>(
    this Ordering<TSelector> ordering,
    IEnumerable<OrderingTerm<TSelector>> terms
);
```

### Composable orderings for EntityFramework

The sorting API associated with the IQueryable&lt;E&gt; interface relies entirely on the OrderBy and ThenBy
extension methods which translate a selector Expression&lt;Func&lt;E,&nbsp;A&gt;&gt; on the sorted entity
into an SQL order by clause (or whatever equivalent syntactic structure applies to your backing storage).

```csharp
public static IOrderedQueryable<E> OrderBy<E, A>(
    this IQueryable<E> queryable,
    Expression<Func<E, A>> expression
);

public static IOrderedQueryable<E> ThenBy<E, A>(
    this IOrderedQueryable<E> queryable,
    Expression<Func<E, A>> expression
);
```

The main barrier to implementing a composable ordering model using this API is the need for a common
type for ordering selector expressions applying to a given entity type. Conveniently EntityFramework
ignores top-level conversions to System.Object occurring at the top level of such expressions (presumably
for exactly this reason), and as such our general objective is to generate an
Ordering&lt;Expression&lt;Func&lt;E,&nbsp;object?&gt;&gt;&gt; which can be applied to an IQueryable&lt;E&gt;.
This approach should also work for any other IQueryable-based ORM provided it similarly ignores top-level
object conversion.

If you are dynamically generating orderings based on client input, you will most likely need a domain-adjacent
model for your ordering *selectors* - the aspects of your subject entities according to which they can be
sorted. Otherwise feel free to skip this and generate an expression-based ordering directly.

The design of your selector model is entirely up to you, however generally you want something akin to a union
type, i.e. an enum-like type supporting nested values which will permit you to define nested/composite
selectors (which we will cover shortly).

```csharp
public abstract class OwnerOrderingSelector {
    private OwnerOrderingSelector() { }

    public sealed class Id : OwnerOrderingSelector { }
    public sealed class Name : OwnerOrderingSelector { }
}
```

Now we need to write a function to translate our selector model into an expression-based ordering consumable
by EntityFramework. You almost certainly want to define a [type alias][4] for your translated ordering type
so that you don't run out of angle brackets. Note that the type alias should appear *within* your namespace
declaration to avoid having to fully-qualify the required type references.

```csharp
using OwnerOrdering = Ordering<Expression<Func<Owner, object?>>>;
```

With our type alias the selector type of our result ordering is known to be an expression - they can
therefore be specified inline, and we can now write a simple function to translate an OwnerOrderingSelector
into an Ordering&lt;Expression&lt;Func&lt;Owner,&nbsp;object?&gt;&gt;&gt;:

```csharp
private OwnerOrdering TranslateOwnerOrderingSelector(
    VetDbContext dbContext,
    OwnerOrderingSelector selector
) =>
    selector switch {
        OwnerOrderingSelector.Id => OwnerOrdering.ByAscending(o => o.Id),
        OwnerOrderingSelector.Name => OwnerOrdering.ByAscending(o => o.Name),
        _ => throw new NotImplementedException()
    };
```

We will use a pair of extension methods with the following signatures to convert and apply an
Ordering&lt;OwnerOrderingSelector&gt; to an IQueryable&lt;Owner&gt; instance:

```csharp
public static Ordering<R> TranslateSelectors<S, D, R>(
    this Ordering<S> ordering,
    D data,
    Func<D, S, IEnumerable<OrderingTerm<R>>> translation
);

public static IQueryable<E> OrderBy<E, A>(
    this IQueryable<E> queryable,
    Ordering<Expression<Func<E, A>>> ordering
);
```

The TranslateSelectors extension method applies your translation function to the selectors of an input
ordering to produce the desired expression-based ordering. TranslateSelectors differs from SelectMany
in that (a) it operates on selectors instead of terms, thus simplifying the implementation of your
translation function; and (b) it automatically applies the OrderingDirection associated with the input
term to the associated translation results.

If provided, the optional data parameter is passed as the initial argument to your translation function.
Typically when writing selector translations for EntityFramework, you should always pass your DbContext
into the translation function to support manual joins and subqueries in your selector expressions.

The provided OrderBy extension method is then used to automatically apply the resulting
Ordering&lt;Expression&lt;Func&lt;Owner,&nbsp;object?&gt;&gt;&gt; to an IQueryable&lt;Owner&gt; instance:

```csharp
var ordering = Ordering<OwnerOrderingSelector>
.ByDescending(new OwnerOrderingSelector.Id())
.ThenByAscending(new OwnerOrderingSelector.Name());

Ordering<Expression<Func<Owner, object?>>> expressionOrdering =
    ordering.TranslateSelectors(dbContext, TranslateOwnerOrderingSelector);

IQueryable<Owner> orderedQuery =
    dbContext.Owner.AsQueryable().OrderBy(expressionOrdering);
```

You may note that the OrderBy extension method returns an IQueryable&lt;E&gt; instead of an
IOrderedQueryable&lt;E&gt; - this is because an ordering may be empty, in which case no
actual ordering occurs. Under ideal circumstances all applicable orderings for a given
query should be merged into and applied via a single ordering.

It is also worth noting that our input ordering is explicitly typed as an
Ordering&lt;OwnerOrderingSelector&gt; - this is a workaround for the C# type system's lack of
support for least upper bound calculations. An alternative approach would be to define factories
for your selector types which explicitly return the base type.

To compose selectors for an entity with those of a related entity, you can define a selector
which carries a selector for the related entity:

```csharp
public abstract class CatOrderingSelector {
    private CatOrderingSelector() { }
    
    public sealed class Id : CatOrderingSelector { }

    public sealed class Owner(OwnerOrderingSelector selector)
        : CatOrderingSelector
    {
        public OwnerOrderingSelector Selector { get; } = selector;
    }
}
```

And the requisite type alias:


```csharp
using CatOrdering = Ordering<Expression<Func<Cat, object?>>>;
```

Your translation function should then invoke the translation process for the related selector,
and graft the resulting Expression&lt;Func&lt;Owner,&nbsp;object?&gt;&gt; selectors onto a
projection expression identifying the related entity to which the composite selector applies.
Extension methods GraftSelectorExpressionsTo and GraftSelectorExpressionsToNullable are provided
to assist with this process:

```csharp
private CatOrdering TranslateCatOrderingSelector(
    VetDbContext dbContext,
    CatOrderingSelector selector
) =>
    selector switch {
        CatOrderingSelector.Id => CatOrdering.ByAscending(c => c.Id),

        CatOrderingSelector.Owner ownerSelector =>
            TranslateOwnerOrderingSelector(dbContext, ownerSelector.Selector)
            .GraftSelectorExpressionsTo(ExpressionOn<Cat>.Of(c => c.Owner)),

        _ => throw new NotImplementedException()
    };
```

Here note that the projection used can be arbitrarily complex (provided your ORM is capable of translating
it), however you are relying on the query analyzer's ability to cache the results of the projection in
the event that it appears in multiple order by clauses.

The GraftSelectorExpressionsToNullable extension method is intended to be used in the event that the
relationship between the two entities is optional (in which case the result of the projection and by
extension the grafted selectors can be null). You can control the handling of null values by prepending
an appropriate term to the result ordering:

```csharp
CatOrdering.ByAscending(c => c.Owner == null).ThenBy(
    TranslateOwnerOrderingSelector(dbContext, ownerSelector.Selector)
    .GraftSelectorExpressionsToNullable(ExpressionOn<Cat>.Of(c => c.Owner))
)
```

The GraftSelectorExpressionsTo family of extension methods are provided as a syntactic convenience covering
the most common usage scenarios - you can always implement arbitrarily complex ordering transformations
yourself:

```csharp
TranslateOwnerOrderingSelector(dbContext, ownerSelector.Selector)
.Select(term => OrderingTerm.Create(
    selector: ExpressionOn<Cat>.Interpolate(
        term,
        (x, c) => x.SpliceBody(c.Owner, x.Data.Selector)
    ),
    direction: term.Direction
))
```

### Ordering simplification

In the event that you are using the ordering model to derive query orderings from user input (as
is often the case), you should always impose reasonable limits on the number of ordering clauses
which can be supplied to a query by the user. Generally this looks like applying the Simplify
extension method to eliminate superfluous ordering terms, followed by the Take extension to
ultimately cap the number of ordering terms:

```csharp
ordering.Simplify().Take(MaxOrderingTerms)
```

By default the simplification process drops terms referencing selectors which have already been
observed in the subject ordering per the default C# notion of equality. Further simplification
is possible by implementing the IOrderingSelector&lt;TSelf&gt; or
IOrderingSelectorComparer&lt;TSelector&gt; interfaces, which extend the well-known
IEquatable&lt;T&gt; or IEqualityComparer&lt;T&gt; interfaces with a single additional method
signaling whether or not a selector represents an absolute ordering:

```csharp
public interface IOrderingSelector<TSelf>
    : IEquatable<TSelf>
{
    public bool IsAbsoluteOrdering { get; }
}

public interface IOrderingSelectorComparer<in TSelector>
    : IEqualityComparer<TSelector>
{
    public bool IsAbsoluteOrdering([DisallowNull] TSelector selector);
}
```

An *absolute ordering* is an ordering such that the order of the set of entities resulting from the
application of the ordering is always the same, regardless of the order in which they appeared in the
input. In most scenarios you want an ordering applied to a query to end with such a selector to
stabilize the order of results.

This is relevant to the simplification process because ordering effectively stops after processing
a selector representing an absolute ordering, as any further terms in the ordering cannot alter the
order of the results. A typical example of such selectors are those that map to a database column
with a unique constraint, as every value in the column is known to be unique and should have a
strict order with respect to all other values in the column.

The easiest and most reliable way to implement IOrderingSelector&lt;TSelf&gt; will always be using
the automatically derived equality implementations provided by record types.
RecordOrderingSelector&lt;TSelf&gt; is provided as an abstract base class which takes care of
most of the boilerplate required for a record-based implementation:

```csharp
public abstract record CatOrderingSelector
    : RecordOrderingSelector<CatOrderingSelector>
{
    private CatOrderingSelector() { }

    public sealed record Id : CatOrderingSelector {
        // Override to mark the selector as absolute
        public override bool IsAbsoluteOrdering => true;
    }

    public sealed record Name : CatOrderingSelector;
    
    // Composite orderings are rarely absolute unless the relationship is 1:1
    public sealed record Owner(OwnerOrderingSelector Selector)
        : CatOrderingSelector;
}
```

### Ordering JSON serialization

System.Text.Json converters are provided for orderings and related types per the following simple grammar (recall
an ordering is a collection of terms), with the JSON representation of selectors being dependent on the selector
type:

```
         Ordering ::= [ OrderingTerm* ]

     OrderingTerm ::= [ TSelector, OrderingDirection ]

OrderingDirection ::= "a[scending]"  // canonically "asc"
                    | "d[escending]" // canonically "desc"
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
[3]: https://learn.microsoft.com/en-us/dotnet/csharp/whats-new/csharp-12#interceptors
[4]: https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/using-directive#the-using-alias
[5]: https://learn.microsoft.com/en-us/dotnet/api/microsoft.entityframeworkcore.ef.parameter
