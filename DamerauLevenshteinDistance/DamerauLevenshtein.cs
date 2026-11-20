namespace DamerauLevenshteinDistance;

/// <summary>
/// Configuration options for <see cref="DamerauLevenshtein"/> calculations.
/// </summary>
public sealed record DamerauLevenshteinOptions
{
    /// <summary>
    /// Maximum allowed length for each input string. Defaults to <c>400</c>.
    /// </summary>
    public int MaxInputLength { get; init; } = 400;

    /// <summary>
    /// Maximum allowed number of cells in the dynamic programming matrix. Defaults to <c>1_000_000</c>.
    /// </summary>
    public long MaxMatrixCells { get; init; } = 1_000_000;

    /// <summary>
    /// Maximum allowed edit distance threshold. When set, computation may stop early
    /// and return <c>MaxDistance + 1</c> if the threshold is exceeded.
    /// <see langword="null"/> indicates no distance limit.
    /// </summary>
    public int? MaxDistance { get; init; }
}

/// <summary>
/// Provides methods to calculate the unrestricted Damerau-Levenshtein edit distance
/// and check typo tolerance between strings.
/// </summary>
public static class DamerauLevenshtein
{
    private static readonly DamerauLevenshteinOptions DefaultOptions = new();

    /// <summary>
    /// Calculates the unrestricted Damerau-Levenshtein distance between two strings,
    /// stopping early and returning <c>MaxDistance + 1</c> when
    /// <see cref="DamerauLevenshteinOptions.MaxDistance"/> is exceeded.
    /// </summary>
    /// <param name="a">The first input string to compare.</param>
    /// <param name="b">The second input string to compare.</param>
    /// <param name="options">
    /// Optional configuration controlling input length limits, maximum matrix size,
    /// and the distance threshold (<see cref="DamerauLevenshteinOptions.MaxDistance"/>).
    /// </param>
    /// <returns>
    /// The edit distance between <paramref name="a"/> and <paramref name="b"/> when within
    /// <see cref="DamerauLevenshteinOptions.MaxDistance"/> (or when no threshold is configured).
    /// When a threshold is set and the actual distance exceeds it, computation may abort early
    /// and returns <c>MaxDistance + 1</c> so callers can distinguish an exact boundary match
    /// (<c>result == MaxDistance</c>) from an exceeded threshold (<c>result &gt; MaxDistance</c>).
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="a"/> or <paramref name="b"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <see cref="DamerauLevenshteinOptions.MaxInputLength"/> is negative,
    /// <see cref="DamerauLevenshteinOptions.MaxMatrixCells"/> is less than or equal to zero,
    /// <see cref="DamerauLevenshteinOptions.MaxDistance"/> is negative, or either input
    /// string exceeds <see cref="DamerauLevenshteinOptions.MaxInputLength"/>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when the required distance matrix exceeds
    /// <see cref="DamerauLevenshteinOptions.MaxMatrixCells"/>.
    /// </exception>
    public static int CalculateDistance(
        string a,
        string b,
        DamerauLevenshteinOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(a);
        ArgumentNullException.ThrowIfNull(b);

        options ??= DefaultOptions;

        if (options.MaxInputLength < 0)
            throw new ArgumentOutOfRangeException(
                nameof(options),
                "MaxInputLength cannot be negative.");

        if (options.MaxMatrixCells <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(options),
                "MaxMatrixCells must be greater than zero.");

        if (options.MaxDistance is < 0)
            throw new ArgumentOutOfRangeException(
                nameof(options),
                "MaxDistance cannot be negative.");

        if (a.Length > options.MaxInputLength)
            throw new ArgumentOutOfRangeException(
                nameof(a),
                $"Input length exceeds the limit of {options.MaxInputLength}.");

        if (b.Length > options.MaxInputLength)
            throw new ArgumentOutOfRangeException(
                nameof(b),
                $"Input length exceeds the limit of {options.MaxInputLength}.");

        var maxDistance = options.MaxDistance;
        var lengthDiff = Math.Abs(a.Length - b.Length);

        if (maxDistance.HasValue && lengthDiff > maxDistance.Value)
            return maxDistance.Value + 1;

        if (a.Length == 0)
            return b.Length;

        if (b.Length == 0)
            return a.Length;

        var rowCount = (long)a.Length + 2;
        var columnCount = (long)b.Length + 2;

        if (rowCount * columnCount > options.MaxMatrixCells)
            throw new ArgumentException(
                $"The distance matrix would exceed the limit of {options.MaxMatrixCells} cells.");

        var totalLength = a.Length + b.Length;
        // Keep sentinel above any valid distance so uncomputed cells outside the band
        // do not win Math.Min(...) and do not falsely trigger early exit.
        var sentinelCost = maxDistance.HasValue
            ? Math.Max(maxDistance.Value + 2, totalLength + 1)
            : totalLength + 1;

        var costMatrix = new int[a.Length + 2, b.Length + 2];

        for (var rowIndex = 0; rowIndex < costMatrix.GetLength(0); rowIndex++)
            for (var columnIndex = 0; columnIndex < costMatrix.GetLength(1); columnIndex++)
                costMatrix[rowIndex, columnIndex] = sentinelCost;

        // Preserve the base DP anchor at (1, 1) while guarding row 0 and column 0.
        costMatrix[1, 1] = 0;

        for (var rowIndex = 0; rowIndex < costMatrix.GetLength(0); rowIndex++)
            costMatrix[rowIndex, 0] = totalLength;

        for (var columnIndex = 0; columnIndex < costMatrix.GetLength(1); columnIndex++)
            costMatrix[0, columnIndex] = totalLength;

        for (var rowIndex = 1; rowIndex <= a.Length; rowIndex++)
            costMatrix[rowIndex + 1, 1] = rowIndex;

        for (var columnIndex = 1; columnIndex <= b.Length; columnIndex++)
            costMatrix[1, columnIndex + 1] = columnIndex;

        var lastRowByChar = new Dictionary<char, int>(a.Length);

        for (var rowIndex = 1; rowIndex <= a.Length; rowIndex++)
        {
            var lastMatchingColumn = 0;

            // Restrict the column window around the current row when MaxDistance is set.
            var minCol = 1;
            var maxCol = b.Length;

            if (maxDistance.HasValue)
            {
                var k = maxDistance.Value;
                minCol = Math.Max(1, rowIndex - k);
                maxCol = Math.Min(b.Length, rowIndex + k);
            }

            var rowMinCost = sentinelCost;

            for (var columnIndex = minCol; columnIndex <= maxCol; columnIndex++)
            {
                var i1 = lastRowByChar.GetValueOrDefault(
                    b[columnIndex - 1],
                    0);

                var j1 = lastMatchingColumn;

                var cost = a[rowIndex - 1] == b[columnIndex - 1] ? 0 : 1;

                var deletion = costMatrix[rowIndex, columnIndex + 1] + 1;
                var insertion = costMatrix[rowIndex + 1, columnIndex] + 1;
                var substitution = costMatrix[rowIndex, columnIndex] + cost;

                var transposition =
                    costMatrix[i1, j1]
                    + (rowIndex - i1 - 1)
                    + 1
                    + (columnIndex - j1 - 1);

                var cellCost = Math.Min(
                    Math.Min(deletion, insertion),
                    Math.Min(substitution, transposition));

                costMatrix[rowIndex + 1, columnIndex + 1] = cellCost;

                if (cellCost < rowMinCost)
                    rowMinCost = cellCost;

                if (cost == 0)
                    lastMatchingColumn = columnIndex;
            }

            // If even the smallest cost in this row exceeds MaxDistance,
            // the final distance can no longer stay within the threshold.
            if (maxDistance.HasValue && rowMinCost > maxDistance.Value)
                return maxDistance.Value + 1;

            lastRowByChar[a[rowIndex - 1]] = rowIndex;
        }

        var result = costMatrix[a.Length + 1, b.Length + 1];

        if (maxDistance.HasValue && result > maxDistance.Value)
            return maxDistance.Value + 1;

        return result;
    }

