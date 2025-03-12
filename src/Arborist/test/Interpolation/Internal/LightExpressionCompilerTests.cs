using Arborist.TestFixtures;

namespace Arborist.Interpolation.Internal;

[Collection(nameof(LightExpressionCompilerTests))]
[CollectionDefinition(nameof(LightExpressionCompilerTests), DisableParallelization = true)]
public class LightExpressionCompilerTests {
    [Fact]
    public void Should_work_as_expected() {
        var expr = ExpressionOn<Cat>.Of(c => c.Owner.Name);

        var defaultCompiled = expr.Compile();
        var lightCompiled = LightExpressionCompiler.Instance.Compile(expr);

        Assert.Null(defaultCompiled.Method.DeclaringType);
        Assert.NotNull(lightCompiled.Method.DeclaringType);
    }

    [Fact]
    public void Should_be_10x_faster_than_the_default_compiler() {
        var lambdas = new LambdaExpression[] {
            ExpressionOn<Cat>.Of(c => 42),
            ExpressionOn<Cat>.Of(c => c.Owner.Name),
            ExpressionOn<Cat>.Of(c => c.Owner.Cats.Any(d => d.Age > c.Age - 2))
        };

        var defaultElapsed = Benchmark(static lambda => lambda.Compile());
        var lightElapsed = Benchmark(static lambda => LightExpressionCompiler.Instance.Compile(lambda));

        Assert.True(lightElapsed < defaultElapsed / 10);

        TimeSpan Benchmark(Action<LambdaExpression> compile) {
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true);

            var lambdaCount = lambdas.Length;
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            for(var i = 0; i < 10000; i++)
                compile(lambdas[Random.Shared.Next(lambdaCount)]);

            var elapsed = stopwatch.Elapsed;
            return elapsed;
        }
    }
}
