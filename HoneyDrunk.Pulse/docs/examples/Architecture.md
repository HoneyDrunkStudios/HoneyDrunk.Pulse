# 🏛️ Architecture - Layer Responsibilities and Design

[← Back to File Guide](FILE_GUIDE.md)

---

## Table of Contents

- [Overview](#overview)
- [Layer Architecture](#layer-architecture)
- [Package Dependencies](#package-dependencies)
- [Middleware Pipeline](#middleware-pipeline)
- [Response Flow](#response-flow)
- [Design Decisions](#design-decisions)
- [Integration Patterns](#integration-patterns)

---

## Overview

HoneyDrunk.Web.Rest follows a **layered architecture** that separates pure contracts from runtime implementations. This enables:

- **Contract-first development** - Define API shapes before implementation
- **Testability** - Mock contracts without runtime dependencies
- **Flexibility** - Swap implementations without changing contracts
- **Minimal dependencies** - Use only what you need

```
┌─────────────────────────────────────────────────────────────┐
│                    Application Layer                         │
│              (Controllers, Services, Handlers)               │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│              HoneyDrunk.Web.Rest.AspNetCore                  │
│     (Middleware, Filters, Extensions, Serialization)        │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│             HoneyDrunk.Web.Rest.Abstractions                 │
│        (Contracts, Results, Errors, Paging, Constants)       │
└─────────────────────────────────────────────────────────────┘
```

[↑ Back to top](#table-of-contents)

---

## Layer Architecture

### HoneyDrunk.Web.Rest.Abstractions

**Purpose:** Pure contracts with zero runtime dependencies

**Contains:**
- Response envelope contracts (`ApiResult`, `ApiResult<T>`)
- Error contracts (`ApiError`, `ApiErrorResponse`, `ValidationError`)
- Pagination contracts (`PageRequest`, `PageResult<T>`)
- Constants (`HeaderNames`, `MediaTypes`, `ApiErrorCode`)
- Telemetry tags (`RestTelemetryTags`)

**Dependencies:** None (pure .NET)

**Use when:**
- Defining shared contracts between services
- Building client libraries that consume APIs
- Creating DTOs that don't need ASP.NET Core

```csharp
// Can be used anywhere without ASP.NET Core
public class OrderResponse
{
    public ApiResult<Order> ToApiResult()
    {
        return ApiResult<Order>.Success(this.Order);
    }
}
```

### HoneyDrunk.Web.Rest.AspNetCore

**Purpose:** ASP.NET Core integration and runtime components

**Contains:**
- Middleware (Correlation, Exception Mapping, Logging)
- MVC Filters (Model State Validation)
- DI Extensions (`AddRest`, `UseRest`)
- JSON Serialization Defaults
- Context Accessors (`ICorrelationIdAccessor`)
- Auth Result Handler (`RestAuthorizationResultHandler`)
- Transport Extensions (`TransportResultExtensions`)

**Dependencies:** 
- `HoneyDrunk.Web.Rest.Abstractions`
- `Microsoft.AspNetCore.App` (framework reference)
- `HoneyDrunk.Kernel.Abstractions` (optional, for Grid context)
- `HoneyDrunk.Auth.AspNetCore` (optional, for identity context)
- `HoneyDrunk.Transport` (optional, uses `HoneyDrunk.Transport.Abstractions` namespace types for envelope mapping only)

**Use when:**
- Building ASP.NET Core web APIs
- Need middleware and filters
- Want automatic exception mapping

```csharp
// Requires ASP.NET Core
builder.Services.AddRest();
app.UseRest();
```

[↑ Back to top](#table-of-contents)

---

## Package Dependencies

```
┌─────────────────────────────────────────┐
│     HoneyDrunk.Web.Rest.AspNetCore      │
│                                         │
│  ┌─────────────────────────────────┐   │
│  │  Microsoft.AspNetCore.App       │   │
│  │  (Framework Reference)          │   │
│  └─────────────────────────────────┘   │
│                                         │
│  ┌─────────────────────────────────┐   │
│  │  HoneyDrunk.Web.Rest.           │   │
│  │  Abstractions                   │   │
│  └─────────────────────────────────┘   │
│                                         │
│  ┌─────────────────────────────────┐   │
│  │  HoneyDrunk.Kernel.Abstractions │   │
│  │  (Optional - Grid context)      │   │
│  └─────────────────────────────────┘   │
│                                         │
│  ┌─────────────────────────────────┐   │
│  │  HoneyDrunk.Auth.AspNetCore     │   │
│  │  (Optional - Identity context)  │   │
│  └─────────────────────────────────┘   │
│                                         │
│  ┌─────────────────────────────────┐   │
│  │  HoneyDrunk.Transport           │   │
│  │  (Optional - Envelope mapping)  │   │
│  └─────────────────────────────────┘   │
└─────────────────────────────────────────┘
           │
           ▼
┌─────────────────────────────────────────┐
│   HoneyDrunk.Web.Rest.Abstractions      │
│                                         │
│  ┌─────────────────────────────────┐   │
│  │  System.Text.Json               │   │
│  │  (for JSON attributes)          │   │
│  └─────────────────────────────────┘   │
│                                         │
│  No other dependencies!                 │
└─────────────────────────────────────────┘
```

### Dependency Rules

1. **Abstractions has no NuGet dependencies** - Uses BCL only (`System.Text.Json` attributes are part of the platform)
2. **AspNetCore depends on Abstractions** - Uses contracts for implementation
3. **Applications depend on AspNetCore** - For full functionality
4. **Client libraries depend on Abstractions only** - For contract definitions
5. **Kernel/Auth/Transport are optional** - Middleware gracefully degrades when not registered

[↑ Back to top](#table-of-contents)

---

## Middleware Pipeline

The middleware is registered in a specific order to ensure proper functionality:

```
Request
   │
   ▼
┌─────────────────────────────────────┐
│   CorrelationMiddleware             │  ◄── First (sets correlation ID)
│   (Extracts/generates correlation   │
│    ID, sets in accessor)            │
│   (Uses Kernel IOperationContext    │
│    if available)                    │
└─────────────────────────────────────┘
   │
   ▼
┌─────────────────────────────────────┐
│   ExceptionMappingMiddleware        │  ◄── Second (catches exceptions)
│   (Catches exceptions, maps to      │
│    ApiErrorResponse with            │
│    correlation ID)                  │
└─────────────────────────────────────┘
   │
   ▼
┌─────────────────────────────────────┐
│   RequestLoggingScopeMiddleware     │  ◄── Third (optional)
│   (Adds correlation to log scope)   │
│   (Enriches with Kernel context     │
│    if available)                    │
└─────────────────────────────────────┘
   │
   ▼
┌─────────────────────────────────────┐
│   Routing / Controllers / Handlers  │  ◄── Application code
└─────────────────────────────────────┘
   │
   ▼
Response
```

### Why This Order?

1. **CorrelationMiddleware first** - Sets correlation before any processing
2. **ExceptionMappingMiddleware second** - Can include correlation in error responses
3. **RequestLoggingScopeMiddleware third** - Uses correlation set by first middleware
4. **Application code last** - Has access to all middleware-set values

> **Note:** Auth failure shaping (401/403) occurs via `IAuthorizationMiddlewareResultHandler` during authorization, not as a dedicated middleware in this pipeline.

[↑ Back to top](#table-of-contents)

---

## Response Flow

### Success Response Flow

```
Controller                    Framework                     Client
    │                            │                            │
    │  return Ok(ApiResult       │                            │
    │    <Order>.Success(order)) │                            │
    │───────────────────────────►│                            │
    │                            │                            │
    │                            │  CorrelationMiddleware     │
    │                            │  sets X-Correlation-Id     │
    │                            │  header, framework         │
    │                            │  serializes response       │
    │                            │───────────────────────────►│
    │                            │                            │
    │                            │  HTTP 200                  │
    │                            │  {                         │
    │                            │    "status": "success",    │
    │                            │    "data": { ... },        │
    │                            │    "correlationId": "..."  │
    │                            │  }                         │
```

### Error Response Flow (Exception)

```
Controller                  Middleware                      Client
    │                            │                            │
    │  throw KeyNotFound         │                            │
    │    Exception()             │                            │
    │───────────────────────────►│                            │
    │                            │                            │
    │              ExceptionMappingMiddleware                 │
    │                            │                            │
    │              Map to 404 + ApiErrorResponse              │
    │                            │───────────────────────────►│
    │                            │                            │
    │                            │  HTTP 404                  │
    │                            │  {                         │
    │                            │    "correlationId": "...", │
    │                            │    "error": {              │
    │                            │      "code": "NOT_FOUND",  │
    │                            │      "message": "..."      │
    │                            │    }                       │
    │                            │  }                         │
```

### Validation Error Flow

```
Controller                    Filter                        Client
    │                            │                            │
    │  [FromBody] with           │                            │
    │  invalid model             │                            │
    │───────────────────────────►│                            │
    │                            │                            │
    │         ModelStateValidationFilter                      │
    │                            │                            │
    │         ModelState.IsValid = false                      │
    │         Return 400 + ApiErrorResponse                   │
    │                            │───────────────────────────►│
    │                            │                            │
    │                            │  HTTP 400                  │
    │                            │  {                         │
    │                            │    "correlationId": "...", │
    │                            │    "error": {              │
    │                            │      "code":               │
    │                            │        "VALIDATION_FAILED" │
    │                            │    },                      │
    │                            │    "validationErrors": [   │
    │                            │      { "field": "email",   │
    │                            │        "message": "..." }  │
    │                            │    ]                       │
    │                            │  }                         │
```

[↑ Back to top](#table-of-contents)

---

## Design Decisions

### Why Records for Contracts?

```csharp
public record ApiResult { ... }
public record ApiError { ... }
```

**Benefits:**
- Immutability by default
- Value-based equality
- Built-in `ToString()` for debugging
- `with` expressions for modifications
- Concise syntax

### Why Static Factory Methods?

```csharp
public static ApiResult Success(string? correlationId = null);
public static ApiResult NotFound(string message = "...");
```

**Benefits:**
- Semantic naming (`NotFound` vs `new ApiResult { Status = NotFound }`)
- Encapsulate default values
- Easy to discover via IntelliSense
- Consistent initialization patterns

### Why Separate Error Types?

```csharp
ApiError         // Single error
ValidationError  // Field-level validation error
ApiErrorResponse // Complete error response envelope
```

**Benefits:**
- Single Responsibility Principle
- Validation errors have different structure
- Reusable components
- Clear intent

### Why AsyncLocal for Correlation?

```csharp
private static readonly AsyncLocal<string?> CurrentCorrelationId = new();
```

**Benefits:**
- Flows across async boundaries
- Thread-safe
- No HttpContext dependency in services
- Works in background tasks

### Why IAuthorizationMiddlewareResultHandler for Auth?

```csharp
internal sealed class RestAuthorizationResultHandler : IAuthorizationMiddlewareResultHandler
```

**Benefits:**
- Scheme-agnostic (works with JWT, cookies, etc.)
- Intercepts at authorization layer, not authentication
- Single place to shape all 401/403 responses
- Registered automatically by `AddRest()`

[↑ Back to top](#table-of-contents)

---

## Integration Patterns

### Pattern 1: Full Integration

```csharp
// Use everything
builder.Services.AddRest(options =>
{
    options.EnableExceptionMapping = true;
    options.EnableRequestLoggingScope = true;
    options.EnableModelStateValidationFilter = true;
    options.EnableAuthFailureShaping = true;
});

app.UseRest();
```

### Pattern 2: Contracts Only

```csharp
// Just use the response types (no middleware)
// In a shared library:
public class OrderService
{
    public ApiResult<Order> GetOrder(Guid id)
    {
        var order = _repository.Find(id);
        return order is null
            ? ApiResult<Order>.NotFound()
            : ApiResult<Order>.Success(order);
    }
}
```

### Pattern 3: Custom Middleware

```csharp
// Use contracts, but custom middleware
builder.Services.AddSingleton<ICorrelationIdAccessor, CorrelationIdAccessor>();

app.Use(async (context, next) =>
{
    // Custom correlation logic
    var accessor = context.RequestServices
        .GetRequiredService<ICorrelationIdAccessor>();
    accessor.SetCorrelationId(Guid.NewGuid().ToString());
    await next();
});
```

### Pattern 4: Minimal API Only

```csharp
// Skip MVC filter, use minimal APIs
builder.Services.AddRest(options =>
{
    options.EnableModelStateValidationFilter = false; // Not needed
});

app.MapGet("/orders/{id}", (Guid id) =>
{
    return Results.Ok(ApiResult<Order>.Success(order));
})
.WithRest<Order>();
```

### Pattern 5: With Kernel Integration

```csharp
// When HoneyDrunk.Kernel is registered, correlation prefers IOperationContext
// Register Kernel services so IOperationContextAccessor is available
builder.Services.AddSingleton<IOperationContextAccessor, OperationContextAccessor>();
builder.Services.AddRest();

// Middleware will:
// 1. Use IOperationContext.CorrelationId if available
// 2. Enrich logging scope with Grid context values
