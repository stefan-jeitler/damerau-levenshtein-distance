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

        var costMatrix = new int[a.Length + 1, b.Length + 1];

        for (var rowIndex = 1; rowIndex <= a.Length; costMatrix[rowIndex, 0] = rowIndex++) ;
        for (var columnIndex = 1; columnIndex <= b.Length; costMatrix[0, columnIndex] = columnIndex++) ;

        for (var rowIndex = 1; rowIndex <= a.Length; rowIndex++)
        for (var columnIndex = 1; columnIndex <= b.Length; columnIndex++)
        {
            var cost = a[rowIndex - 1] == b[columnIndex - 1] ? 0 : 1;

            var insertion = costMatrix[rowIndex, columnIndex - 1] + 1;
            var deletion = costMatrix[rowIndex - 1, columnIndex] + 1;
            var substitution = costMatrix[rowIndex - 1, columnIndex - 1] + cost;
            
            var distance = Math.Min(insertion, Math.Min(deletion, substitution));

            if (rowIndex > 1 && columnIndex > 1 &&
                a[rowIndex - 1] == b[columnIndex - 2] &&
                a[rowIndex - 2] == b[columnIndex - 1])
            {
                var transposition = costMatrix[rowIndex - 2, columnIndex - 2] + 1;
                distance = Math.Min(distance, transposition);
            }

            costMatrix[rowIndex, columnIndex] = distance;
        }

        return costMatrix[a.Length, b.Length];
    }
}