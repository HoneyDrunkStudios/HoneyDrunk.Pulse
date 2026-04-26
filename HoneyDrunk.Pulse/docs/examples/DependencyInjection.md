# 🔌 DependencyInjection - Service Registration

[← Back to File Guide](FILE_GUIDE.md)

---

## Table of Contents

- [Overview](#overview)
- [ServiceCollectionExtensions.cs](#servicecollectionextensionscs)
- [ApplicationBuilderExtensions.cs](#applicationbuilderextensionscs)
- [ICorrelationIdAccessor.cs](#icorrelationidaccessorcs)
- [CorrelationIdAccessor.cs](#correlationidaccessorcs)

---

## Overview

Extension methods for registering REST services and middleware.

**Location:** `HoneyDrunk.Web.Rest.AspNetCore/Extensions/` and `HoneyDrunk.Web.Rest.AspNetCore/Context/`

**Components:**
- **ServiceCollectionExtensions** - `AddRest()` for DI registration
- **ApplicationBuilderExtensions** - `UseRest()` for middleware
- **ICorrelationIdAccessor** - Contract for correlation ID access
- **CorrelationIdAccessor** - AsyncLocal implementation

[↑ Back to top](#table-of-contents)

---

## ServiceCollectionExtensions.cs

**DI registration extension methods**

```csharp
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddRest(
        this IServiceCollection services,
        Action<RestOptions>? configure = null);
}
```

### Purpose

Registers all REST services with the dependency injection container. This is the main entry point for configuring the library.

### What Gets Registered

| Service | Lifetime | Description |
|---------|----------|-------------|
| `ICorrelationIdAccessor` | Singleton | Correlation ID accessor |
| `IOptions<RestOptions>` | Options pattern | Configuration options |
| `IAuthorizationMiddlewareResultHandler` | Singleton | Auth failure shaping (if enabled) |
| `ModelStateValidationFilter` | (via MvcOptions) | Validation filter |
| JSON serializer options | (via JsonOptions) | JSON defaults |

### Usage

```csharp
var builder = WebApplication.CreateBuilder(args);

// Basic registration with defaults
builder.Services.AddRest();

// Or with configuration
builder.Services.AddRest(options =>
{
    options.EnableExceptionMapping = true;
    options.IncludeExceptionDetails = builder.Environment.IsDevelopment();
    options.EnableModelStateValidationFilter = true;
    options.EnableAuthFailureShaping = true;
});

builder.Services.AddControllers();

var app = builder.Build();
```

### Behavior

1. **Registers ICorrelationIdAccessor** - `TryAddSingleton` (won't override existing)
2. **Configures options** - Applies provided configuration or defaults
3. **Configures JSON** - If `ConfigureJsonDefaults = true`
4. **Configures MVC** - If `EnableModelStateValidationFilter = true`
5. **Registers auth handler** - If `EnableAuthFailureShaping = true`
6. **Suppresses default validation** - If using custom filter

> **Note:** Core services use `TryAdd*` methods, allowing you to register custom implementations before calling `AddRest()`.

### Registration Order

```csharp
var builder = WebApplication.CreateBuilder(args);

// 1. Add REST services FIRST
builder.Services.AddRest(options => { /* ... */ });

// 2. Then add MVC/controllers
builder.Services.AddControllers();

// 3. Or add other services that depend on correlation
builder.Services.AddScoped<IOrderService, OrderService>();
```

[↑ Back to top](#table-of-contents)

---

## ApplicationBuilderExtensions.cs

**Middleware registration extension methods**

```csharp
public static class ApplicationBuilderExtensions
{
    public static IApplicationBuilder UseRest(this IApplicationBuilder app);
}
```

### Purpose

Adds REST middleware to the request pipeline based on configured options.

### Middleware Order

```csharp
public static IApplicationBuilder UseRest(this IApplicationBuilder app)
{
    // 1. Correlation ID (sets before processing)
    app.UseMiddleware<CorrelationMiddleware>();

    // 2. Exception mapping (catches all, has correlation)
    if (options.EnableExceptionMapping)
    {
        app.UseMiddleware<ExceptionMappingMiddleware>();
    }

    // 3. Logging scope (uses correlation)
    if (options.EnableRequestLoggingScope)
    {
        app.UseMiddleware<RequestLoggingScopeMiddleware>();
    }

    return app;
}
```

### Usage

```csharp
var app = builder.Build();

// Add REST middleware early in the pipeline
app.UseRest();

// Then add auth middleware
app.UseAuthentication();
app.UseAuthorization();

// Then map endpoints
app.MapControllers();

app.Run();
```

> **Note:** `UseRest()` should run early to ensure correlation IDs are set before other middleware executes. In minimal hosting, explicit `UseRouting()` is typically not required.

### Pipeline Position

```
Request
   │
   ▼
┌─────────────────────────────────────┐
│ app.UseRest()                       │  ◄── ADD EARLY
│   - CorrelationMiddleware           │
│   - ExceptionMappingMiddleware      │
│   - RequestLoggingScopeMiddleware   │
└─────────────────────────────────────┘
   │
   ▼
┌─────────────────────────────────────┐
│ app.UseAuthentication()             │
│ app.UseAuthorization()              │
└─────────────────────────────────────┘
   │
   ▼
┌─────────────────────────────────────┐
│ app.UseRouting()                    │
│ app.MapControllers()                │
└─────────────────────────────────────┘
   │
   ▼
Response
```

> **Note:** 401/403 shaping is handled by `IAuthorizationMiddlewareResultHandler` during authorization, not by `UseRest()` middleware.

[↑ Back to top](#table-of-contents)

---

## ICorrelationIdAccessor.cs

**Correlation ID access contract**

```csharp
public interface ICorrelationIdAccessor
{
    string? CorrelationId { get; }
    void SetCorrelationId(string correlationId);
}
```

### Purpose

Provides access to the current request's correlation ID without requiring `HttpContext`. Can be injected into services anywhere in the application.

### Properties and Methods

| Member | Description |
|--------|-------------|
| `CorrelationId` | Gets the current correlation ID (null if not set) |
| `SetCorrelationId(string)` | Sets the correlation ID for the current async context |

### Usage

```csharp
public class OrderService
{
    private readonly ICorrelationIdAccessor _correlationIdAccessor;
    private readonly ILogger<OrderService> _logger;
    
    public OrderService(
        ICorrelationIdAccessor correlationIdAccessor,
        ILogger<OrderService> logger)
    {
        _correlationIdAccessor = correlationIdAccessor;
        _logger = logger;
    }
    
    public async Task ProcessOrderAsync(Order order)
    {
        var correlationId = _correlationIdAccessor.CorrelationId;
        
        _logger.LogInformation(
            "Processing order {OrderId} with correlation {CorrelationId}",
            order.Id,
            correlationId);
        
        // Pass to downstream services
        await _paymentService.ProcessAsync(order, correlationId);
    }
}
```

### Why Not HttpContext?

| Approach | Pros | Cons |
|----------|------|------|
| `ICorrelationIdAccessor` | Works in background services | Need to set manually outside HTTP |
| | No HttpContext dependency | |
| | Testable | |
| `IHttpContextAccessor` | Direct access | Requires HttpContext |
| | | Doesn't work in background tasks |
| | | Harder to test |

> **Warning:** In non-HTTP contexts (background services, message handlers), callers are responsible for setting the correlation ID explicitly via `SetCorrelationId()`. Without this, `CorrelationId` will be null.

[↑ Back to top](#table-of-contents)

---

## CorrelationIdAccessor.cs

**AsyncLocal implementation**

```csharp
public sealed class CorrelationIdAccessor : ICorrelationIdAccessor
{
    private static readonly AsyncLocal<string?> CurrentCorrelationId = new();

    public string? CorrelationId => CurrentCorrelationId.Value;

    public void SetCorrelationId(string correlationId)
    {
        CurrentCorrelationId.Value = correlationId;
    }
}
```

### Purpose

Default implementation using `AsyncLocal<T>` to flow correlation ID across async boundaries.

### How It Works

```
Request Thread
    │
    ├─ CorrelationMiddleware sets ID
    │   CurrentCorrelationId.Value = "abc-123"
    │
    ├─ await SomeAsync()
    │   │
    │   └─ Different thread, but same AsyncLocal value
    │       CurrentCorrelationId.Value is still "abc-123"
    │
    ├─ await AnotherAsync()
    │   │
    │   └─ Value flows through all awaits
    │       CurrentCorrelationId.Value is still "abc-123"
    │
    └─ Request completes
```

### Thread Safety

`AsyncLocal<T>` is designed for exactly this scenario:
- Thread-safe
- Flows across `await` boundaries
- Isolated between concurrent requests
- No locking required

### Custom Implementation

You can replace the default implementation:

```csharp
public class HttpContextCorrelationIdAccessor : ICorrelationIdAccessor
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    
    public HttpContextCorrelationIdAccessor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }
    
    public string? CorrelationId =>
        _httpContextAccessor.HttpContext?.Items[HeaderNames.CorrelationId] as string;
    
    public void SetCorrelationId(string correlationId)
    {
        if (_httpContextAccessor.HttpContext is not null)
        {
            _httpContextAccessor.HttpContext.Items[HeaderNames.CorrelationId] = correlationId;
        }
    }
}

// Register before AddRest
services.AddSingleton<ICorrelationIdAccessor, HttpContextCorrelationIdAccessor>();
services.AddRest(); // Won't override due to TryAddSingleton
```

---

[← Back to File Guide](FILE_GUIDE.md) | [↑ Back to top](#table-of-contents)
