using System.Drawing;

namespace UrlExtractor;

abstract class Program
{
    static async Task Main(string[] args)
    {
        string inputFilePath = "C:\\Users\\Yaroslav.Ryda\\RiderProjects\\UrlExtractor\\UrlExtractor\\Parser_ImageSize.csv";
        string outputFilePath = "image_urls.csv";

        try
        {
            List<string> urls = ReadUrlsFromCsv(inputFilePath);
            Dictionary<string, (int width, int height)> dimensions = await GetImageDimensionsAsync(urls);

            UpdateCsv(inputFilePath, outputFilePath, dimensions);

            Console.WriteLine("Image dimension processing completed.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred: {ex.Message}");
        }
    }

    static List<string> ReadUrlsFromCsv(string filePath)
    {
        return File.ReadAllLines(filePath).ToList();
    }

    static async Task<Dictionary<string, (int width, int height)>> GetImageDimensionsAsync(List<string> urls)
    {
        var dimensions = new Dictionary<string, (int width, int height)>();

        using (var httpClient = new HttpClient())
        {
            httpClient.Timeout = TimeSpan.FromSeconds(10); // Set timeout to 10 seconds
            httpClient.MaxResponseContentBufferSize = 1024 * 1024 * 10; // 10 MB

            // Divide batch size
            int batchSize = 1000;
            var batches = urls.SplitIntoBatches(batchSize);

            // Process batches concurrently
            var tasks = batches.Select(batch => GetBatchDimensionsAsync(httpClient, batch));
            var results = await Task.WhenAll(tasks);

            // Merge results from all batches
            foreach (var result in results)
            {
                foreach (var kvp in result)
                {
                    dimensions[kvp.Key] = kvp.Value;
                }
            }
        }

        return dimensions;
    }

    static async Task<Dictionary<string, (int width, int height)>> GetBatchDimensionsAsync(HttpClient httpClient, IEnumerable<string> urls)
    {
        var dimensions = new Dictionary<string, (int width, int height)>();

        // Process URLs in the batch concurrently
        var tasks = urls.Select(url => GetImageDimensionsAsync(httpClient, url));
        var results = await Task.WhenAll(tasks);

        // Aggregate results from the batch
        foreach (var result in results)
        {
            dimensions[result.Key] = result.Value;
        }

        return dimensions;
    }

    static async Task<KeyValuePair<string, (int width, int height)>> GetImageDimensionsAsync(HttpClient httpClient, string url)
    {
        try
        {
            using (HttpResponseMessage response = await httpClient.GetAsync(url))
            using (Stream stream = await response.Content.ReadAsStreamAsync())
            using (Image image = Image.FromStream(stream))
            {
                return new KeyValuePair<string, (int width, int height)>(url, (image.Width, image.Height));
            }
        }
        catch
        {
            return new KeyValuePair<string, (int width, int height)>(url, (-1, -1));
        }
    }

    static void UpdateCsv(string inputFilePath, string outputFilePath, Dictionary<string, (int width, int height)> dimensions)
    {
        using (StreamWriter writer = new StreamWriter(outputFilePath))
        {
            foreach (var kvp in dimensions)
            {
                if (kvp.Value != (-1, -1))
                {
                    writer.WriteLine($"{kvp.Key},{kvp.Value.width},{kvp.Value.height}");
                }
                else
                {
                    writer.WriteLine($"{kvp.Key},Dimensions not found");
                }
            }
        }

        File.Move(outputFilePath, inputFilePath, true);
    }
}