namespace UrlExtractor;

internal static class Extensions
{
    public static IEnumerable<IEnumerable<T>> SplitIntoBatches<T>(this IEnumerable<T> source, int batchSize)
    {
        var enumerator = source.GetEnumerator();
        while (enumerator.MoveNext())
        {
            yield return GetBatch(enumerator, batchSize);
        }
    }

    private static IEnumerable<T> GetBatch<T>(IEnumerator<T> enumerator, int batchSize)
    {
        do
        {
            yield return enumerator.Current;
        } while (--batchSize > 0 && enumerator.MoveNext());
    }
}