# .NET 8 Migration

```markdown
- [ ] Update language version
- [ ] Update dotnet version
- [ ] Library upgrades with releated .NET 8
- [ ] Docker upgrade
- [ ] Other library upgrades
- [ ] Syntax improvement
  - [ ] Use primary constructors
    - Parameter name start with underscore
    - Use primary c. where dependency injection and record exist
  - [ ] Use collection expressions
  - [ ] Use default lambda parameters
- [ ] Use/Test source link
- [ ] `DO`: Review DO's exception handling structure and see if you can switch
  to `IExceptionHandling`.
- [ ] `DO`: `TimeProvider` to be injected.
- [ ] `Learn`: Learn how to use `Keyed DI services`
- [ ] `Learn`: Learn how to use AOT
- [ ] `DO`: See if we can use AOT in DO.
```

## Primary constructors

Primary Constructor can be used where there is Dependency Injection and Record.

Parameters will start with underscore for class and struct.

```csharp
public class Entity(IEntityContext<Entity> _context, ITransaction _transaction)
{
  ...
  public virtual void Delete()
  {
    _context.Delete(this);
  }
}

public record Stubber(Spec Spec);
```

## Collection expressions

Several collection-like types can be created without requiring external BCL
support. These types are:

- Array types, such as `int[]`.
- `System.Span<T>` and `System.ReadOnlySpan<T>`.
- Types that support collection initializers, such as
  `System.Collections.Generic.List<T>`.

```csharp
int[] row0 = [1, 2, 3];
int[] row1 = [4, 5, 6];
int[] row2 = [7, 8, 9];
int[] single = [..row0, ..row1, ..row2];
foreach (var element in single)
{
    Console.Write($"{element}, ");
}
// output:
// 1, 2, 3, 4, 5, 6, 7, 8, 9,
```

## Default lambda parameters

You can now define default values for parameters on lambda expressions.

```csharp
var IncrementBy = (int source, int increment = 1) => source + increment;

Console.WriteLine(IncrementBy(5)); // 6
Console.WriteLine(IncrementBy(5, 2)); // 7
```

## Source Link

We use the Source Link feature in our open source projects to improve the
development environment for our users.

This can be used in nuget packages that come in .net8 and have the source link
automatically provided.

## Keyed DI services

We use `FromKeyedServicesAttribute` from Keyed DI Services to specify which
keyed service to use.

```csharp
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

...

builder.Services.AddKeyedSingleton<IMemoryCache, BigCache>("big");
builder.Services.AddKeyedSingleton<IMemoryCache, SmallCache>("small");

app.Run();

class BigCacheConsumer([FromKeyedServices("big")] IMemoryCache cache)
{
    public object? GetData() => cache.Get("data");
}

class SmallCacheConsumer(IKeyedServiceProvider keyedServiceProvider)
{
    public object? GetData() => keyedServiceProvider.GetRequiredKeyedService<IMemoryCache>("small");
}
```

## `IExceptionHandler`

`IExceptionHandler` is a new interface that gives the developer a callback for
handling known exceptions in a central location.

`IExceptionHandler` implementations are registered by calling
`IServiceCollection.AddExceptionHandler<T>`. Multiple implementations can be
added, and they're called in the order registered.

You need two things to add an `IExceptionHandler` implementation to the ASP.NET
Core request pipeline:

1. Register the `IExceptionHandler` service with dependency injection
1. Register the `ExceptionHandlerMiddleware` with the request pipeline

```csharp
internal sealed class GlobalExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        // Handle the exception, log errors.

        return true;
    }
}

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
```

> :warning:
>
> To chain your Exception handlers and only want to handle exception with them,
> chain them, but you HAVE TO define a default Exception handler that will run
> (and placed in the last position) to handle any Exception that has been
> handled by the previous handlers.
> **The order matters!**
>
> ```csharp
> builder.Services.AddExceptionHandler<TimeOutExceptionHandler>();
> builder.Services.AddExceptionHandler<DefaultExceptionHandler>();
> ```

You also need to call `UseExceptionHandler` to add the
`ExceptionHandlerMiddleware` to the request pipeline:

```csharp
app.UseExceptionHandler();
```

## Time abstraction

The new `TimeProvider` class add time abstraction functionality, which allows
you to mock time in test scenarios.

Abstraction

```csharp
public abstract class TimeProvider
{
    public static TimeProvider System { get; }
    protected TimeProvider();
    public virtual DateTimeOffset GetUtcNow();
    public DateTimeOffset GetLocalNow();
    public virtual TimeZoneInfo LocalTimeZone { get; }
    public virtual long TimestampFrequency { get; }
    public virtual long GetTimestamp();
    public TimeSpan GetElapsedTime(long startingTimestamp);
    public TimeSpan GetElapsedTime(long startingTimestamp, long endingTimestamp);
    public virtual ITimer CreateTimer(TimerCallback callback, object? state,TimeSpan dueTime, TimeSpan period);
}

public class MyService
{
    public readonly TimeProvider _timeProvider;

    public MyService(TimeProvider timeProvider){
        _timeProvider = timeProvider;
    }

    public boolean IsMonday() {
        return _timeProvider.GetLocalNow().DayOfWeek == DayOfWeek.Monday;
    }
}

// Dependency injection:
var builder = WebApplication.CreateBuilder();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<MyService>();
```

Mocking

```csharp
[Test]
public void MyTest()
{
    var mock = new Mock<TimeProvider>();
 mock.Setup(x => x.GetLocalNow()).Returns(new DateTimeOffset(2025, 12, 31, 23, 59, 59, TimeSpan.Zero));
    var mockedTimeProvider = mock.Object;

    var myService = new MyService(mockedTimeProvider);
    var result = myService.IsMonday(mockedTimeProvider);
    Assert.IsTrue(result, "Should be Monday");
}
```