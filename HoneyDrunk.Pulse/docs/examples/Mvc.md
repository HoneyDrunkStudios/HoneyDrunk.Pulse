# 🎯 MVC - Filters and Controller Support

[← Back to File Guide](FILE_GUIDE.md)

---

## Table of Contents

- [Overview](#overview)
- [ModelStateValidationFilter.cs](#modelstatevalidationfiltercs)
- [ApiConventions.cs](#apiconventionscs)

---

## Overview

MVC components for controller-based APIs including filters and action result conventions.

**Location:** `HoneyDrunk.Web.Rest.AspNetCore/Mvc/`

**Components:**
- **ModelStateValidationFilter** - Converts invalid model state to `ApiErrorResponse`
- **ApiConventions** - Static methods for consistent action results

**Registration:**
```csharp
services.AddRest(options =>
{
    options.EnableModelStateValidationFilter = true;  // Adds filter to MVC
});
```

[↑ Back to top](#table-of-contents)

---

## ModelStateValidationFilter.cs

**Model state validation action filter**

```csharp
public sealed class ModelStateValidationFilter : IActionFilter
{
    public void OnActionExecuting(ActionExecutingContext context);
    public void OnActionExecuted(ActionExecutedContext context);
}
```

### Purpose

Intercepts requests with invalid model state and returns a standardized `ApiErrorResponse` with validation errors. Replaces the default ASP.NET Core validation response.

### Behavior

1. **Check ModelState** - If valid, continue to action
2. **Extract correlation ID** - From `ICorrelationIdAccessor` or `HttpContext.Items`
3. **Extract trace ID** - From `Activity.Current?.Id`
4. **Build validation errors** - Convert `ModelState` entries to `ValidationError`
5. **Short-circuit** - Return 400 Bad Request with `ApiErrorResponse`

### Registration

The filter is automatically registered when using `AddRest`:

```csharp
services.AddRest(options =>
{
    options.EnableModelStateValidationFilter = true;  // Default: true
});

// This also suppresses the default ASP.NET Core validation filter:
// apiBehaviorOptions.SuppressModelStateInvalidFilter = true;
```

### Usage Example

```csharp
// Model with validation attributes
public class CreateOrderRequest
{
    [Required(ErrorMessage = "Customer ID is required.")]
    public Guid CustomerId { get; init; }
    
    [Required(ErrorMessage = "At least one item is required.")]
    [MinLength(1, ErrorMessage = "At least one item is required.")]
    public List<OrderItem> Items { get; init; } = [];
    
    [Range(0.01, double.MaxValue, ErrorMessage = "Total must be greater than zero.")]
    public decimal Total { get; init; }
}

// Controller action
[HttpPost]
public IActionResult CreateOrder([FromBody] CreateOrderRequest request)
{
    // If request is invalid, ModelStateValidationFilter short-circuits
    // and returns 400 with ApiErrorResponse before this code runs
    
    var order = _orderService.Create(request);
    return Ok(ApiResult<Order>.Success(order));
}
```

### Response Example

Request with invalid model:
```json
{
  "customerId": "00000000-0000-0000-0000-000000000000",
  "items": [],
  "total": -5.00
}
```

Response (400 Bad Request):
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
    {
      "field": "Items",
      "message": "At least one item is required."
    },
    {
      "field": "Total",
      "message": "Total must be greater than zero."
    }
  ]
}
```

> **Note:** Field names are derived from `ModelState` keys and may vary based on binding source and naming conventions (e.g., `Items`, `items`, or `request.Items`).

### Correlation ID Resolution

The filter resolves correlation ID in this order:

1. **ICorrelationIdAccessor** - Preferred source (set by CorrelationMiddleware)
2. **HttpContext.Items[HeaderNames.CorrelationId]** - Fallback if accessor not available
3. **New GUID** - Generated if neither available (indicates correlation middleware hasn't run)

> **Recommendation:** Always register `CorrelationMiddleware` before MVC to ensure consistent correlation IDs across all responses.

```csharp
string correlationId = accessor?.CorrelationId
    ?? (context.HttpContext.Items.TryGetValue(HeaderNames.CorrelationId, out var value) 
        && value is string id ? id : Guid.NewGuid().ToString("N"));
```

[↑ Back to top](#table-of-contents)

---

## ApiConventions.cs

**Static helper methods for consistent action results**

```csharp
public static class ApiConventions
{
    public static IActionResult Ok<T>(T data, string? correlationId = null);
    public static IActionResult Created<T>(string location, T data, string? correlationId = null);
    public static IActionResult NoContent();
    public static IActionResult BadRequest(string correlationId, string message, string? traceId = null);
    public static IActionResult NotFound(string correlationId, string? message = null, string? traceId = null);
    public static IActionResult Unauthorized(string correlationId, string? message = null, string? traceId = null);
    public static IActionResult Forbidden(string correlationId, string? message = null, string? traceId = null);
    public static IActionResult Conflict(string correlationId, string message, string? traceId = null);
    public static IActionResult InternalError(string correlationId, string? message = null, string? traceId = null);
}
```

### Purpose

Provides static helper methods for creating consistent action results. Wraps data in `ApiResult<T>` for success and `ApiErrorResponse` for errors.

> **Note:** These helpers are intended for MVC controllers and advanced scenarios requiring explicit `IActionResult` creation. In typical applications, prefer returning `ApiResult<T>` directly and letting middleware populate correlation and trace metadata automatically.

**ApiConventions vs ApiResult factories:**
`ApiConventions` methods return `IActionResult` directly (e.g., `OkObjectResult`), while `ApiResult` factories return data contracts intended to be serialized by framework helpers. They produce equivalent response bodies but differ in responsibility—`ApiConventions` handles HTTP result creation, `ApiResult` handles data shaping.

### Success Methods

#### Ok\<T\>

Returns 200 OK with data wrapped in `ApiResult<T>`:

```csharp
[HttpGet("{id}")]
public IActionResult GetOrder(Guid id)
{
    var order = _orderService.GetById(id);
    var correlationId = _correlationIdAccessor.CorrelationId;
    
    return ApiConventions.Ok(order, correlationId);
}
```

Response:
```json
{
  "status": "success",
  "correlationId": "abc-123",
  "timestamp": "2024-01-01T00:00:00Z",
  "data": {
    "id": "order-123",
    "total": 99.99
  }
}
```

#### Created\<T\>

Returns 201 Created with location header:

```csharp
[HttpPost]
public IActionResult CreateOrder([FromBody] CreateOrderRequest request)
{
    var order = _orderService.Create(request);
    var location = $"/api/orders/{order.Id}";
    var correlationId = _correlationIdAccessor.CorrelationId;
    
    return ApiConventions.Created(location, order, correlationId);
}
```

#### NoContent

Returns 204 No Content:

```csharp
[HttpDelete("{id}")]
public IActionResult DeleteOrder(Guid id)
{
    _orderService.Delete(id);
    return ApiConventions.NoContent();
}
```

### Error Methods

#### BadRequest

Returns 400 Bad Request with `ApiErrorResponse`:

```csharp
[HttpPost]
public IActionResult CreateOrder([FromBody] CreateOrderRequest request)
{
    if (request.Total <= 0)
    {
        var correlationId = _correlationIdAccessor.CorrelationId;
        return ApiConventions.BadRequest(correlationId, "Total must be greater than zero.");
    }
    
    // ... create order
}
```

#### NotFound

Returns 404 Not Found with `ApiErrorResponse`:

```csharp
[HttpGet("{id}")]
public IActionResult GetOrder(Guid id)
{
    var order = _orderService.GetById(id);
    
    if (order is null)
    {
        var correlationId = _correlationIdAccessor.CorrelationId;
        return ApiConventions.NotFound(correlationId, $"Order {id} not found.");
    }
    
    return ApiConventions.Ok(order, correlationId);
}
```

#### Unauthorized

Returns 401 Unauthorized with `ApiErrorResponse`:

```csharp
return ApiConventions.Unauthorized(correlationId, "Invalid API key.");
```

#### Forbidden

Returns 403 Forbidden with `ApiErrorResponse`:

```csharp
return ApiConventions.Forbidden(correlationId, "You do not have access to this order.");
```

#### Conflict

Returns 409 Conflict with `ApiErrorResponse`:

```csharp
[HttpPost("{id}/cancel")]
public IActionResult CancelOrder(Guid id)
{
    var order = _orderService.GetById(id);
    
    if (order.Status == OrderStatus.Shipped)
    {
        var correlationId = _correlationIdAccessor.CorrelationId;
        return ApiConventions.Conflict(correlationId, "Cannot cancel an order that has been shipped.");
    }
    
    // ... cancel order
}
```

#### InternalError

Returns 500 Internal Server Error with `ApiErrorResponse`:

```csharp
return ApiConventions.InternalError(correlationId, "An unexpected error occurred while processing the order.");
```

### When to Use

**Recommended Patterns (in order of preference):**

1. **Return `ApiResult<T>` + middleware** - Preferred default. Middleware handles correlation, trace IDs, and consistent formatting automatically.
2. **Throw exceptions + ExceptionMappingMiddleware** - For error flows. Middleware converts exceptions to `ApiErrorResponse`.
3. **Use `ApiConventions`** - When you need explicit `IActionResult` creation without exceptions, or in scenarios where middleware isn't available.

| Scenario | Use ApiConventions | Use ControllerBase Methods |
|----------|-------------------|----------------------------|
| Need standardized response | Yes | No |
| Quick prototyping | No | Yes |
| Error responses | Yes (structured) | No (plain) |
| Success with data | Yes | Both work |

> **Note:** `ApiConventions` is designed for MVC controllers, not minimal APIs. For minimal API patterns, see [MinimalApi.md](MinimalApi.md).

---

[← Back to File Guide](FILE_GUIDE.md) | [↑ Back to top](#table-of-contents)
