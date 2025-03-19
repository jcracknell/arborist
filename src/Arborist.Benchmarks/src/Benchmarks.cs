using Arborist.Interpolation.Internal;
using Arborist.TestFixtures;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using LinqKit;

namespace Arborist.Benchmarks;

[MemoryDiagnoser(displayGenColumns: false)]
public class Benchmarks {
    public static void Main(string[] args) {
        var summary = BenchmarkRunner.Run(typeof(Benchmarks).Assembly);
    }

    private static readonly Expression<Func<Dog, bool>> InstanceDogPredicate =
       d => d.Name == "Odie";

    private static readonly Expression<Func<Dog, bool>> StaticDogPredicate =
       d => d.Name == "Odie";

    [Benchmark]
    public void Arborist_Interpolate_Dynamic() {
        ExpressionOn<Cat>.Interpolate(
            new { InstanceDogPredicate },
            static (x, c) => c.Owner.Dogs.Any(x.Splice(x.Data.InstanceDogPredicate))
        );
    }

    [Benchmark]
    public void Arborist_Interpolate_Static() {
        ExpressionOn<Cat>.Interpolate(
            static (x, c) => c.Owner.Dogs.Any(x.Splice(StaticDogPredicate))
        );
    }

    [Benchmark]
    public void Arborist_Interpolate_Compiled() {
        ExpressionOn<Cat>.Interpolate(
            static (x, c) => c.Owner.Dogs.Any(x.Splice(ReflectivePartialSplicedExpressionEvaluator.Unsupported(StaticDogPredicate)))
        );
    }

    [Benchmark]
    public void LinqKit_Expand_Dynamic() {
        ExpressionOn<Cat>.Of(
            c => c.Owner.Dogs.Any(InstanceDogPredicate.Compile())
        )
        .Expand();
    }

    [Benchmark]
    public void LinqKit_Expand_Static() {
        ExpressionOn<Cat>.Of(
            static c => c.Owner.Dogs.Any(StaticDogPredicate.Compile())
        )
        .Expand();
    }
}