    /// <summary>
    /// Calculates the unrestricted Damerau-Levenshtein distance between two strings using
    /// a fixed distance threshold, stopping early and returning <paramref name="maxDistance"/> + 1
    /// if the threshold is exceeded.
    /// </summary>
    /// <param name="a">The first input string to compare.</param>
    /// <param name="b">The second input string to compare.</param>
    /// <param name="maxDistance">Maximum allowed edit distance threshold.</param>
    /// <param name="baseOptions">
    /// Optional base configuration whose <see cref="DamerauLevenshteinOptions.MaxDistance"/>
    /// is overridden with <paramref name="maxDistance"/>.
    /// </param>
    /// <returns>
    /// The edit distance if within <paramref name="maxDistance"/>; otherwise
    /// <paramref name="maxDistance"/> + 1.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="maxDistance"/> is negative or another option/input limit is violated.
    /// </exception>
    public static int CalculateDistance(
        string a,
        string b,
        int maxDistance,
        DamerauLevenshteinOptions? baseOptions = null)
    {
        var options = baseOptions is null
            ? new DamerauLevenshteinOptions { MaxDistance = maxDistance }
            : baseOptions with { MaxDistance = maxDistance };

        return CalculateDistance(a, b, options);
    }

    /// <summary>
    /// Determines whether two strings are within the specified typo distance threshold.
    /// </summary>
    /// <param name="a">The first input string to compare.</param>
    /// <param name="b">The second input string to compare.</param>
    /// <param name="maxDistance">Maximum allowed edit distance threshold (inclusive).</param>
    /// <param name="baseOptions">
    /// Optional base configuration whose <see cref="DamerauLevenshteinOptions.MaxDistance"/>
    /// is overridden with <paramref name="maxDistance"/>.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if the edit distance between <paramref name="a"/> and
    /// <paramref name="b"/> is less than or equal to <paramref name="maxDistance"/>;
    /// otherwise <see langword="false"/>.
    /// </returns>
    public static bool IsWithinThreshold(
        string a,
        string b,
        int maxDistance,
        DamerauLevenshteinOptions? baseOptions = null)
    {
        return CalculateDistance(a, b, maxDistance, baseOptions) <= maxDistance;
    }

    /// <summary>
    /// Suggests a default typo distance threshold for typical words (commonly up to 30-40 characters):
    /// <c>1</c> edit for short words (length up to 5) and <c>2</c> edits for longer words.
    /// </summary>
    /// <param name="wordLength">Length of the reference or input word.</param>
    /// <returns>Suggested maximum allowed edit distance (<c>1</c> or <c>2</c>).</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="wordLength"/> is negative.
    /// </exception>
    public static int GetDefaultTypoThreshold(int wordLength)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(wordLength);

        return wordLength <= 5 ? 1 : 2;
    }
}