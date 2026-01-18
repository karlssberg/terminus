namespace Terminus.Example.CrossProject.Dependent;

[AttributeUsage(AttributeTargets.Method)]
public class MyTargetAttribute : Attribute;

public class MyService
{
    [MyTarget]
    public string GetData(int id)
    {
        return $"Data for ID: {id}";
    }
    
    [MyTarget]
    public void SaveData(int id, string data)
    {
        // Simulate saving data
    }
}

public class MyAsyncService
{
    [MyTarget]
    public async Task<string> GetDataAsync(int id)
    {
        await Task.Delay(100); // Simulate async operation
        return $"Async Data for ID: {id}";
    }

    [MyTarget]
    public async Task SaveDataAsync(int id, string data)
    {
        await Task.Delay(100); // Simulate async operation
        // Simulate saving data
    }
}

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