using Microsoft.Extensions.DependencyInjection;
using Terminus.Tests.Unit.TestFacades;
using Xunit;

namespace Terminus.Tests.Unit;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void Given_calling_assembly_Should_register_facade_implementations()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddTerminusFacades();
        var provider = services.BuildServiceProvider();

        // Assert
        var facade = provider.GetService<ITestFacade>();
        Assert.NotNull(facade);
        Assert.IsType<ITestFacade_Generated>(facade);
    }

    [Fact]
    public void Given_specific_assembly_Should_register_facades_from_that_assembly()
    {
        // Arrange
        var services = new ServiceCollection();
        var assembly = typeof(ITestFacade).Assembly;

        // Act
        services.AddTerminusFacades(assembly!);
        var provider = services.BuildServiceProvider();

        // Assert
        var facade = provider.GetService<ITestFacade>();
        Assert.NotNull(facade);
        Assert.IsType<ITestFacade_Generated>(facade);
    }

    [Fact]
    public void Given_disposable_facade_Should_register_as_scoped_by_default()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddTerminusFacades();

        // Assert
        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IScopedFacade));
        Assert.NotNull(descriptor);
        Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime);
    }

    [Fact]
    public void Given_async_disposable_facade_Should_register_as_scoped_by_default()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddTerminusFacades();

        // Assert
        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IAsyncScopedFacade));
        Assert.NotNull(descriptor);
        Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime);
    }

    [Fact]
    public void Given_non_disposable_facade_Should_register_as_transient_by_default()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddTerminusFacades();

        // Assert
        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(ITestFacade));
        Assert.NotNull(descriptor);
        Assert.Equal(ServiceLifetime.Transient, descriptor.Lifetime);
    }

    [Fact]
    public void Given_explicit_singleton_lifetime_Should_use_specified_lifetime()
    {
        // Arrange
        var services = new ServiceCollection();
        var assembly = typeof(ITestFacade).Assembly;

        // Act
        services.AddTerminusFacades(ServiceLifetime.Singleton, assembly!);

        // Assert
        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(ITestFacade));
        Assert.NotNull(descriptor);
        Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
    }

    [Fact]
    public void Given_explicit_scoped_lifetime_Should_override_default_transient()
    {
        // Arrange
        var services = new ServiceCollection();
        var assembly = typeof(ITestFacade).Assembly;

        // Act
        services.AddTerminusFacades(ServiceLifetime.Scoped, assembly!);

        // Assert
        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(ITestFacade));
        Assert.NotNull(descriptor);
        Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime);
    }

    [Fact]
    public void Given_explicit_lifetime_Should_override_default_scoped_for_disposable()
    {
        // Arrange
        var services = new ServiceCollection();
        var assembly = typeof(IScopedFacade).Assembly;

        // Act
        services.AddTerminusFacades(ServiceLifetime.Singleton, assembly!);

        // Assert
        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IScopedFacade));
        Assert.NotNull(descriptor);
        Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
    }

    [Fact]
    public void Given_multiple_assemblies_Should_scan_all()
    {
        // Arrange
        var services = new ServiceCollection();
        var assembly1 = typeof(ITestFacade).Assembly;
        // In this test, we're using the same assembly twice to verify multi-assembly support works
        var assembly2 = typeof(IAnotherFacade).Assembly;

        // Act
        services.AddTerminusFacades(assembly1!, assembly2!);

        // Assert - verify registrations from assemblies
        Assert.Contains(services, d => d.ServiceType == typeof(ITestFacade));
        Assert.Contains(services, d => d.ServiceType == typeof(IAnotherFacade));
    }

    [Fact]
    public void Given_duplicate_registration_Should_use_last_one_wins()
    {
        // Arrange
        var services = new ServiceCollection();
        var assembly = typeof(ITestFacade).Assembly;

        // Act - register twice with different lifetimes
        services.AddTerminusFacades(ServiceLifetime.Transient, assembly!);
        services.AddTerminusFacades(ServiceLifetime.Singleton, assembly!);

        // Assert - last one (Singleton) should be last in collection
        var descriptors = services.Where(d => d.ServiceType == typeof(ITestFacade)).ToList();
        Assert.Equal(2, descriptors.Count);
        Assert.Equal(ServiceLifetime.Singleton, descriptors.Last().Lifetime);
    }

    [Fact]
    public void Given_multiple_facades_Should_register_all()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddTerminusFacades();

        // Assert - verify all test facades are registered
        Assert.Contains(services, d => d.ServiceType == typeof(ITestFacade));
        Assert.Contains(services, d => d.ServiceType == typeof(IScopedFacade));
        Assert.Contains(services, d => d.ServiceType == typeof(IAsyncScopedFacade));
        Assert.Contains(services, d => d.ServiceType == typeof(IAnotherFacade));
    }

    [Fact]
    public void Given_service_provider_Should_resolve_facade_instances()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTerminusFacades();
        var provider = services.BuildServiceProvider();

        // Act
        var testFacade = provider.GetService<ITestFacade>();
        var scopedFacade = provider.CreateScope().ServiceProvider.GetService<IScopedFacade>();
        var anotherFacade = provider.GetService<IAnotherFacade>();

        // Assert
        Assert.NotNull(testFacade);
        Assert.NotNull(scopedFacade);
        Assert.NotNull(anotherFacade);
    }

    [Fact]
    public void Given_transient_facade_Should_create_new_instance_each_time()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTerminusFacades();
        var provider = services.BuildServiceProvider();

        // Act
        var instance1 = provider.GetService<ITestFacade>();
        var instance2 = provider.GetService<ITestFacade>();

        // Assert
        Assert.NotNull(instance1);
        Assert.NotNull(instance2);
        Assert.NotSame(instance1, instance2);
    }

    [Fact]
    public void Given_singleton_facade_Should_return_same_instance()
    {
        // Arrange
        var services = new ServiceCollection();
        var assembly = typeof(ITestFacade).Assembly;
        services.AddTerminusFacades(ServiceLifetime.Singleton, assembly!);
        var provider = services.BuildServiceProvider();

        // Act
        var instance1 = provider.GetService<ITestFacade>();
        var instance2 = provider.GetService<ITestFacade>();

        // Assert
        Assert.NotNull(instance1);
        Assert.NotNull(instance2);
        Assert.Same(instance1, instance2);
    }

    [Fact]
    public void Given_specific_facade_type_generic_Should_register_only_that_facade()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddTerminusFacade<ITestFacade>();
        var provider = services.BuildServiceProvider();

        // Assert - ITestFacade should be registered
        var testFacade = provider.GetService<ITestFacade>();
        Assert.NotNull(testFacade);
        Assert.IsType<ITestFacade_Generated>(testFacade);

        // Assert - Other facades should NOT be registered
        Assert.DoesNotContain(services, d => d.ServiceType == typeof(IScopedFacade));
        Assert.DoesNotContain(services, d => d.ServiceType == typeof(IAnotherFacade));
    }

    [Fact]
    public void Given_specific_facade_type_Should_register_only_that_facade()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddTerminusFacade(typeof(ITestFacade));
        var provider = services.BuildServiceProvider();

        // Assert - ITestFacade should be registered
        var testFacade = provider.GetService<ITestFacade>();
        Assert.NotNull(testFacade);
        Assert.IsType<ITestFacade_Generated>(testFacade);

        // Assert - Other facades should NOT be registered
        Assert.DoesNotContain(services, d => d.ServiceType == typeof(IScopedFacade));
        Assert.DoesNotContain(services, d => d.ServiceType == typeof(IAnotherFacade));
    }

    [Fact]
    public void Given_specific_facade_with_assemblies_Should_scan_those_assemblies()
    {
        // Arrange
        var services = new ServiceCollection();
        var assembly = typeof(ITestFacade).Assembly;

        // Act
        services.AddTerminusFacade<ITestFacade>(assembly!);
        var provider = services.BuildServiceProvider();

        // Assert
        var facade = provider.GetService<ITestFacade>();
        Assert.NotNull(facade);
        Assert.IsType<ITestFacade_Generated>(facade);
    }

    [Fact]
    public void Given_specific_facade_with_lifetime_Should_use_specified_lifetime()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddTerminusFacade<ITestFacade>(ServiceLifetime.Singleton);

        // Assert
        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(ITestFacade));
        Assert.NotNull(descriptor);
        Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
    }

    [Fact]
    public void Given_nonexistent_facade_type_Should_not_throw()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert - Should not throw
        services.AddTerminusFacade(typeof(INonExistentFacade));

        // Verify no registration occurred
        Assert.DoesNotContain(services, d => d.ServiceType == typeof(INonExistentFacade));
    }

    [Fact]
    public void Given_specific_disposable_facade_Should_respect_auto_lifetime_detection()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddTerminusFacade<IScopedFacade>();

        // Assert - Should auto-detect as Scoped due to IDisposable
        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IScopedFacade));
        Assert.NotNull(descriptor);
        Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime);
    }

    // Marker interface for testing nonexistent facades
    private interface INonExistentFacade { }
}
