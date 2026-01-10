namespace Terminus.Tests.Unit.TestFacades;

// Test facade without disposal
public interface ITestFacade;

[FacadeImplementation(typeof(ITestFacade))]
public sealed class ITestFacade_Generated : ITestFacade
{
    public ITestFacade_Generated(IServiceProvider serviceProvider)
    {
    }
}

// Test facade with IDisposable (should be scoped)
public interface IScopedFacade;

[FacadeImplementation(typeof(IScopedFacade))]
public sealed class IScopedFacade_Generated : IScopedFacade, IDisposable
{
    public IScopedFacade_Generated(IServiceProvider serviceProvider)
    {
    }

    public void Dispose()
    {
    }
}

// Test facade with IAsyncDisposable (should also be scoped)
public interface IAsyncScopedFacade;

[FacadeImplementation(typeof(IAsyncScopedFacade))]
public sealed class IAsyncScopedFacade_Generated : IAsyncScopedFacade, IAsyncDisposable
{
    public IAsyncScopedFacade_Generated(IServiceProvider serviceProvider)
    {
    }

    public ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }
}

// Another test facade for multi-facade tests
public interface IAnotherFacade;

[FacadeImplementation(typeof(IAnotherFacade))]
public sealed class IAnotherFacade_Generated : IAnotherFacade
{
    public IAnotherFacade_Generated(IServiceProvider serviceProvider)
    {
    }
}
