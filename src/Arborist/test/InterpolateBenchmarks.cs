using Arborist.Interpolation;
using Arborist.TestFixtures;
using Xunit.Abstractions;

namespace Arborist;

[Collection(nameof(InterpolateBenchmarks))]
[CollectionDefinition(nameof(InterpolateBenchmarks), DisableParallelization = true)]
public class InterpolateBenchmarks(ITestOutputHelper outputHelper) {
    private const int WARMUP_ITERATIONS = 1_000;
    private const int BENCHMARK_ITERATIONS = 10_000;

    private static readonly Expression<Func<string, bool>> SpliceBodyExpression =
        ExpressionOn<string>.Of(str => str == "Garfield");

    private static readonly Expression<Func<Dog, bool>> SpliceExpression =
        ExpressionOn<Dog>.Of(d => d.Name == "Odie");

    private static readonly string SplicedValue = "Jon";

    [Fact]
    public void CompileTime() {
        var data = new { SpliceExpression, SpliceBodyExpression, SplicedValue };

        Benchmark(nameof(CompileTime), data, static data => {
            ExpressionOn<Owner>.Interpolate(data, (x, o) =>
                o.Name == x.SpliceValue(x.Data.SplicedValue)
                && o.Cats.Any(c => x.SpliceBody(c.Name, x.Data.SpliceBodyExpression))
                && o.Dogs.Any(x.Splice(x.Data.SpliceExpression))
            );
        });
    }

    [Fact]
    public void Runtime() {
        var data = new { SpliceExpression, SpliceBodyExpression, SplicedValue };

        Benchmark(nameof(Runtime), data, static data => {
            ExpressionOn<Owner>.InterpolateRuntimeFallback(data, (x, o) =>
                o.Name == x.SpliceValue(x.Data.SplicedValue)
                && o.Cats.Any(c => x.SpliceBody(c.Name, x.Data.SpliceBodyExpression))
                && o.Dogs.Any(x.Splice(x.Data.SpliceExpression))
            );
        });
    }

    private void Benchmark<TData>(string title, TData data, Action<TData> action) {
        for(var i = 0; i < WARMUP_ITERATIONS; i++)
            action(data);

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        for(var i = 0; i < BENCHMARK_ITERATIONS; i++)
            action(data);

        var elapsed = stopwatch.Elapsed;
        outputHelper.WriteLine($"{title}: {BENCHMARK_ITERATIONS} iterations in {elapsed}");
    }
}
