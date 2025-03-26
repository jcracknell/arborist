namespace Arborist.Analyzers;

public class InterpolationAnalyzerTests {
    [Fact]
    public async Task Should_produce_diagnostics_for_invocation_with_named_parameters() {
        await InterpolationAnalyzerTestBuilder.Create()
        .Generate(@"
            ExpressionOn<Cat>.Interpolate(
                data: default(object),
                expression: {|ARB001:(x, c) => {|ARB002:x|}|}
            );
        ");
    }

    [Fact]
    public async Task Should_produce_diagnostics_for_invocation_with_out_of_order_named_parameters() {
        await InterpolationAnalyzerTestBuilder.Create()
        .Generate(@"
            ExpressionOn<Cat>.Interpolate(
                expression: {|ARB001:(x, c) => {|ARB002:x|}|},
                data: default(object)
            );
        ");
    }

    [Fact]
    public async Task Should_produce_ARB001_for_ExpressionOnNone_with_no_splices() {
        await InterpolationAnalyzerTestBuilder.Create()
        .Generate(@"
            ExpressionOnNone.Interpolate({|ARB001:x => 42|});
        ");
    }

    [Fact]
    public async Task Should_produce_ARB001_for_SelectInterpolated_with_no_splices() {
        await InterpolationAnalyzerTestBuilder.Create()
        .Generate(@"
            default(IQueryable<Cat>)!.SelectInterpolated({|ARB001:(x, c) => c.Id|});
        ");
    }

    [Fact]
    public async Task Should_produce_ARB001_for_splice_using_nested_context() {
        await InterpolationAnalyzerTestBuilder.Create()
        .Generate(@"
            ExpressionOn<Cat>.Interpolate(
                {|ARB001:(x, c) => c.Owner.Dogs.AsQueryable().Any(
                    {|ARB004:ExpressionOn<Dog>.Interpolate(
                        (x, d) => x.SpliceConstant(true)
                    )|}
                )|}
            );
        ");
    }

    [Fact]
    public async Task Should_produce_ARB002_for_ExpressionOnNone_with_bare_context_reference() {
        await InterpolationAnalyzerTestBuilder.Create()
        .Generate(@"
            #pragma warning disable ARB001
            ExpressionOnNone.Interpolate(x => {|ARB002:x|});
            #pragma warning restore
        ");
    }

    [Fact]
    public async Task Should_produce_ARB002_for_context_reference_in_evaluated_splice_argument() {
        await InterpolationAnalyzerTestBuilder.Create()
        .Generate(@"
            ExpressionOn<Owner>.Interpolate(
                (x, o) => x.SpliceBody(o, o => {|ARB002:x|})
            );
        ");
    }

    [Fact]
    public async Task Should_produce_ARB002_for_context_reference_in_interpolated_splice_argument() {
        await InterpolationAnalyzerTestBuilder.Create()
        .Generate(@"
            ExpressionOnNone.Interpolate(
                new { Predicate = ExpressionOn<IInterpolationContext>.Of(x => true) },
                x => x.SpliceBody({|ARB002:x|}, x.Data.Predicate)
            );
        ");
    }

    [Fact]
    public async Task Should_produce_ARB002_for_unevaluated_data_reference() {
        await InterpolationAnalyzerTestBuilder.Create()
        .Generate(@"
            ExpressionOnNone.Interpolate(
                ""foo"",
                x => {|ARB002:x|}.Data + x.SpliceConstant(""bar"")
            );
        ");
    }

    [Fact]
    public async Task Should_produce_ARB002_for_data_reference_in_interpolated_splice_arg() {
        await InterpolationAnalyzerTestBuilder.Create()
        .Generate(@"
            ExpressionOnNone.Interpolate(
                ""foo"",
                x => x.SpliceBody({|ARB002:x|}.Data, v => v.GetHashCode())
            );
        ");
    }


    [Fact]
    public async Task Should_not_produce_ARB002_for_splice_in_interpolated_splice_argument() {
        await InterpolationAnalyzerTestBuilder.Create()
        .Generate(@"
            ExpressionOn<Cat>.Interpolate(
                new {
                    CatOwner = ExpressionOn<Cat>.Of(c => c.Owner),
                    OwnerPredicate = ExpressionOn<Owner>.Of(o => o.Id == 42)
                },
                (x, c) => x.SpliceBody(x.SpliceBody(c, x.Data.CatOwner), x.Data.OwnerPredicate)
            );
        ");
    }

    [Fact]
    public async Task Should_not_produce_ARB002_for_shadowed_context_reference() {
        await InterpolationAnalyzerTestBuilder.Create()
        .Generate(@"
            #pragma warning disable ARB001
            ExpressionOnNone.Interpolate(
                x => Array.Empty<string>().Select(x => x.GetHashCode())
            );
            #pragma warning restore
        ");
    }

    [Fact]
    public async Task Should_produce_ARB003_for_interpolated_parameter_reference_in_evaluated_expression() {
        await InterpolationAnalyzerTestBuilder.Create()
        .Generate(@"
            ExpressionOn<Owner>.Interpolate(
                (x, o) => x.SpliceBody(o, p => {|ARB003:o|})
            );
        ");
    }

    [Fact]
    public async Task Should_not_produce_ARB003_for_shadowed_parameter_reference_in_evaluated_expression() {
        await InterpolationAnalyzerTestBuilder.Create()
        .Generate(@"
            ExpressionOn<Owner>.Interpolate(
                (x, o) => x.SpliceBody(o, o => o)
            );
        ");
    }

    [Fact]
    public async Task Should_produce_ARB004_for_nested_interpolation_call() {
        await InterpolationAnalyzerTestBuilder.Create()
        .Generate(@"
            ExpressionOn<IQueryable<Cat>>.Interpolate(
                (x, q) => q.SelectMany(
                    c => x.SpliceBody(c, c => c.Owner.Dogs),
                    {|ARB004:ExpressionOn<Cat, Dog>.Interpolate((x, c, d) => x.SpliceConstant(42))|}
                )
            );
        ");
    }

    [Fact]
    public async Task Should_produce_ARB004_for_nested_SelectInterpolated() {
        await InterpolationAnalyzerTestBuilder.Create()
        .Generate(@"
            ExpressionOn<IQueryable<Cat>>.Interpolate(
                (x, q) => {|ARB004:q.SelectInterpolated((y, c) => x.SpliceConstant(1) + y.SpliceConstant(1))|}
            );
        ");
    }

    [Fact]
    public async Task Should_not_produce_ARB004_for_nested_interpolation_in_evaluated_splice_parameter() {
        await InterpolationAnalyzerTestBuilder.Create()
        .Generate(@"
            ExpressionOn<Cat>.Interpolate(
                (x, c) => x.SpliceBody(
                    c,
                    ExpressionOn<Cat>.Interpolate((x, c) => x.SpliceConstant(42))
                )
            );
        ");
    }

    [Fact]
    public async Task Should_produce_ARB004_for_nested_interpolation_in_interpolated_splice_parameter() {
        await InterpolationAnalyzerTestBuilder.Create()
        .Generate(@"
            ExpressionOn<Cat>.Interpolate(
                (x, c) => x.SpliceBody(
                    c.Owner.Dogs.AsQueryable().Any(
                        {|ARB004:ExpressionOn<Dog>.Interpolate((x, d) => x.SpliceConstant(true))|}
                    ),
                    ExpressionOn<bool>.Of(v => v)
                )
            );
        ");
    }
}
