namespace DamerauLevenshteinDistance.Tests;

public class DamerauLevenshteinTests
{
    [Theory]
    [InlineData("", "", 0)]
    [InlineData("", "abc", 3)]
    [InlineData("abc", "", 3)]
    [InlineData("ab", "ba", 1)]
    [InlineData("haus", "maus", 1)]
    [InlineData("test", "test", 0)]
    public void CalculateDistance(string a, string b, int expectedDistance)
    {
        var distance = DamerauLevenshtein.CalculateDistance(a, b);

        Assert.Equal(expectedDistance, distance);
    }
}