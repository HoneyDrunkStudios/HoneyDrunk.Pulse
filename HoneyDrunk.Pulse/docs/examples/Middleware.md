# 🔄 Middleware - Request Pipeline Components

[← Back to File Guide](FILE_GUIDE.md)

---

## Table of Contents

- [Overview](#overview)
- [CorrelationMiddleware.cs](#correlationmiddlewarecs)
- [ExceptionMappingMiddleware.cs](#exceptionmappingmiddlewarecs)
- [RequestLoggingScopeMiddleware.cs](#requestloggingscopemiddlewarecs)
- [ExceptionMappingResult.cs](#exceptionmappingresultcs)
- [Pipeline Order](#pipeline-order)

---

## Overview

Middleware components that process requests and responses in the ASP.NET Core pipeline.

**Location:** `HoneyDrunk.Web.Rest.AspNetCore/Middleware/`

**Components:**
- **CorrelationMiddleware** - Extracts/generates correlation IDs (uses Kernel if available)
- **ExceptionMappingMiddleware** - Maps exceptions to error responses
- **RequestLoggingScopeMiddleware** - Enriches logging scope (uses Kernel if available)

**Pipeline Registration:**
```csharp
app.UseRest();  // Adds all middleware in correct order
```

[↑ Back to top](#table-of-contents)

---

## CorrelationMiddleware.cs

**Correlation ID propagation middleware**

```csharp
public sealed class CorrelationMiddleware
{
    public CorrelationMiddleware(
        RequestDelegate next,
        IOptions<RestOptions> options);
    
    public async Task InvokeAsync(
        HttpContext context,
        ICorrelationIdAccessor correlationIdAccessor);
}
```

### Purpose

Extracts correlation ID from incoming request headers, Kernel operation context, or generates a new one. Stores the ID in `ICorrelationIdAccessor` for use throughout the request lifecycle and optionally adds it to response headers.

### Behavior

1. **Check Kernel context** - Uses `IOperationContext.CorrelationId` if `IOperationContextAccessor` is registered
2. **Check incoming header** - Looks for `X-Correlation-Id` (configurable)
3. **Generate if missing** - Uses `Activity.Current?.Id` if available, otherwise creates a new GUID
4. **Store in accessor** - Makes ID available via `ICorrelationIdAccessor`
5. **Store in HttpContext.Items** - Uses `HeaderNames.CorrelationId` constant
6. **Add to response header** - Returns correlation ID to caller

### Correlation ID Priority (highest to lowest)

1. **Kernel `IOperationContext.CorrelationId`** - If `IOperationContextAccessor` is registered and has a current context
2. **Incoming `X-Correlation-Id` header** - From the request
3. **`Activity.Current?.Id`** - If available (from distributed tracing)
4. **Generated GUID** - If `GenerateCorrelationIdIfMissing = true`

### Configuration

```csharp
services.AddRest(options =>
{
    options.CorrelationIdHeaderName = "X-Correlation-Id";     // Header name
    options.GenerateCorrelationIdIfMissing = true;            // Auto-generate
    options.ReturnCorrelationIdInResponseHeader = true;       // Echo back
});
```

### Usage Example

```csharp
// Middleware automatically handles correlation
// Access via ICorrelationIdAccessor:

public class OrderService
{
    private readonly ICorrelationIdAccessor _correlationIdAccessor;
    
    public OrderService(ICorrelationIdAccessor correlationIdAccessor)
    {
        _correlationIdAccessor = correlationIdAccessor;
    }
    
    public void ProcessOrder()
    {
        var correlationId = _correlationIdAccessor.CorrelationId;
        _logger.LogInformation(
            "Processing with correlation {CorrelationId}",
            correlationId);
    }
}
```

### Request/Response Flow

```
Request:
  Headers: { "X-Correlation-Id": "client-abc-123" }
    │
    ▼
CorrelationMiddleware:
  - Checks IOperationContextAccessor (if registered)
  - Falls back to extracting "client-abc-123" from header
  - Sets ICorrelationIdAccessor.CorrelationId = "client-abc-123"
  - Sets HttpContext.Items[HeaderNames.CorrelationId] = "client-abc-123"
  - Registers response callback to add header
    │
    ▼
Application Code:
  - ICorrelationIdAccessor.CorrelationId returns "client-abc-123"
    │
    ▼
Response:
  Headers: { "X-Correlation-Id": "client-abc-123" }
```

[↑ Back to top](#table-of-contents)

---

## ExceptionMappingMiddleware.cs

**Exception to API error response mapping**

```csharp
public sealed class ExceptionMappingMiddleware
{
    public ExceptionMappingMiddleware(
        RequestDelegate next,
        IOptions<RestOptions> options,
        IHostEnvironment environment,
        ILogger<ExceptionMappingMiddleware> logger);
    
    public async Task InvokeAsync(
        HttpContext context,
        ICorrelationIdAccessor correlationIdAccessor);
}
```

### Purpose

Catches unhandled exceptions and converts them to standardized `ApiErrorResponse` JSON responses with appropriate HTTP status codes.

### Behavior

1. **Wrap pipeline** - Runs after correlation middleware
2. **Catch exceptions** - Intercepts any unhandled exception
3. **Map to status code** - Uses `ExceptionToApiErrorMapper` for mapping
4. **Build response** - Creates `ApiErrorResponse` with correlation ID
5. **Log error** - Logs exception with correlation context
6. **Write response** - Returns JSON error response

### Default Mappings

| Exception Type | HTTP Status | Error Code |
|----------------|-------------|------------|
| `ArgumentNullException` | 400 Bad Request | `BAD_REQUEST` |
| `ArgumentException` | 400 Bad Request | `BAD_REQUEST` |
| `InvalidOperationException` | 409 Conflict | `CONFLICT` |
| `KeyNotFoundException` | 404 Not Found | `NOT_FOUND` |
| `UnauthorizedAccessException` | 403 Forbidden | `FORBIDDEN` |
| `NotImplementedException` | 501 Not Implemented | `NOT_IMPLEMENTED` |
| `OperationCanceledException` | 499 Client Closed | `GENERAL_ERROR` |
| All others | 500 Internal Server Error | `INTERNAL_ERROR` |

> **Note:** These mappings reflect the defaults in `ExceptionToApiErrorMapper`. Custom exception handling can be added by catching exceptions before they reach this middleware.

### Configuration

```csharp
services.AddRest(options =>
{
    options.EnableExceptionMapping = true;       // Enable middleware
    options.IncludeExceptionDetails = false;     // Stack traces (dev only)
    options.IncludeTraceId = true;               // OpenTelemetry trace ID
});
```

### Usage Example

```csharp
// Just throw exceptions - middleware handles the rest

[HttpGet("{id}")]
public IActionResult GetOrder(Guid id)
{
    var order = _repository.Find(id);
    
    if (order is null)
    {
        // Middleware catches and returns 404
        throw new KeyNotFoundException($"Order {id} not found");
    }
    
    return Ok(ApiResult<Order>.Success(order));
}
```

### Response Example

```json
{
  "correlationId": "abc-123",
  "traceId": "00-abc123def456...",
  "timestamp": "2024-01-01T00:00:00Z",
  "error": {
    "code": "NOT_FOUND",
    "message": "The requested resource was not found."
  }
}
```

### Development vs Production

```csharp
// Development (IncludeExceptionDetails = true)
{
  "error": {
    "code": "INTERNAL_ERROR",
    "message": "An internal server error occurred.",
    "details": "System.NullReferenceException: Object reference not set...\n   at OrderService.Process()..."
  }
}

// Production (IncludeExceptionDetails = false)
{
  "error": {
    "code": "INTERNAL_ERROR",
    "message": "An internal server error occurred."
  }
}
```

[↑ Back to top](#table-of-contents)

---

## RequestLoggingScopeMiddleware.cs

**Logging scope enrichment middleware**

```csharp
public sealed class RequestLoggingScopeMiddleware
{
    public RequestLoggingScopeMiddleware(
        RequestDelegate next,
        ILogger<RequestLoggingScopeMiddleware> logger,
        IOptions<RestOptions> options);
    
    public async Task InvokeAsync(
        HttpContext context,
        ICorrelationIdAccessor correlationIdAccessor);
}
```

### Purpose

Enriches the logging scope with correlation ID, request metadata, and Kernel context values. All log entries within the request will include these properties.

### Behavior

1. **Create logging scope** - Adds properties to scope
2. **Include correlation ID** - From `ICorrelationIdAccessor`
3. **Include request metadata** - HTTP method, path, request ID, trace ID
4. **Include Kernel context** - If `IOperationContextAccessor` is registered:
   - OperationId, OperationName, CausationId
   - TenantId, ProjectId
   - NodeId, StudioId, Environment (from IGridContext)
5. **Execute pipeline** - All logs now include scope data
6. **Dispose scope** - Automatically on request completion

### Scope Properties

| Property | Source | Description |
|----------|--------|-------------|
| `CorrelationId` | `ICorrelationIdAccessor` | Request correlation ID |
| `HttpMethod` | `HttpContext.Request` | HTTP method |
| `HttpPath` | `HttpContext.Request` | Request path |
| `RequestId` | `HttpContext.TraceIdentifier` | ASP.NET Core request ID |
| `TraceId` | `Activity.Current?.Id` | OpenTelemetry trace ID |
| `OperationId` | `IOperationContext` | Kernel operation ID (if available) |
| `OperationName` | `IOperationContext` | Kernel operation name (if available) |
| `TenantId` | `IOperationContext` | Tenant ID (if available) |
| `NodeId` | `IGridContext` | Grid node ID (if available) |
| `StudioId` | `IGridContext` | Studio ID (if available) |
| `Environment` | `IGridContext` | Environment name (if available) |

> **Note:** `RequestId` is ASP.NET Core's internal request identifier (`HttpContext.TraceIdentifier`), not a client-supplied header. It differs from `CorrelationId`, which is propagated across service boundaries.

### Configuration

```csharp
services.AddRest(options =>
{
    options.EnableRequestLoggingScope = true;  // Enable middleware
    options.IncludeTraceId = true;             // Include trace ID in scope
});
```

### Usage Example

```csharp
// Logs automatically include correlation ID and Kernel context

public class OrderService
{
    private readonly ILogger<OrderService> _logger;
    
    public void ProcessOrder(Guid orderId)
    {
        // Log automatically includes CorrelationId and Kernel context from scope
        _logger.LogInformation("Processing order {OrderId}", orderId);
        
        // Output: "Processing order abc-123" with properties:
        // - CorrelationId: "corr-456"
        // - HttpMethod: "POST"
        // - HttpPath: "/api/orders"
        // - TenantId: "tenant-789" (if Kernel registered)
        // - NodeId: "order-service" (if Kernel registered)
        // - OrderId: "abc-123"
    }
}
```

### Structured Log Output

Kernel-derived properties (`TenantId`, `NodeId`, etc.) appear only when `HoneyDrunk.Kernel` is registered and `IOperationContextAccessor` provides a current context.

```json
{
  "Timestamp": "2024-01-01T00:00:00Z",
  "Level": "Information",
  "Message": "Processing order abc-123",
  "Properties": {
    "OrderId": "abc-123",
    "CorrelationId": "corr-456",
    "HttpMethod": "POST",
    "HttpPath": "/api/orders",
    "TenantId": "tenant-789",
    "NodeId": "order-service"
  }
}
```

[↑ Back to top](#table-of-contents)

---

## ExceptionMappingResult.cs

**Exception mapping result type**

```csharp
internal readonly record struct ExceptionMappingResult(
    HttpStatusCode StatusCode,
    string ErrorCode,
    string Message);
```

### Purpose

Internal type that holds the result of mapping an exception. Used by `ExceptionToApiErrorMapper` and `ExceptionMappingMiddleware`.

> **Note:** This type is `internal` and not part of the public API surface. Do not depend on it in consuming code.

### Properties

| Property | Type | Description |
|----------|------|-------------|
| `StatusCode` | `HttpStatusCode` | HTTP status code to return |
| `ErrorCode` | `string` | Machine-readable error code |
| `Message` | `string` | Human-readable error message |

### Usage

```csharp
// Internal usage in ExceptionToApiErrorMapper
public static ExceptionMappingResult Map(Exception exception)
{
    return exception switch
    {
        KeyNotFoundException => new ExceptionMappingResult(
            HttpStatusCode.NotFound,
            ApiErrorCode.NotFound,
            "The requested resource was not found."),
        
        // ... other mappings
    };
}
```

[↑ Back to top](#table-of-contents)

---

## Pipeline Order

The middleware must be registered in a specific order:

```csharp
// ApplicationBuilderExtensions.cs
public static IApplicationBuilder UseRest(this IApplicationBuilder app)
{
    // 1. Correlation FIRST (sets correlation ID)
    app.UseMiddleware<CorrelationMiddleware>();

    // 2. Exception mapping SECOND (can use correlation)
    if (options.EnableExceptionMapping)
    {
        app.UseMiddleware<ExceptionMappingMiddleware>();
    }

    // 3. Logging scope THIRD (uses correlation)
    if (options.EnableRequestLoggingScope)
    {
        app.UseMiddleware<RequestLoggingScopeMiddleware>();
    }

    return app;
}
```

### Why This Order?

```
Request Flow:
┌─────────────────────────────────────┐
│ 1. CorrelationMiddleware            │  ← Sets correlation ID first
│   ┌─────────────────────────────┐   │
│   │ 2. ExceptionMappingMiddleware│  │  ← Catches exceptions with correlation
│   │   ┌─────────────────────┐   │   │
│   │   │ 3. LoggingScope     │   │   │  ← Uses correlation for logs
│   │   │   ┌─────────────┐   │   │   │
│   │   │   │ Application │   │   │   │  ← Your controllers/handlers
│   │   │   └─────────────┘   │   │   │
│   │   └─────────────────────┘   │   │
│   └─────────────────────────────┘   │
└─────────────────────────────────────┘
```

1. **Correlation first** - Sets correlation ID before any other middleware
2. **ExceptionMapping second** - Can include correlation ID in error responses
3. **LoggingScope third** - Uses correlation ID set by first middleware

---

[← Back to File Guide](FILE_GUIDE.md) | [↑ Back to top](#table-of-contents)
