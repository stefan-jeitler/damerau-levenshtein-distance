namespace DamerauLevenshteinDistance;

public static class DamerauLevenshtein
{
    public static int CalculateDistance(string a, string b)
    {
        ArgumentNullException.ThrowIfNull(a);
        ArgumentNullException.ThrowIfNull(b);

        if (a.Length == 0)
            return b.Length;

        if (b.Length == 0)
            return a.Length;

        var costMatrix = Enumerable.Range(0, a.Length + 1)
            .Select(x => new int[b.Length + 1])
            .ToArray();

        for (var rowIndex = 1; rowIndex <= a.Length; rowIndex++)
            costMatrix[rowIndex][0] = rowIndex;

        for (var columnIndex = 1; columnIndex <= b.Length; columnIndex++)
            costMatrix[0][columnIndex] = columnIndex;

        for (var rowIndex = 1; rowIndex <= a.Length; rowIndex++)
        for (var columnIndex = 1; columnIndex <= b.Length; columnIndex++)
        {
            var cost = a[rowIndex - 1] == b[columnIndex - 1] ? 0 : 1;

            var insertion = costMatrix[rowIndex][columnIndex - 1] + 1;
            var deletion = costMatrix[rowIndex - 1][columnIndex] + 1;
            var substitution = costMatrix[rowIndex - 1][columnIndex - 1] + cost;

            costMatrix[rowIndex][columnIndex] = Math.Min(insertion, Math.Min(deletion, substitution));
        }

        return costMatrix[a.Length][b.Length];
    }
}