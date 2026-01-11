# Testing Facades

This guide covers strategies for testing Terminus facades and the services they aggregate.

## Testing Strategies

### 1. Test Services Directly (Recommended)

Test your services in isolation without involving the facade:

```csharp
public class UserServiceTests
{
    [Fact]
    public async Task GetUserAsync_WhenUserExists_ReturnsUser()
    {
        // Arrange
        var mockRepo = new Mock<IUserRepository>();
        mockRepo.Setup(r => r.FindAsync(123))
            .ReturnsAsync(new User { Id = 123, Name = "John" });

        var service = new UserService(mockRepo.Object);

        // Act
        var user = await service.GetUserAsync(123);

        // Assert
        Assert.NotNull(user);
        Assert.Equal("John", user.Name);
    }
}
```

**Why test services directly?**
- Simpler test setup
- Faster execution (no DI container)
- Clearer failure messages
- Tests business logic, not infrastructure

### 2. Integration Tests with Facade

Test the complete facade integration when needed:

```csharp
public class FacadeIntegrationTests
{
    [Fact]
    public async Task Facade_IntegratesServicesCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTransient<UserService>();
        services.AddTransient<OrderService>();
        services.AddSingleton<IUserRepository, InMemoryUserRepository>();
        services.AddSingleton<IOrderRepository, InMemoryOrderRepository>();
        services.AddTerminusFacades();

        var provider = services.BuildServiceProvider();
        var facade = provider.GetRequiredService<IAppFacade>();

        // Act
        var user = await facade.CreateUserAsync(new User { Name = "Jane" });
        var order = await facade.CreateOrderAsync(new Order { UserId = user.Id });

        // Assert
        Assert.NotNull(user);
        Assert.NotNull(order);
        Assert.Equal(user.Id, order.UserId);
    }
}
```

### 3. Mock the Facade Interface

For testing consumers of facades:

```csharp
public class OrderController
{
    private readonly IOrderFacade _facade;

    public OrderController(IOrderFacade facade)
    {
        _facade = facade;
    }

    public async Task<IActionResult> PlaceOrder(Order order)
    {
        await _facade.CreateOrderAsync(order);
        return Ok();
    }
}

public class OrderControllerTests
{
    [Fact]
    public async Task PlaceOrder_CallsFacade_ReturnsOk()
    {
        // Arrange
        var mockFacade = new Mock<IOrderFacade>();
        mockFacade.Setup(f => f.CreateOrderAsync(It.IsAny<Order>()))
            .Returns(Task.CompletedTask);

        var controller = new OrderController(mockFacade.Object);

        // Act
        var result = await controller.PlaceOrder(new Order());

        // Assert
        Assert.IsType<OkResult>(result);
        mockFacade.Verify(f => f.CreateOrderAsync(It.IsAny<Order>()), Times.Once);
    }
}
```

## Unit Testing Services

### Basic Service Test

```csharp
public class UserService
{
    private readonly IUserRepository _repository;

    public UserService(IUserRepository repository)
    {
        _repository = repository;
    }

    [Handler]
    public async Task<User> GetUserAsync(int id)
    {
        return await _repository.FindAsync(id);
    }
}

public class UserServiceTests
{
    [Fact]
    public async Task GetUserAsync_CallsRepository()
    {
        // Arrange
        var mockRepo = new Mock<IUserRepository>();
        var expectedUser = new User { Id = 1, Name = "Test" };
        mockRepo.Setup(r => r.FindAsync(1)).ReturnsAsync(expectedUser);

        var service = new UserService(mockRepo.Object);

        // Act
        var result = await service.GetUserAsync(1);

        // Assert
        Assert.Same(expectedUser, result);
        mockRepo.Verify(r => r.FindAsync(1), Times.Once);
    }
}
```

### Testing Static Methods

```csharp
public static class ValidationHelpers
{
    [Handler]
    public static bool IsValidEmail(string email)
    {
        return !string.IsNullOrEmpty(email) &&
               email.Contains("@");
    }
}

public class ValidationHelpersTests
{
    [Theory]
    [InlineData("test@example.com", true)]
    [InlineData("invalid", false)]
    [InlineData("", false)]
    public void IsValidEmail_ValidatesCorrectly(string email, bool expected)
    {
        var result = ValidationHelpers.IsValidEmail(email);
        Assert.Equal(expected, result);
    }
}
```

