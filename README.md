# Arborist

[![build](https://github.com/jcracknell/arborist/actions/workflows/build.yml/badge.svg)](https://github.com/jcracknell/arborist/actions/workflows/build.yml)

Arborist is a library for manipulating and combining C# expression trees consumed by the IQueryable
interface and object-relational mappers such as EntityFramework. It provides full expression
interpolation ("quasiquoting") capabilities, allowing you to interpolate expressions in a manner
analagous to string interpolation, as well as a suite of generalized expression manipulation helpers.

Arborist differs from [LINQKit][2] in that it:

  - provides a more powerful, generalized approach to expression interpolation which is also
    more performant than Expand/AsExpandable; and
  - adopts a composable, functional approach to expression manipulation supporting expressions
    in general as compared to mutable PredicateBuilder instances.

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
  static (x, q) => q.Any(x.SpliceQuoted(x.Data.Predicate))
);
// q => q.Any(c => c.Age == 8)
```

#### SpliceValue

Splices the value of the provided argument into the expression tree as a constant value using
[Expression.Constant][0], which is somewhat unfortunately named as it is easily confused with 
the concept of `const` in C#, but is also used to represent "constant references".

```csharp
ExpressionOnNone.Interpolate(
    new { Value = 42 },
    static x => x.SpliceValue(x.Data.Value)
);
// () => 42
```

### Compile-time interpolator interception

![Compile-time interpolator interception](https://github.com/user-attachments/assets/a88e389b-479a-4454-9606-163ac2fbceb2)

Arborist supports compile-time generation of interpolator implementations via C#12/.NET8
[method interception][3].
This dramatically improves the performance characteristics of expression interpolation (which
can otherwise be relatively costly), as an intercepted interpolation call takes somewhere on
the order of 5% of the execution time of the equivalent un-intercepted call.

To enable code generation for interpolation interceptors, you need to add
`Arborist.Interpolation.Interceptors` to the `InterceptorsPreviewNamespaces` build property
(or `InterceptorsNamespaces` if you are using the .NET 9 SDK):

```xml
<PropertyGroup>
  <InterceptorsPreviewNamespaces>$(InterceptorsPreviewNamespaces);Arborist.Interpolation.Interceptors</InterceptorsPreviewNamespaces>
</PropertyGroup>
```

Interceptor generation relys on analyzing the syntax of the interpolated expression tree to produce
an optimized implementation of the required interpolation operations which avoids having to compile
an expression to extract the values of evaluated splice arguments (those decorated with
EvaluatedSpliceParameterAttribute).

There are a number of scenarios which can prevent interception of an interpolation call, in which
case the call is handled by the slower runtime interpolation process:

  - The interpolated expression is not a literal (inline) lambda expression, in which case
    it cannot be analyzed.

  - The interpolated expression contains evaluated splice arguments referencing types or
    members which are not accessible from the global scope of the assembly. Broadly this means
    private and protected types and members, as well as type parameters defined in the scope
    in which the interpolation call occurs.

  - The interpolated expression contains an evaluated splice argument containing references to
    values defined in the scope of the interpolation call.

Some of these cases can be worked around by pre-computing and passing spliced values using the
data parameter. This handles the case where a spliced value is computed by an inaccessible method,
however in the event that the type of the spliced value itself is inaccessible to the interceptor
implementation, you may have no choice but to accept the slower runtime interpolation implementation.

> **NOTE:** In the event that you encounter problems related to the interceptor generator,
> InterpolateRuntimeFallback overloads are provided for all interceptable expression interpolation
> methods which are always handled by the slower but more reliable runtime interpolation process.

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
[3]: https://learn.microsoft.com/en-us/dotnet/csharp/whats-new/csharp-12#interceptors
