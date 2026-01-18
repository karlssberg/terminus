namespace Terminus.Example.CrossProject.Dependent;

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