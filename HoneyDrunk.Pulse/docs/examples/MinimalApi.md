# 🚀 MinimalApi - Endpoint Conventions

[← Back to File Guide](FILE_GUIDE.md)

---

## Table of Contents

- [Overview](#overview)
- [RestEndpointConventions.cs](#restendpointconventionscs)
- [Usage Examples](#usage-examples)

---

## Overview

Extension methods for applying REST conventions to minimal API endpoints.

**Location:** `HoneyDrunk.Web.Rest.AspNetCore/MinimalApi/`

**Components:**
- **RestEndpointConventions** - Extension methods for `RouteHandlerBuilder`

**Purpose:**
- Annotate endpoints with OpenAPI metadata for consistent documentation
- Define standard produces/consumes for all endpoints
- Support typed success responses
- Provide operation-specific conventions (Create, Delete)

[↑ Back to top](#table-of-contents)

---

## RestEndpointConventions.cs

**Extension methods for minimal API endpoints**

```csharp
public static class RestEndpointConventions
{
    public static RouteHandlerBuilder WithRest(this RouteHandlerBuilder builder);
    public static RouteHandlerBuilder WithRest<TResponse>(this RouteHandlerBuilder builder);
    public static RouteHandlerBuilder WithRestCreate<TResponse>(this RouteHandlerBuilder builder);
    public static RouteHandlerBuilder WithRestDelete(this RouteHandlerBuilder builder);
    public static RouteHandlerBuilder RequireRestAuth(this RouteHandlerBuilder builder, string? policy = null);
}
```

### Purpose

Applies standard OpenAPI metadata to minimal API endpoints. This ensures consistent documentation across all endpoints and proper response type definitions for Swagger/OpenAPI tools.

### Methods

#### WithRest()

Applies standard REST conventions without a typed response:

```csharp
app.MapGet("/api/health", () => Results.Ok(ApiResult.Success()))
    .WithRest();
```

> **Note:** The following responses are documented as OpenAPI metadata. Actual error responses are produced when corresponding middleware (`ExceptionMappingMiddleware`) or auth integration (`EnableAuthFailureShaping`) is enabled.

**Produces:**
- 200 OK: `ApiResult`
- 400 Bad Request: `ApiErrorResponse`
- 401 Unauthorized: `ApiErrorResponse`
- 403 Forbidden: `ApiErrorResponse`
- 404 Not Found: `ApiErrorResponse`
- 500 Internal Server Error: `ApiErrorResponse`

#### WithRest\<TResponse\>()

Applies REST conventions with a typed success response:

```csharp
app.MapGet("/api/orders/{id}", (Guid id, IOrderService orderService) =>
{
    var order = orderService.GetById(id);
    return order is not null
        ? Results.Ok(ApiResult<Order>.Success(order))
        : Results.NotFound(ApiResult<Order>.NotFound());
})
.WithRest<Order>();
```

**Produces:**
- 200 OK: `ApiResult<Order>`
- 400 Bad Request: `ApiErrorResponse`
- 401 Unauthorized: `ApiErrorResponse`
- 403 Forbidden: `ApiErrorResponse`
- 404 Not Found: `ApiErrorResponse`
- 500 Internal Server Error: `ApiErrorResponse`

#### WithRestCreate\<TResponse\>()

Applies conventions for create operations (POST):

```csharp
app.MapPost("/api/orders", (CreateOrderRequest request, IOrderService orderService) =>
{
    var order = orderService.Create(request);
    return Results.Created($"/api/orders/{order.Id}", ApiResult<Order>.Success(order));
})
.WithRestCreate<Order>();
```

**Produces:**
- 201 Created: `ApiResult<Order>`
- 400 Bad Request: `ApiErrorResponse`
- 401 Unauthorized: `ApiErrorResponse`
- 403 Forbidden: `ApiErrorResponse`
- 409 Conflict: `ApiErrorResponse`
- 500 Internal Server Error: `ApiErrorResponse`

#### WithRestDelete()

Applies conventions for delete operations:

> **Note:** `WithRestDelete()` assumes 204 No Content semantics. If your API returns an envelope on delete (e.g., `ApiResult.Success()`), use `WithRest()` instead.

```csharp
app.MapDelete("/api/orders/{id}", (Guid id, IOrderService orderService) =>
{
    orderService.Delete(id);
    return Results.NoContent();
})
.WithRestDelete();
```

**Produces:**
- 204 No Content
- 400 Bad Request: `ApiErrorResponse`
- 401 Unauthorized: `ApiErrorResponse`
- 403 Forbidden: `ApiErrorResponse`
- 404 Not Found: `ApiErrorResponse`
- 500 Internal Server Error: `ApiErrorResponse`

#### RequireRestAuth()

Adds authorization with consistent error shaping:

```csharp
// Default authorization
app.MapGet("/api/orders", () => { /* ... */ })
    .WithRest<IEnumerable<Order>>()
    .RequireRestAuth();

// Named policy
app.MapDelete("/api/orders/{id}", (Guid id) => { /* ... */ })
    .WithRestDelete()
    .RequireRestAuth("AdminPolicy");
```

> **Note:** `RequireRestAuth()` calls ASP.NET Core's `RequireAuthorization()`. Consistent 401/403 error shaping requires authentication/authorization to be configured and `EnableAuthFailureShaping = true`.

[↑ Back to top](#table-of-contents)

---

## Usage Examples

### Complete CRUD API

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRest();
builder.Services.AddSingleton<IOrderService, OrderService>();

var app = builder.Build();

app.UseRest();

// Note: In minimal hosting, routing is configured implicitly.
// Ensure authentication/authorization middleware is added if using RequireRestAuth().

// GET all orders
app.MapGet("/api/orders", (IOrderService orderService) =>
{
    var orders = orderService.GetAll();
    return Results.Ok(ApiResult<IEnumerable<Order>>.Success(orders));
})
.WithRest<IEnumerable<Order>>()
.WithName("GetOrders")
.WithTags("Orders");

// GET single order
app.MapGet("/api/orders/{id}", (Guid id, IOrderService orderService) =>
{
    var order = orderService.GetById(id);
    return order is not null
        ? Results.Ok(ApiResult<Order>.Success(order))
        : Results.NotFound(ApiResult<Order>.NotFound($"Order {id} not found."));
})
.WithRest<Order>()
.WithName("GetOrder")
.WithTags("Orders");

// POST create order
app.MapPost("/api/orders", (CreateOrderRequest request, IOrderService orderService) =>
{
    var order = orderService.Create(request);
    return Results.Created(
        $"/api/orders/{order.Id}",
        ApiResult<Order>.Success(order));
})
.WithRestCreate<Order>()
.WithName("CreateOrder")
.WithTags("Orders");

// PUT update order
app.MapPut("/api/orders/{id}", (Guid id, UpdateOrderRequest request, IOrderService orderService) =>
{
    var order = orderService.Update(id, request);
    return order is not null
        ? Results.Ok(ApiResult<Order>.Success(order))
        : Results.NotFound(ApiResult<Order>.NotFound());
})
.WithRest<Order>()
.WithName("UpdateOrder")
.WithTags("Orders");

// DELETE order
app.MapDelete("/api/orders/{id}", (
    Guid id,
    IOrderService orderService,
    ICorrelationIdAccessor correlationIdAccessor) =>
{
    var deleted = orderService.Delete(id);
    return deleted
        ? Results.NoContent()
        : Results.NotFound(ApiErrorResponse.CreateNotFound(
            correlationIdAccessor.CorrelationId ?? string.Empty,
            $"Order {id} not found."));
})
.WithRestDelete()
.WithName("DeleteOrder")
.WithTags("Orders");

app.Run();
```

### With Correlation ID Access

```csharp
app.MapGet("/api/orders/{id}", (
    Guid id,
    IOrderService orderService,
    ICorrelationIdAccessor correlationIdAccessor) =>
{
    var order = orderService.GetById(id);
    var correlationId = correlationIdAccessor.CorrelationId;
    
    return order is not null
        ? Results.Ok(ApiResult<Order>.Success(order, correlationId))
        : Results.NotFound(ApiResult<Order>.NotFound("Order not found.", correlationId));
})
.WithRest<Order>();
```

### With Authorization

```csharp
// Public endpoint
app.MapGet("/api/products", () => { /* ... */ })
    .WithRest<IEnumerable<Product>>();

// Authenticated endpoint
app.MapGet("/api/orders", () => { /* ... */ })
    .WithRest<IEnumerable<Order>>()
    .RequireRestAuth();

// Admin-only endpoint
app.MapDelete("/api/orders/{id}", (Guid id) => { /* ... */ })
    .WithRestDelete()
    .RequireRestAuth("AdminPolicy");
```

### Chaining with Other Conventions

```csharp
app.MapGet("/api/orders", () => { /* ... */ })
    .WithRest<IEnumerable<Order>>()
    .WithName("GetOrders")
    .WithTags("Orders")
    .WithDescription("Retrieves all orders for the current user")
    .WithSummary("Get Orders")
    .RequireRestAuth()
    .CacheOutput(policy => policy.Expire(TimeSpan.FromMinutes(5)));
```

### OpenAPI/Swagger Output

With these conventions, your OpenAPI documentation will show:

> **Note:** Schema names (e.g., `ApiResultOfOrder`) may vary depending on your OpenAPI generator configuration.

```yaml
paths:
  /api/orders/{id}:
    get:
      operationId: GetOrder
      tags:
        - Orders
      parameters:
        - name: id
          in: path
          required: true
          schema:
            type: string
            format: uuid
      responses:
        '200':
          description: Success
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/ApiResultOfOrder'
        '400':
          description: Bad Request
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/ApiErrorResponse'
        '404':
          description: Not Found
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/ApiErrorResponse'
        '500':
          description: Internal Server Error
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/ApiErrorResponse'
```

---

[← Back to File Guide](FILE_GUIDE.md) | [↑ Back to top](#table-of-contents)
