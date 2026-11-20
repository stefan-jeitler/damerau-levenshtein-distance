namespace DamerauLevenshteinDistance.Tests;

public class DamerauLevenshteinTests
{
    [Theory]
    [InlineData("", "", 0)]
    [InlineData("test", "test", 0)]
    public void Identical_strings_have_zero_distance(
        string a,
        string b,
        int expectedDistance)
    {
        var distance = DamerauLevenshtein.CalculateDistance(a, b);

        Assert.Equal(expectedDistance, distance);
    }

    [Theory]
    [InlineData("", "abc", 3)]
    [InlineData("abc", "", 3)]
    public void Distance_from_an_empty_string_is_the_other_strings_length(
        string a,
        string b,
        int expectedDistance)
    {
        var distance = DamerauLevenshtein.CalculateDistance(a, b);

        Assert.Equal(expectedDistance, distance);
    }

    [Theory]
    [InlineData("haus", "maus", 1)]
    [InlineData("kitten", "sitting", 3)]
    public void Substitutions_and_other_basic_edits_contribute_to_the_distance(
        string a,
        string b,
        int expectedDistance)
    {
        var distance = DamerauLevenshtein.CalculateDistance(a, b);

        Assert.Equal(expectedDistance, distance);
    }

    [Fact]
    public void Adjacent_transposed_characters_have_distance_one()
    {
        var distance = DamerauLevenshtein.CalculateDistance("ab", "ba");

        Assert.Equal(1, distance);
    }

    [Fact]
    public void Transposition_can_combine_with_an_insertion()
    {
        // Distinguishes unrestricted Damerau–Levenshtein from OSA.
        var distance = DamerauLevenshtein.CalculateDistance("CA", "ABC");

        Assert.Equal(2, distance);
    }

    [Fact]
    public void Two_independent_transpositions_each_contribute_one()
    {
        var distance = DamerauLevenshtein.CalculateDistance("abcd", "badc");

        Assert.Equal(2, distance);
    }

    [Fact]
    public void Repeated_characters_can_be_transposed()
    {
        var distance = DamerauLevenshtein.CalculateDistance("aab", "aba");

        Assert.Equal(1, distance);
    }

    [Fact]
    public void Deleting_one_of_several_repeated_characters_costs_one()
    {
        var distance = DamerauLevenshtein.CalculateDistance("aaaa", "aaa");

        Assert.Equal(1, distance);
    }

    [Fact]
    public void Uppercase_and_lowercase_letters_are_distinct()
    {
        var distance = DamerauLevenshtein.CalculateDistance("A", "a");

        Assert.Equal(1, distance);
    }

    [Fact]
    public void A_bmp_accented_letter_can_be_compared()
    {
        var distance = DamerauLevenshtein.CalculateDistance("café", "cafe");

        Assert.Equal(1, distance);
    }

    [Theory]
    [InlineData("ab", "ba")]
    [InlineData("CA", "ABC")]
    [InlineData("kitten", "sitting")]
    [InlineData("aab", "aba")]
    public void Reversing_the_strings_preserves_the_distance(string a, string b)
    {
        var distanceFromAToB = DamerauLevenshtein.CalculateDistance(a, b);
        var distanceFromBToA = DamerauLevenshtein.CalculateDistance(b, a);

        Assert.Equal(distanceFromAToB, distanceFromBToA);
    }

    [Fact]
    public void Null_first_string_is_rejected()
    {
        var exception = Assert.Throws<ArgumentNullException>(
            () => DamerauLevenshtein.CalculateDistance(null!, "a"));

        Assert.Equal("a", exception.ParamName);
    }

    [Fact]
    public void Null_second_string_is_rejected()
    {
        var exception = Assert.Throws<ArgumentNullException>(
            () => DamerauLevenshtein.CalculateDistance("a", null!));

        Assert.Equal("b", exception.ParamName);
    }
    
    [Theory]
    [InlineData("CA", "ABC", 1, false)]
    [InlineData("CA", "ABC", 2, true)]
    [InlineData("haus", "häuser", 2, false)]
    [InlineData("haus", "häuser", 3, true)]
    [InlineData("xxxxxx", "yyyyyy", 2, false)]
    public void Threshold_matches_expected_tolerance_on_edge_cases(
        string a,
        string b,
        int maxDistance,
        bool shouldBeWithinThreshold)
    {
        var options = new DamerauLevenshteinOptions { MaxDistance = maxDistance };
        var distance = DamerauLevenshtein.CalculateDistance(a, b, options);

        if (shouldBeWithinThreshold)
            Assert.InRange(distance, 0, maxDistance);
        else
            Assert.True(distance > maxDistance);
    }

    [Theory]
    [InlineData("abxyzcd", "cdxyzab", 2)]
    [InlineData("abxyzcd", "cdxyzab", 3)]
    [InlineData("kitten", "sitting", 2)]
    [InlineData("kitten", "sitting", 3)]
    public void Bounded_calculation_never_underestimates_unbounded_distance(
        string a,
        string b,
        int maxDistance)
    {
        var exactDistance = DamerauLevenshtein.CalculateDistance(a, b);
        var boundedDistance = DamerauLevenshtein.CalculateDistance(
            a,
            b,
            new DamerauLevenshteinOptions { MaxDistance = maxDistance });

        if (exactDistance <= maxDistance)
            Assert.Equal(exactDistance, boundedDistance);
        else
            Assert.True(boundedDistance > maxDistance);
    }
}
