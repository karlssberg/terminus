namespace Terminus.Example.CrossProject.Dependent;

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