namespace Terminus.Example.CrossProject.Dependent;

public class MyStreamingService
{
    [MyTarget]
    public async IAsyncEnumerable<string> StreamDataAsync(int count)
    {
        for (var i = 0; i < count; i++)
        {
            await Task.Delay(50); // Simulate async operation
            yield return $"Streaming Data {i + 1}";
        }
    }
}