## Integration Testing

### In-Memory Database Testing

```csharp
public class OrderIntegrationTests : IDisposable
{
    private readonly ServiceProvider _provider;
    private readonly AppDbContext _db;

    public OrderIntegrationTests()
    {
        var services = new ServiceCollection();

        // Use in-memory database
        services.AddDbContext<AppDbContext>(options =>
            options.UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}"));

        services.AddScoped<OrderService>();
        services.AddTerminusFacades();

        _provider = services.BuildServiceProvider();
        _db = _provider.GetRequiredService<AppDbContext>();
    }

    [Fact]
    public async Task CreateOrder_PersistsToDatabase()
    {
        // Arrange
        using var scope = _provider.CreateScope();
        var facade = scope.ServiceProvider.GetRequiredService<IOrderFacade>();

        var order = new Order
        {
            CustomerId = 1,
            Total = 99.99m
        };

        // Act
        await facade.CreateOrderAsync(order);

        // Assert
        var savedOrder = await _db.Orders.FirstOrDefaultAsync();
        Assert.NotNull(savedOrder);
        Assert.Equal(99.99m, savedOrder.Total);
    }

    public void Dispose()
    {
        _db?.Dispose();
        _provider?.Dispose();
    }
}
```

### Testing Scoped Facades

```csharp
public class ScopedFacadeTests
{
    [Fact]
    public async Task ScopedFacade_SharesDbContext_AcrossMethodCalls()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddDbContext<AppDbContext>(opt =>
            opt.UseInMemoryDatabase("TestDb"));
        services.AddScoped<OrderService>();
        services.AddTerminusFacades();

        var provider = services.BuildServiceProvider();

        // Act & Assert
        await using (var facade = provider.GetRequiredService<IOrderFacade>())
        {
            var order = new Order { Total = 100m };
            await facade.CreateOrderAsync(order);

            // Same DbContext instance used
            var retrieved = await facade.GetOrderAsync(order.Id);
            Assert.NotNull(retrieved);
            Assert.Equal(100m, retrieved.Total);
        }
    }
}
```

## Mocking and Test Doubles

### Using Moq

```csharp
[Fact]
public async Task ProcessOrder_SendsNotification()
{
    // Arrange
    var mockNotificationService = new Mock<INotificationService>();
    var mockOrderRepo = new Mock<IOrderRepository>();

    var order = new Order { Id = 1, CustomerId = 123 };
    mockOrderRepo.Setup(r => r.FindAsync(1)).ReturnsAsync(order);

    var service = new OrderService(mockOrderRepo.Object, mockNotificationService.Object);

    // Act
    await service.ProcessOrderAsync(1);

    // Assert
    mockNotificationService.Verify(
        n => n.SendAsync(It.Is<string>(s => s.Contains("Order processed"))),
        Times.Once);
}
```

### Using NSubstitute

```csharp
[Fact]
public async Task GetUser_CachesResult()
{
    // Arrange
    var cache = Substitute.For<ICache>();
    var repo = Substitute.For<IUserRepository>();

    var user = new User { Id = 1, Name = "Test" };
    repo.GetAsync(1).Returns(user);

    var service = new UserService(repo, cache);

    // Act
    await service.GetUserAsync(1);

    // Assert
    await cache.Received(1).SetAsync(Arg.Any<string>(), Arg.Any<User>());
}
```

## Testing Async Methods

### Testing Task\<T\>

```csharp
[Fact]
public async Task GetDataAsync_ReturnsData()
{
    // Arrange
    var mockClient = new Mock<IHttpClient>();
    mockClient.Setup(c => c.GetAsync<Data>("https://api.example.com/data"))
        .ReturnsAsync(new Data { Value = "test" });

    var service = new DataService(mockClient.Object);

    // Act
    var result = await service.GetDataAsync();

    // Assert
    Assert.NotNull(result);
    Assert.Equal("test", result.Value);
}
```

### Testing IAsyncEnumerable\<T\>

