namespace DamerauLevenshteinDistance;

public sealed record DamerauLevenshteinOptions
{
    public int MaxInputLength { get; init; } = 400;

    public long MaxMatrixCells { get; init; } = 1_000_000;
}

public static class DamerauLevenshtein
{
    private static readonly DamerauLevenshteinOptions DefaultOptions = new();

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

        if (a.Length > options.MaxInputLength)
            throw new ArgumentOutOfRangeException(
                nameof(a),
                $"Input length exceeds the limit of {options.MaxInputLength}.");

        if (b.Length > options.MaxInputLength)
            throw new ArgumentOutOfRangeException(
                nameof(b),
                $"Input length exceeds the limit of {options.MaxInputLength}.");

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
        var costMatrix = new int[a.Length + 2, b.Length + 2];

        // Populate the matrix
        for (var rowIndex = 0; rowIndex < costMatrix.GetLength(0); rowIndex++)
            costMatrix[rowIndex, 0] = totalLength;

        for (var columnIndex = 0; columnIndex < costMatrix.GetLength(1); columnIndex++)
            costMatrix[0, columnIndex] = totalLength;

        for (var rowIndex = 1; rowIndex <= a.Length; rowIndex++)
            costMatrix[rowIndex + 1, 1] = rowIndex;

        for (var columnIndex = 1; columnIndex <= b.Length; columnIndex++)
            costMatrix[1, columnIndex + 1] = columnIndex;

        var lastRowByChar = new Dictionary<char, int>(a.Length);

        // Actual algorithm
        for (var rowIndex = 1; rowIndex <= a.Length; rowIndex++)
        {
            var lastMatchingColumn = 0;

            for (var columnIndex = 1; columnIndex <= b.Length; columnIndex++)
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

                costMatrix[rowIndex + 1, columnIndex + 1] = Math.Min(
                    Math.Min(deletion, insertion),
                    Math.Min(substitution, transposition));

                if (cost == 0)
                    lastMatchingColumn = columnIndex;
            }

            lastRowByChar[a[rowIndex - 1]] = rowIndex;
        }

        return costMatrix[a.Length + 1, b.Length + 1];
    }
}