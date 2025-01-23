using Xunit;

namespace Arborist.CodeGen;

public partial class InterpolatedSyntaxVisitorTests {
    [Fact]
    public void Should_work_for_checked_addition() {
        var results = InterpolatorInterceptorGeneratorTestBuilder.Create()
        .Generate(@"
            ExpressionOn<Cat>.Interpolate(default(object), (x, c) => checked(c.Id + 1));
        ");

        Assert.Equal(1, results.AnalysisResults.Count);
        CodeGenAssert.CodeEqual(
            expected: @"
                global::System.Linq.Expressions.Expression.MakeBinary(
                    global::System.Linq.Expressions.ExpressionType.AddChecked,
                    global::System.Linq.Expressions.Expression.Property(
                        __p0,
                        typeof(global::Arborist.TestFixtures.Cat).GetProperty(""Id"")!
                    ),
                    global::System.Linq.Expressions.Expression.Constant(
                        1,
                        typeof(global::System.Int32)
                    ),
                    false,
                    __m0
                )
            ",
            actual: results.AnalysisResults[0].BodyTree.ToString()
        );
    }

    [Fact]
    public void Should_work_for_checked_multiplication() {
        var results = InterpolatorInterceptorGeneratorTestBuilder.Create()
        .Generate(@"
            ExpressionOn<Cat>.Interpolate(default(object), (x, c) => checked(c.Id * 1));
        ");

        Assert.Equal(1, results.AnalysisResults.Count);
        CodeGenAssert.CodeEqual(
            expected: @"
                global::System.Linq.Expressions.Expression.MakeBinary(
                    global::System.Linq.Expressions.ExpressionType.MultiplyChecked,
                    global::System.Linq.Expressions.Expression.Property(
                        __p0,
                        typeof(global::Arborist.TestFixtures.Cat).GetProperty(""Id"")!
                    ),
                    global::System.Linq.Expressions.Expression.Constant(
                        1,
                        typeof(global::System.Int32)
                    ),
                    false,
                    __m0
                )
            ",
            actual: results.AnalysisResults[0].BodyTree.ToString()
        );
    }

    [Fact]
    public void Should_work_for_checked_negation() {
        var results = InterpolatorInterceptorGeneratorTestBuilder.Create()
        .Generate(@"
            ExpressionOn<Cat>.Interpolate(default(object), (x, c) => checked(-c.Id));
        ");

        Assert.Equal(1, results.AnalysisResults.Count);
        CodeGenAssert.CodeEqual(
            expected: @"
                global::System.Linq.Expressions.Expression.MakeUnary(
                    global::System.Linq.Expressions.ExpressionType.NegateChecked,
                    global::System.Linq.Expressions.Expression.Property(
                        __p0,
                        typeof(global::Arborist.TestFixtures.Cat).GetProperty(""Id"")!
                    ),
                    typeof(global::System.Int32),
                    __m0
                )
            ",
            actual: results.AnalysisResults[0].BodyTree.ToString()
        );
    }

    [Fact]
    public void Should_work_for_checked_subtraction() {
        var results = InterpolatorInterceptorGeneratorTestBuilder.Create()
        .Generate(@"
            ExpressionOn<Cat>.Interpolate(default(object), (x, c) => checked(c.Id - 1));
        ");

        Assert.Equal(1, results.AnalysisResults.Count);
        CodeGenAssert.CodeEqual(
            expected: @"
                global::System.Linq.Expressions.Expression.MakeBinary(
                    global::System.Linq.Expressions.ExpressionType.SubtractChecked,
                    global::System.Linq.Expressions.Expression.Property(
                        __p0,
                        typeof(global::Arborist.TestFixtures.Cat).GetProperty(""Id"")!
                    ),
                    global::System.Linq.Expressions.Expression.Constant(
                        1,
                        typeof(global::System.Int32)
                    ),
                    false,
                    __m0
                )
            ",
            actual: results.AnalysisResults[0].BodyTree.ToString()
        );
    }
}
