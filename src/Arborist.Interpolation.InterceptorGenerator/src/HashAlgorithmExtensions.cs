using System.Security.Cryptography;
using System.Text;

namespace Arborist.Interpolation.InterceptorGenerator;

internal static class HashAlgorithmExtensions {
    /// <summary>
    /// Applies the provided string <paramref name="value"/> to the subject <see cref="HashAlgorithm"/>,
    /// converting the string to bytes using the specified <paramref name="encoding"/>.
    /// </summary>
    public static void TransformString(
        this HashAlgorithm hash,
        string value,
        Encoding encoding,
        byte[] buffer
    ) {
        var charOffset = 0;
        var chunkSize = buffer.Length / encoding.GetMaxByteCount(1);
        while(charOffset < value.Length) {
            var charCount = Math.Min(value.Length - charOffset, chunkSize);
            var byteCount = encoding.GetBytes(value, charOffset, charCount, buffer, 0);
            hash.TransformBlock(buffer, 0, byteCount, null, 0);
            charOffset += charCount;
        }
    }
}
