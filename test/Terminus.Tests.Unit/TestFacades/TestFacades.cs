namespace Terminus.Tests.Unit.TestFacades;

// Custom attribute for test facade methods
public class TestFacadeMethodAttribute : Attribute;

// Test facade without disposal - should be Transient
[FacadeOf(typeof(TestFacadeMethodAttribute))]
public partial interface ITestFacade;

// Test facade with IDisposable - should be Scoped
[FacadeOf(typeof(TestFacadeMethodAttribute), Lifetime = FacadeLifetime.Scoped)]
public partial interface IScopedFacade;

// Test facade with IAsyncDisposable - should also be Scoped
[FacadeOf(typeof(TestFacadeMethodAttribute), Lifetime = FacadeLifetime.Scoped)]
public partial interface IAsyncScopedFacade;

// Another test facade for multi-facade tests
[FacadeOf(typeof(TestFacadeMethodAttribute))]
public partial interface IAnotherFacade;

// Implementation classes with facade methods
public class TestFacadeImplementations
{
    [TestFacadeMethod]
    public void TestMethod()
    {
        // Test implementation
    }
}

public class ScopedFacadeImplementations
{
    [TestFacadeMethod]
    public void ScopedMethod()
    {
        // Scoped test implementation
    }
}

public class AsyncScopedFacadeImplementations
{
    [TestFacadeMethod]
    public async Task AsyncScopedMethod()
    {
        await Task.CompletedTask;
    }
}

public class AnotherFacadeImplementations
{
    [TestFacadeMethod]
    public void AnotherMethod()
    {
        // Another facade implementation
    }
}
