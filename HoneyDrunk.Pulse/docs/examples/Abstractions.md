# 📋 Abstractions - Pure Contracts

[← Back to File Guide](FILE_GUIDE.md)

---

## Table of Contents

- [Overview](#overview)
- [Results](#results)
  - [ApiResultStatus.cs](#apiresultstatuscs)
  - [ApiResult.cs](#apiresultcs)
  - [ApiResult\<T\>.cs](#apiresulttcs)
- [Errors](#errors)
  - [ApiErrorCode.cs](#apierrorcodecs)
  - [ApiError.cs](#apierrorcs)
  - [ApiErrorResponse.cs](#apierrorresponsecs)
  - [ValidationError.cs](#validationerrorcs)
- [Paging](#paging)
  - [PageRequest.cs](#pagerequestcs)
  - [PageResult\<T\>.cs](#pageresulttcs)
- [Constants](#constants)
  - [HeaderNames.cs](#headernamescs)
  - [MediaTypes.cs](#mediatypescs)
- [Telemetry](#telemetry)
  - [RestTelemetryTags.cs](#resttelemetrytagscs)

---

## Overview

Pure contracts with **zero runtime dependencies**. This package can be used in any .NET project without requiring ASP.NET Core.

**Location:** `HoneyDrunk.Web.Rest.Abstractions/`

**Key Contracts:**
- **Results** - `ApiResult`, `ApiResult<T>` response envelopes
- **Errors** - `ApiError`, `ApiErrorResponse`, `ValidationError` error structures
- **Paging** - `PageRequest`, `PageResult<T>` pagination support
- **Constants** - `HeaderNames`, `MediaTypes`, `ApiErrorCode` standard values
- **Telemetry** - `RestTelemetryTags` for observability

**Design Principles:**
- Immutable records for thread safety
- Static factory methods for common scenarios
- JSON serialization attributes for consistent output
- No external dependencies

[↑ Back to top](#table-of-contents)

---

## Results

### ApiResultStatus.cs

**Result status enumeration**

```csharp
public enum ApiResultStatus
{
    Success = 0,
    Failed = 1,
    Unauthorized = 2,
    Forbidden = 3,
    NotFound = 4,
    Conflict = 5,
    ValidationFailed = 6,
    Error = 7,
}
```

#### Purpose

Defines all possible result states for API operations. Used by `ApiResult` to indicate the outcome of an operation.

#### Values

| Value | Description | Typical HTTP Status |
|-------|-------------|---------------------|
| `Success` | Operation completed successfully | 200 OK |
| `Failed` | General failure | 400 Bad Request |
| `Unauthorized` | Authentication required | 401 Unauthorized |
| `Forbidden` | Insufficient permissions | 403 Forbidden |
| `NotFound` | Resource not found | 404 Not Found |
| `Conflict` | State conflict | 409 Conflict |
| `ValidationFailed` | Input validation failed | 400 Bad Request |
| `Error` | Internal error | 500 Internal Server Error |

> **Note:** `ApiResultStatus` values are serialized as camelCase strings (e.g., `"success"`, `"notFound"`).

> **Guidance:** `Failed` represents client-side or domain failures (400-class), while `Error` represents unexpected server failures (500-class).

[↑ Back to top](#table-of-contents)

---

### ApiResult.cs

**Non-generic result envelope**

```csharp
public record ApiResult
{
    public ApiResultStatus Status { get; init; } = ApiResultStatus.Success;
    
    [JsonIgnore]
    public bool IsSuccess => Status == ApiResultStatus.Success;
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? CorrelationId { get; init; }
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? TraceId { get; init; }
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ApiError? Error { get; init; }
    
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;

    // Factory methods
    public static ApiResult Success(string? correlationId = null);
    public static ApiResult Fail(string message, string code = ApiErrorCode.GeneralError, string? correlationId = null);
    public static ApiResult NotFound(string message = "The requested resource was not found.", string? correlationId = null);
    public static ApiResult Unauthorized(string message = "Authentication is required.", string? correlationId = null);
    public static ApiResult Forbidden(string message = "You do not have permission to access this resource.", string? correlationId = null);
    public static ApiResult Conflict(string message, string? correlationId = null);
    public static ApiResult InternalError(string message = "An internal server error occurred.", string? correlationId = null);
}
```

#### Purpose

Base result envelope for operations that don't return data. Provides consistent structure for all API responses.

#### Usage Example

```csharp
// Success without data
[HttpDelete("{id}")]
public IActionResult DeleteOrder(Guid id)
{
    _orderService.Delete(id);
    return Ok(ApiResult.Success());
}

// Error responses
[HttpGet("{id}")]
public IActionResult GetOrder(Guid id)
{
    var order = _orderService.GetById(id);
    
    if (order is null)
    {
        return NotFound(ApiResult.NotFound("Order not found."));
    }
    
    return Ok(ApiResult<Order>.Success(order));
}
```

#### JSON Output

```json
{
  "status": "success",
  "correlationId": "abc-123",
  "timestamp": "2024-01-01T00:00:00Z"
}
```

[↑ Back to top](#table-of-contents)

---

### ApiResult\<T\>.cs

**Generic result envelope with data**

```csharp
public record ApiResult<T> : ApiResult
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public T? Data { get; init; }

    // Factory methods
    public static ApiResult<T> Success(T data, string? correlationId = null);
    public static new ApiResult<T> Fail(string message, string code = ApiErrorCode.GeneralError, string? correlationId = null);
    public static new ApiResult<T> NotFound(string message = "The requested resource was not found.", string? correlationId = null);
    public static new ApiResult<T> Unauthorized(string message = "Authentication is required.", string? correlationId = null);
    public static new ApiResult<T> Forbidden(string message = "You do not have permission to access this resource.", string? correlationId = null);
    public static new ApiResult<T> Conflict(string message, string? correlationId = null);
    public static new ApiResult<T> InternalError(string message = "An internal server error occurred.", string? correlationId = null);
    public static ApiResult<T> FromResult(ApiResult result);
}
```

#### Purpose

Result envelope that includes a data payload. The primary response type for successful operations that return data.

#### Usage Example

```csharp
// Return single item
[HttpGet("{id}")]
public IActionResult GetOrder(Guid id)
{
    var order = _orderService.GetById(id);
    
    if (order is null)
    {
        return NotFound(ApiResult<Order>.NotFound());
    }
    
    return Ok(ApiResult<Order>.Success(order));
}

// Return collection
[HttpGet]
public IActionResult GetOrders()
{
    var orders = _orderService.GetAll();
    return Ok(ApiResult<IEnumerable<Order>>.Success(orders));
}

// Return created resource
[HttpPost]
public IActionResult CreateOrder([FromBody] CreateOrderRequest request)
{
    var order = _orderService.Create(request);
    return CreatedAtAction(
        nameof(GetOrder),
        new { id = order.Id },
        ApiResult<Order>.Success(order));
}
```

#### JSON Output

```json
{
  "status": "success",
  "correlationId": "abc-123",
  "timestamp": "2024-01-01T00:00:00Z",
  "data": {
    "id": "order-123",
    "customerId": "customer-456",
    "total": 99.99,
    "status": "pending"
  }
}
```

#### FromResult Conversion

The `FromResult` method converts a non-generic `ApiResult` to a typed `ApiResult<T>`. It copies all metadata (Status, Error, CorrelationId, TraceId, Timestamp) but does NOT include data.

```csharp
// Convert a non-generic failure to typed result
ApiResult baseResult = ApiResult.NotFound("Order not found.");
ApiResult<Order> typedResult = ApiResult<Order>.FromResult(baseResult);
// typedResult.Data is default (null for reference types)
```

[↑ Back to top](#table-of-contents)

---

## Errors

### ApiErrorCode.cs

**Standard error codes**

```csharp
public static class ApiErrorCode
{
    public const string GeneralError = "GENERAL_ERROR";
    public const string ValidationFailed = "VALIDATION_FAILED";
    public const string NotFound = "NOT_FOUND";
    public const string Unauthorized = "UNAUTHORIZED";
    public const string Forbidden = "FORBIDDEN";
    public const string Conflict = "CONFLICT";
    public const string BadRequest = "BAD_REQUEST";
    public const string InternalError = "INTERNAL_ERROR";
    public const string NotImplemented = "NOT_IMPLEMENTED";
    public const string ServiceUnavailable = "SERVICE_UNAVAILABLE";
}
```

#### Purpose

Machine-readable error codes for consistent error identification. Clients can switch on these codes for programmatic error handling.

#### Usage Example

```csharp
// Server-side
return BadRequest(ApiResult.Fail(
    "Invalid order amount",
    ApiErrorCode.BadRequest));

// Client-side
var result = await httpClient.GetFromJsonAsync<ApiResult<Order>>(url);
if (!result.IsSuccess)
{
    switch (result.Error?.Code)
    {
        case ApiErrorCode.NotFound:
            ShowNotFoundMessage();
            break;
        case ApiErrorCode.Forbidden:
            RedirectToLogin();
            break;
        default:
            ShowGenericError();
            break;
    }
}
```

[↑ Back to top](#table-of-contents)

---

### ApiError.cs

**Single error representation**

```csharp
public sealed record ApiError
{
    public required string Code { get; init; }
    public required string Message { get; init; }
    public string? Details { get; init; }
    public string? Target { get; init; }
}
```

#### Purpose

Represents a single error with code, message, and optional details. Used within `ApiResult` and `ApiErrorResponse`.

#### Properties

| Property | Description | Example |
|----------|-------------|---------|
| `Code` | Machine-readable error code | `"NOT_FOUND"` |
| `Message` | Human-readable error message | `"Order not found."` |
| `Details` | Optional additional details | Stack trace (dev only) |
| `Target` | Optional error target | `"orderId"` |

#### JSON Output

```json
{
  "code": "NOT_FOUND",
  "message": "The requested order was not found.",
  "target": "orderId"
}
```

[↑ Back to top](#table-of-contents)

---

### ApiErrorResponse.cs

**Error response envelope**

```csharp
public sealed record ApiErrorResponse
{
    public required string CorrelationId { get; init; }
    public required ApiError Error { get; init; }
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<ValidationError>? ValidationErrors { get; init; }
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? TraceId { get; init; }
    
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;

    // Factory methods
    public static ApiErrorResponse Create(string correlationId, string message, string code = ApiErrorCode.GeneralError, string? traceId = null);
    public static ApiErrorResponse CreateValidationError(string correlationId, IReadOnlyList<ValidationError> validationErrors, string? traceId = null);
    public static ApiErrorResponse CreateNotFound(string correlationId, string message = "The requested resource was not found.", string? traceId = null);
    public static ApiErrorResponse CreateUnauthorized(string correlationId, string message = "Authentication is required.", string? traceId = null);
    public static ApiErrorResponse CreateForbidden(string correlationId, string message = "You do not have permission to access this resource.", string? traceId = null);
    public static ApiErrorResponse CreateInternalError(string correlationId, string message = "An internal server error occurred.", string? traceId = null);
}
```

#### Purpose

Complete error response envelope used by middleware for non-2xx responses. Includes correlation, timestamp, and optional validation errors.

> **Note:** Specific factory methods (`CreateNotFound`, `CreateUnauthorized`, etc.) set appropriate error codes automatically. The generic `Create` method defaults to `GENERAL_ERROR` unless explicitly overridden.

#### Usage Example

```csharp
// Created automatically by ExceptionMappingMiddleware
// But can be created manually:

var errorResponse = ApiErrorResponse.CreateValidationError(
    correlationId: "abc-123",
    validationErrors: new List<ValidationError>
    {
        new() { Field = "email", Message = "Email is required." },
        new() { Field = "password", Message = "Password must be at least 8 characters." }
    });

return BadRequest(errorResponse);
```

#### JSON Output

```json
{
  "correlationId": "abc-123",
  "traceId": "00-abc123...",
  "timestamp": "2024-01-01T00:00:00Z",
  "error": {
    "code": "VALIDATION_FAILED",
    "message": "One or more validation errors occurred."
  },
  "validationErrors": [
    { "field": "email", "message": "Email is required." },
    { "field": "password", "message": "Password must be at least 8 characters." }
  ]
}
```

[↑ Back to top](#table-of-contents)

---

### ValidationError.cs

**Field-level validation error**

```csharp
public sealed record ValidationError
{
    public required string Field { get; init; }
    public required string Message { get; init; }
    public string? Code { get; init; }
}
```

#### Purpose

Represents a validation error for a specific field. Used in `ApiErrorResponse.ValidationErrors` collection.

#### Properties

| Property | Description | Example |
|----------|-------------|---------|
| `Field` | Field that failed validation | `"email"` |
| `Message` | Validation error message | `"Email is required."` |
| `Code` | Optional error code | `"REQUIRED"` |

#### JSON Output

```json
{
  "field": "email",
  "message": "Email format is invalid.",
  "code": "INVALID_FORMAT"
}
```

[↑ Back to top](#table-of-contents)

---

## Paging

### PageRequest.cs

**Pagination request parameters**

```csharp
public sealed record PageRequest
{
    public const int DefaultPageNumber = 1;
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;

    public int PageNumber { get; init; } = DefaultPageNumber;
    public int PageSize { get; init; } = DefaultPageSize;
    
    public int Skip => (NormalizedPageNumber - 1) * NormalizedPageSize;
    public int NormalizedPageNumber => Math.Max(1, PageNumber);
    public int NormalizedPageSize => Math.Clamp(PageSize, 1, MaxPageSize);

    public static PageRequest Default();
    public static PageRequest Create(int pageNumber, int pageSize);
}
```

#### Purpose

Standardized pagination request that can be bound from query parameters. Includes automatic normalization to prevent invalid values.

#### Usage Example

```csharp
[HttpGet]
public IActionResult GetOrders([FromQuery] PageRequest pageRequest)
{
    // pageRequest.Skip - for LINQ Skip()
    // pageRequest.NormalizedPageSize - for LINQ Take()
    
    var orders = _context.Orders
        .Skip(pageRequest.Skip)
        .Take(pageRequest.NormalizedPageSize)
        .ToList();
    
    var totalCount = _context.Orders.Count();
    
    var result = PageResult<Order>.Create(
        orders,
        pageRequest.NormalizedPageNumber,
        pageRequest.NormalizedPageSize,
        totalCount);
    
    return Ok(ApiResult<PageResult<Order>>.Success(result));
}
```

#### Query Parameters

```
GET /api/orders?pageNumber=2&pageSize=10
```

[↑ Back to top](#table-of-contents)

---

### PageResult\<T\>.cs

**Paginated result set**

```csharp
public sealed record PageResult<T>
{
    public required IReadOnlyList<T> Items { get; init; }
    public required int PageNumber { get; init; }
    public required int PageSize { get; init; }
    public required long TotalCount { get; init; }
    
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;
    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;

    public static PageResult<T> Empty(int pageNumber = 1, int pageSize = PageRequest.DefaultPageSize);
    public static PageResult<T> Create(IReadOnlyList<T> items, int pageNumber, int pageSize, long totalCount);
}
```

#### Purpose

Standardized paginated result with navigation metadata. Provides computed properties for easy client-side pagination UI.

#### Usage Example

```csharp
var result = PageResult<Order>.Create(
    items: orders,
    pageNumber: 2,
    pageSize: 10,
    totalCount: 95);

// result.TotalPages = 10
// result.HasPreviousPage = true
// result.HasNextPage = true
```

#### JSON Output

```json
{
  "items": [
    { "id": "order-1", "total": 99.99 },
    { "id": "order-2", "total": 149.99 }
  ],
  "pageNumber": 2,
  "pageSize": 10,
  "totalCount": 95,
  "totalPages": 10,
  "hasPreviousPage": true,
  "hasNextPage": true
}
```

[↑ Back to top](#table-of-contents)

---

## Constants

### HeaderNames.cs

**Standard HTTP header names**

```csharp
public static class HeaderNames
{
    public const string CorrelationId = "X-Correlation-Id";
    public const string RequestId = "X-Request-Id";
    public const string TraceId = "X-Trace-Id";
    public const string ApiVersion = "X-Api-Version";
    public const string RateLimitRemaining = "X-RateLimit-Remaining";
    public const string RateLimitReset = "X-RateLimit-Reset";
}
```

#### Purpose

Centralized HTTP header name constants. Use these instead of magic strings for consistency.

> **Note:** The library actively uses `CorrelationId`. Other headers (`RequestId`, `ApiVersion`, `RateLimitRemaining`, etc.) are provided for interoperability and consistency but are not set by this library.

#### Usage Example

```csharp
// Reading header
var correlationId = request.Headers[HeaderNames.CorrelationId].FirstOrDefault();

// Setting header
response.Headers[HeaderNames.CorrelationId] = correlationId;
```

[↑ Back to top](#table-of-contents)

---

### MediaTypes.cs

**Standard media types**

```csharp
public static class MediaTypes
{
    public const string Json = "application/json";
    public const string JsonUtf8 = "application/json; charset=utf-8";
    public const string ProblemJson = "application/problem+json";
    public const string PlainText = "text/plain";
    public const string OctetStream = "application/octet-stream";
}
```

#### Purpose

Centralized media type constants. Use these for Content-Type and Accept headers.

> **Note:** The library uses `Json` for all responses. `ProblemJson` is provided for RFC 7807 interoperability but is not used by default.

#### Usage Example

```csharp
response.ContentType = MediaTypes.Json;

[Produces(MediaTypes.Json)]
public class OrdersController : ControllerBase { }
```

[↑ Back to top](#table-of-contents)

---

## Telemetry

### RestTelemetryTags.cs

**Telemetry tag names for observability**

```csharp
public static class RestTelemetryTags
{
    public const string CorrelationId = "correlation.id";
    public const string RequestId = "request.id";
    public const string HttpMethod = "http.method";
    public const string HttpRoute = "http.route";
    public const string HttpStatusCode = "http.status_code";
    public const string HttpPath = "http.path";
    public const string ErrorType = "error.type";
    public const string ErrorCode = "error.code";
    public const string UserId = "user.id";
    public const string TenantId = "tenant.id";
    public const string ServiceName = "service.name";
    public const string ServiceVersion = "service.version";
    public const string RequestDurationMs = "request.duration_ms";
}
```

#### Purpose

Standard tag names for logging and tracing. Use these for consistent telemetry across services.

> **Note:** The library's `RequestLoggingScopeMiddleware` sets: `CorrelationId`, `HttpMethod`, `HttpPath`, `RequestId`, and `TenantId` (when Kernel context is available). Other tags are standardized names provided for consistency across services.

#### Usage Example

```csharp
using var activity = ActivitySource.StartActivity("ProcessOrder");
activity?.SetTag(RestTelemetryTags.CorrelationId, correlationId);
activity?.SetTag(RestTelemetryTags.HttpMethod, "POST");
activity?.SetTag(RestTelemetryTags.HttpRoute, "/api/orders");

_logger.LogInformation(
    "Processing order. {CorrelationId} {HttpMethod} {HttpRoute}",
    correlationId,
    "POST",
    "/api/orders");
```

---

[← Back to File Guide](FILE_GUIDE.md) | [↑ Back to top](#table-of-contents)
