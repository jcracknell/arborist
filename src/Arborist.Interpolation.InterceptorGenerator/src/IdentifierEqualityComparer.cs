namespace Arborist.Interpolation.InterceptorGenerator;

public class IdentifierEqualityComparer : IEqualityComparer<string> {
    public static IdentifierEqualityComparer Instance { get; } = new();

    private IdentifierEqualityComparer() { }

    private static int GetStartOffset(string value) =>
        value.Length != 0 && '@' == value[0] ? 1 : 0;

    public bool Equals(string? a, string? b) {
        if(a is null)
            return b is null;
        if(b is null)
            return false;

        var ao = GetStartOffset(a);
        var bo = GetStartOffset(b);
        if(a.Length - ao != b.Length - bo)
            return false;

        while(ao < a.Length) {
            if(a[ao] != b[bo])
                return false;

            ao += 1;
            bo += 1;
        }

        return true;
    }

    public int GetHashCode(string value) {
        var hash = new HashCode();
        hash.AddRange(value.AsSpan(GetStartOffset(value)));
        return hash.ToHashCode();
    }
}
