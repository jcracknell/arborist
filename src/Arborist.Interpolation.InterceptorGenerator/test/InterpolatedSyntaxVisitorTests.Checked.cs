using Xunit;

namespace Arborist.Interpolation.InterceptorGenerator;

public partial class InterpolatedSyntaxVisitorTests {
    [Fact]
    public void Should_work_for_checked_addition() {
        var results = InterpolationInterceptorGeneratorTestBuilder.Create()
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
        var results = InterpolationInterceptorGeneratorTestBuilder.Create()
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
        var results = InterpolationInterceptorGeneratorTestBuilder.Create()
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
        var results = InterpolationInterceptorGeneratorTestBuilder.Create()
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

    [Fact]
    public void Should_work_for_checked_numeric_conversion() {
        var results = InterpolationInterceptorGeneratorTestBuilder.Create()
        .Generate(@"
            ExpressionOn<Cat>.Interpolate(default(object), (x, c) => checked((short)c.Id));
        ");

        Assert.Equal(1, results.AnalysisResults.Count);
        CodeGenAssert.CodeEqual(
            expected: @"
                global::System.Linq.Expressions.Expression.ConvertChecked(
                    global::System.Linq.Expressions.Expression.Property(
                        __p0,
                        typeof(global::Arborist.TestFixtures.Cat).GetProperty(""Id"")!
                    ),
                    typeof(global::System.Int16)
                )
            ",
            actual: results.AnalysisResults[0].BodyTree.ToString()
        );
    }

    [Fact]
    public void Should_work_for_checked_object_conversion() {
        var results = InterpolationInterceptorGeneratorTestBuilder.Create()
        .Generate(@"
            ExpressionOn<Cat>.Interpolate(default(object), (x, c) => checked((object)c.Id));
        ");

        Assert.Equal(1, results.AnalysisResults.Count);
        CodeGenAssert.CodeEqual(
            expected: @"
                global::System.Linq.Expressions.Expression.ConvertChecked(
                    global::System.Linq.Expressions.Expression.Property(
                        __p0,
                        typeof(global::Arborist.TestFixtures.Cat).GetProperty(""Id"")!
                    ),
                    typeof(global::System.Object)
                )
            ",
            actual: results.AnalysisResults[0].BodyTree.ToString()
        );
    }
}
