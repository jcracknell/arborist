using Xunit.Sdk;

namespace Arborist.CodeGen;

public static class CodeGenAssert {
    public static void CodeEqual(string expected, string actual) {
        var actualIndex = 0;
        var expectedIndex = 0;
        var actualLength = actual.Length;
        var expectedLength = expected.Length;

        while(expectedIndex < expectedLength && actualIndex < actualLength) {
            var expectedChar = expected[expectedIndex];
            var actualChar = actual[actualIndex];

            if(Char.IsWhiteSpace(expectedChar) && IsWhitespaceEquivalent(actual, actualIndex)) {
                expectedIndex = SkipWhitespace(expected, expectedIndex);
                actualIndex = SkipWhitespace(actual, actualIndex);
            } else if(Char.IsWhiteSpace(actualChar) && IsWhitespaceEquivalent(expected, expectedIndex)) {
                expectedIndex = SkipWhitespace(expected, expectedIndex);
                actualIndex = SkipWhitespace(actual, actualIndex);
            } else if(expectedChar != actualChar) {
                break;
            } else {
                expectedIndex += 1;
                actualIndex += 1;
            }
        }

        // Skip trailing whitespace occurring in one string only
        if(expectedIndex != expectedLength && actualIndex == actualLength)
            expectedIndex = SkipWhitespace(expected, expectedIndex);
        if(actualIndex != actualLength && expectedIndex == expectedLength)
            actualIndex = SkipWhitespace(actual, actualIndex);

        if(expectedIndex != expectedLength || actualIndex != actualLength)
            throw EqualException.ForMismatchedStrings(expected, actual, expectedIndex, actualIndex);
    }

    private static int SkipWhitespace(string str, int index) {
        while(index < str.Length && Char.IsWhiteSpace(str[index]))
            index += 1;

        return index;
    }

    private static bool IsWhitespaceEquivalent(string str, int index) {
        if(index == 0)
            return true;
        if(index == str.Length)
            return true;
        if(Char.IsWhiteSpace(str[index]))
            return true;
        // Space preceding a symbol
        if(!Char.IsLetterOrDigit(str[index]))
            return true;
        // Space following a symbol
        if(!Char.IsLetterOrDigit(str[index - 1]))
            return true;

        return false;
    }
}