```csharp
[Fact]
public async Task StreamItemsAsync_YieldsItems()
{
    // Arrange
    var items = new List<Item>
    {
        new Item { Id = 1 },
        new Item { Id = 2 },
        new Item { Id = 3 }
    };

    var mockRepo = new Mock<IRepository>();
    mockRepo.Setup(r => r.StreamAsync())
        .Returns(ToAsyncEnumerable(items));

    var service = new DataService(mockRepo.Object);

    // Act
    var results = new List<Item>();
    await foreach (var item in service.StreamItemsAsync())
    {
        results.Add(item);
    }

    // Assert
    Assert.Equal(3, results.Count);
}

private static async IAsyncEnumerable<T> ToAsyncEnumerable<T>(IEnumerable<T> items)
{
    foreach (var item in items)
    {
        await Task.Yield();
        yield return item;
    }
}
```

## Testing CancellationToken

```csharp
[Fact]
public async Task ProcessAsync_RespectsCancellation()
{
    // Arrange
    var cts = new CancellationTokenSource();
    var service = new LongRunningService();

    // Act
    cts.Cancel();

    // Assert
    await Assert.ThrowsAsync<OperationCanceledException>(
        () => service.ProcessAsync(cts.Token));
}
```

## Test Fixtures

### Shared Test Context

```csharp
public class FacadeTestFixture : IDisposable
{
    public ServiceProvider Provider { get; }

    public FacadeTestFixture()
    {
        var services = new ServiceCollection();
        services.AddDbContext<AppDbContext>(opt =>
            opt.UseInMemoryDatabase("TestDb"));
        services.AddScoped<UserService>();
        services.AddScoped<OrderService>();
        services.AddTerminusFacades();

        Provider = services.BuildServiceProvider();
    }

    public void Dispose()
    {
        Provider?.Dispose();
    }
}

public class UserFacadeTests : IClassFixture<FacadeTestFixture>
{
    private readonly FacadeTestFixture _fixture;

    public UserFacadeTests(FacadeTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task CreateUser_Succeeds()
    {
        using var scope = _fixture.Provider.CreateScope();
        var facade = scope.ServiceProvider.GetRequiredService<IUserFacade>();

        var user = await facade.CreateUserAsync(new User { Name = "Test" });
        Assert.NotNull(user);
    }
}
```

## Best Practices

### 1. Test Business Logic, Not Infrastructure

```csharp
// ✅ Good - Tests business logic
[Fact]
public void CalculateDiscount_AppliesCorrectRate()
{
    var service = new PricingService();
    var discount = service.CalculateDiscount(100m, CustomerType.Premium);
    Assert.Equal(10m, discount);
}

// ❌ Bad - Tests Terminus infrastructure
[Fact]
public void Facade_GeneratesCorrectly()
{
    var facade = provider.GetRequiredService<IAppFacade>();
    Assert.NotNull(facade); // This tests Terminus, not your code
}
```

### 2. Use Test Doubles for Dependencies

```csharp
// ✅ Good - Isolated test
[Fact]
public async Task GetUser_CallsRepository()
{
    var mockRepo = new Mock<IUserRepository>();
    var service = new UserService(mockRepo.Object);

    await service.GetUserAsync(1);

    mockRepo.Verify(r => r.FindAsync(1), Times.Once);
}
```

### 3. Test Edge Cases

```csharp
[Theory]
[InlineData(0)]
[InlineData(-1)]
[InlineData(int.MaxValue)]
public async Task GetUser_HandlesEdgeCases(int id)
{
    var service = new UserService(mockRepo.Object);
    var result = await service.GetUserAsync(id);
    Assert.Null(result); // Or throw, depending on design
}
```

### 4. Test Async Properly

```csharp
// ✅ Good - Properly async
[Fact]
public async Task ProcessAsync_CompletesSuccessfully()
{
    await service.ProcessAsync();
    Assert.True(service.IsComplete);
}

// ❌ Bad - Blocking on async
[Fact]
public void ProcessAsync_CompletesSuccessfully()
{
    service.ProcessAsync().Wait(); // Deadlock risk!
}
```

## Next Steps

- Review [Troubleshooting](troubleshooting.md) for common issues
- Explore [Examples](../examples/basic.md) for real-world patterns
- Check [API Reference](../../api/Terminus.html) for complete documentation
