# 📦 HoneyDrunk.Web.Rest - Complete File Guide

## Overview

**Think of this library as a universal translator for your REST APIs**

Just like how a translator ensures everyone speaks the same language, this library ensures all your REST APIs respond consistently. It provides standardized response envelopes, correlation propagation, exception mapping, and validation handling so every API endpoint in your application follows the same conventions.

**Key Concepts:**
- **Response Envelopes** - Unified `ApiResult<T>` and `ApiErrorResponse` contracts for consistent API responses
- **Correlation Propagation** - `X-Correlation-Id` header handling with `ICorrelationIdAccessor`
- **Exception Mapping** - Automatic exception-to-HTTP-status mapping with configurable strategies
- **Validation Handling** - Consistent validation error responses with `ValidationError` collections
- **Pagination** - `PageRequest` and `PageResult<T>` for standardized paging
- **JSON Conventions** - camelCase, enum-as-camelCase-string, null-omission defaults
- **Kernel Integration** - Prefers `IOperationContext.CorrelationId` when available
- **Auth Integration** - Shapes 401/403 as `ApiErrorResponse` via `IAuthorizationMiddlewareResultHandler`
- **Transport Integration** - Maps `ITransportEnvelope` metadata to `ApiResult` for HTTP responses (no transport runtime)

---

## 📚 Documentation Structure

This guide is organized into focused documents by domain:

### 🏛️ Architecture

| Document | Description |
|----------|-------------|
| [Architecture.md](Architecture.md) | **Layer responsibilities, design principles, and integration patterns** |

### 🔷 HoneyDrunk.Web.Rest.Abstractions

| Domain | Document | Description |
|--------|----------|-------------|
| 📋 **Contracts** | [Abstractions.md](Abstractions.md) | Pure contracts with no dependencies (errors, results, paging, constants) |

### 🔸 HoneyDrunk.Web.Rest.AspNetCore

| Domain | Document | Description |
|--------|----------|-------------|
| ⚙️ **Configuration** | [Configuration.md](Configuration.md) | Options, settings, and JSON defaults |
| 🔄 **Middleware** | [Middleware.md](Middleware.md) | Correlation, exception mapping, request logging |
| 🎯 **MVC** | [Mvc.md](Mvc.md) | Filters, conventions, and controller support |
| 🚀 **MinimalApi** | [MinimalApi.md](MinimalApi.md) | Endpoint conventions for minimal APIs |
| 🔌 **DI** | [DependencyInjection.md](DependencyInjection.md) | Service registration and extensions |

---

## 🔷 Quick Start

### Basic Concepts

**Response Flow:**
```
Controller/Endpoint              Middleware                    Client
      ↓                             ↓                            ↓
Return Data → Wrap in ApiResult → Add Correlation → JSON Response
      ↓                             ↓                            ↓
  Order        ApiResult<Order>    X-Correlation-Id        HTTP 200 + JSON
```

**Error Flow:**
```
Controller/Endpoint              Middleware                    Client
      ↓                             ↓                            ↓
Throw Exception → Catch → Map to ApiErrorResponse → JSON Response
      ↓                             ↓                            ↓
KeyNotFoundException  ExceptionMappingMiddleware    HTTP 404 + ApiErrorResponse
```

**Correlation Flow:**
```
Client Request                   Server                    Downstream Services
      ↓                             ↓                            ↓
X-Correlation-Id: abc → CorrelationMiddleware → ICorrelationIdAccessor → Pass abc
      ↓                             ↓                            ↓
  abc-123               Extract & Store           Inject into calls
```

### Installation

```bash
# Full ASP.NET Core integration
dotnet add package HoneyDrunk.Web.Rest.AspNetCore

# Or just the contracts (no runtime dependencies)
dotnet add package HoneyDrunk.Web.Rest.Abstractions
```

### Basic Usage

```csharp
// Program.cs - Setup
using HoneyDrunk.Web.Rest.AspNetCore.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Register REST services
builder.Services.AddRest(options =>
{
    options.EnableExceptionMapping = true;
    options.EnableRequestLoggingScope = true;
    options.EnableModelStateValidationFilter = true;
    options.IncludeExceptionDetails = builder.Environment.IsDevelopment();
    options.IncludeTraceId = true;
});

builder.Services.AddControllers();

var app = builder.Build();

// Add REST middleware early in the pipeline
app.UseRest();

app.MapControllers();

app.Run();
```

```csharp
// Controller Usage
using HoneyDrunk.Web.Rest.Abstractions.Results;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;
    
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
    
    [HttpPost]
    public IActionResult CreateOrder([FromBody] CreateOrderRequest request)
    {
        var order = _orderService.Create(request);
        return Ok(ApiResult<Order>.Success(order));
    }
}
```

```csharp
// Accessing Correlation ID
using HoneyDrunk.Web.Rest.AspNetCore.Context;

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
    
    public Order CreateOrder(CreateOrderRequest request)
    {
        var correlationId = _correlationIdAccessor.CorrelationId;
        
        _logger.LogInformation(
            "Creating order with CorrelationId {CorrelationId}",
            correlationId);
        
        // ... create order
        return new Order();
    }
}
```

---

## 🔷 Design Philosophy

### Core Principles

1. **Consistency** - All APIs respond with the same envelope structure
2. **Observability** - Correlation IDs and trace IDs in every response
3. **Developer Experience** - Simple factory methods for common responses
4. **Separation of Concerns** - Pure contracts in Abstractions, runtime in AspNetCore
5. **Convention over Configuration** - Sensible defaults that work out of the box
6. **Optional Integration** - Kernel/Auth/Transport integrations gracefully degrade when not registered

### Why These Patterns?

**Response Envelope Pattern:**
- Separates metadata (status, correlation, timestamp) from payload (business data)
- Enables consistent client-side handling regardless of endpoint
- Provides structured error information for debugging

**Correlation Propagation:**
- Trace requests across distributed systems
- Link logs, traces, and metrics by correlation ID
- Simplify debugging and incident response
- Prefers Kernel `IOperationContext.CorrelationId` when available

**Exception Mapping:**
- Convert domain exceptions to appropriate HTTP status codes
- Hide internal exception details in production
- Provide consistent error response structure

**Validation Handling:**
- Standardize validation error responses
- Support multiple validation errors per field
- Machine-readable error codes for client handling

**Auth Failure Shaping:**
- Uses `IAuthorizationMiddlewareResultHandler` for scheme-agnostic 401/403 handling
- Shapes responses as `ApiErrorResponse` with correlation ID
- Registered automatically by `AddRest()` when `EnableAuthFailureShaping` is true

---

## 📦 Project Structure

```
HoneyDrunk.Web.Rest/
├── HoneyDrunk.Web.Rest.Abstractions/    # Pure contracts (no dependencies)
│   ├── Constants/                        # HeaderNames, MediaTypes
│   │   ├── HeaderNames.cs               # Standard HTTP header names
│   │   └── MediaTypes.cs                # Standard media types
│   ├── Errors/                           # Error contracts
│   │   ├── ApiError.cs                  # Single error representation
│   │   ├── ApiErrorCode.cs              # Machine-readable error codes
│   │   ├── ApiErrorResponse.cs          # Error response envelope
│   │   └── ValidationError.cs           # Field-level validation error
│   ├── Paging/                           # Pagination contracts
│   │   ├── PageRequest.cs               # Pagination request parameters
│   │   └── PageResult.cs                # Paginated result set
│   ├── Results/                          # Result contracts
│   │   ├── ApiResult.cs                 # Non-generic result envelope
│   │   ├── ApiResultOfT.cs              # Generic result with data
│   │   └── ApiResultStatus.cs           # Result status enumeration
│   └── Telemetry/                        # Observability constants
│       └── RestTelemetryTags.cs         # Telemetry tag names
│
├── HoneyDrunk.Web.Rest.AspNetCore/      # ASP.NET Core integration
│   ├── Auth/                             # Authentication handling
│   │   ├── RestAuthExtensions.cs        # Auth response helper methods
│   │   └── RestAuthorizationResultHandler.cs # IAuthorizationMiddlewareResultHandler
│   ├── Configuration/                    # Options and settings
│   │   └── RestOptions.cs               # Configuration options
│   ├── Context/                          # Request context
│   │   ├── ICorrelationIdAccessor.cs    # Correlation ID contract
│   │   └── CorrelationIdAccessor.cs     # AsyncLocal implementation
│   ├── Errors/                           # Exception handling
│   │   ├── DefaultExceptionMappings.cs  # Default exception mappings
│   │   └── ExceptionToApiErrorMapper.cs # Exception-to-error mapper
│   ├── Extensions/                       # DI and middleware extensions
│   │   ├── ApplicationBuilderExtensions.cs # app.UseRest()
│   │   ├── EndpointRouteBuilderExtensions.cs # Endpoint conventions
│   │   └── ServiceCollectionExtensions.cs # services.AddRest()
│   ├── Middleware/                       # Request pipeline
│   │   ├── CorrelationMiddleware.cs     # Correlation ID propagation (uses Kernel if available)
│   │   ├── ExceptionMappingMiddleware.cs # Exception-to-error mapping
│   │   ├── ExceptionMappingResult.cs    # Mapping result type
│   │   └── RequestLoggingScopeMiddleware.cs # Logging enrichment (uses Kernel if available)
│   ├── MinimalApi/                       # Minimal API support
│   │   └── RestEndpointConventions.cs   # Endpoint response and auth conventions
│   ├── Mvc/                              # MVC support
│   │   ├── ApiConventions.cs            # Controller conventions
│   │   └── ModelStateValidationFilter.cs # Validation filter
│   ├── Serialization/                    # JSON handling
│   │   └── JsonOptionsDefaults.cs       # JSON serializer defaults
│   └── Transport/                        # Transport integration
│       └── TransportResultExtensions.cs # ITransportEnvelope to ApiResult mapping
│
└── HoneyDrunk.Web.Rest.Tests/           # Integration tests
    ├── Abstractions/                     # Contract tests
    ├── AspNetCore/                       # Middleware tests
    └── TestHost/                         # Test infrastructure
```

---

## 🎯 Key Features

### Response Envelopes

- **ApiResult** - Non-generic result with status, error, correlation
- **ApiResult\<T\>** - Generic result with data payload
- **ApiErrorResponse** - Structured error response
- Factory methods for common scenarios (Success, Fail, NotFound, etc.)

### Exception Mapping

| Exception Type | HTTP Status | Error Code |
|----------------|-------------|------------|
| `KeyNotFoundException` | 404 Not Found | `NOT_FOUND` |
| `ArgumentException` | 400 Bad Request | `BAD_REQUEST` |
| `ArgumentNullException` | 400 Bad Request | `BAD_REQUEST` |
| `InvalidOperationException` | 409 Conflict | `CONFLICT` |
| `UnauthorizedAccessException` | 403 Forbidden | `FORBIDDEN` |
| `NotImplementedException` | 501 Not Implemented | `NOT_IMPLEMENTED` |
| `OperationCanceledException` | 499 Client Closed (client aborted) | `GENERAL_ERROR` |
| Other exceptions | 500 Internal Server Error | `INTERNAL_ERROR` |

### Configuration Options

```csharp
services.AddRest(options =>
{
    // Middleware toggles
    options.EnableExceptionMapping = true;          // Exception-to-error mapping
    options.EnableRequestLoggingScope = true;       // Logging scope enrichment
    options.EnableModelStateValidationFilter = true; // MVC validation filter
    options.EnableAuthFailureShaping = true;        // Shape 401/403 as ApiErrorResponse
    
    // Response enrichment
    options.IncludeExceptionDetails = false;        // Stack traces in errors (dev only)
    options.IncludeTraceId = true;                  // OpenTelemetry trace ID
    
    // Correlation ID
    options.CorrelationIdHeaderName = "X-Correlation-Id";
    options.GenerateCorrelationIdIfMissing = true;
    options.ReturnCorrelationIdInResponseHeader = true;
    
    // JSON
    options.ConfigureJsonDefaults = true;           // Apply JSON conventions
});
```

### Optional Integrations

When registered, the middleware automatically uses:

- **HoneyDrunk.Kernel** - `IOperationContextAccessor` for correlation and logging enrichment
- **HoneyDrunk.Auth.AspNetCore** - `IAuthenticatedIdentityAccessor` for context-aware auth errors
- **HoneyDrunk.Transport** - `ITransportEnvelope.ToApiResult()` extension methods

---

## 🔗 Relationships

### Upstream Dependencies

- **System.Text.Json** - JSON serialization
- **Microsoft.AspNetCore.*** - ASP.NET Core runtime (AspNetCore package only)
- **Microsoft.Extensions.*** - DI, Logging, Configuration
- **HoneyDrunk.Kernel.Abstractions** - Optional, for Grid context
- **HoneyDrunk.Auth.AspNetCore** - Optional, for identity context
- **HoneyDrunk.Transport** - Optional, for envelope mapping

### Downstream Consumers

Applications using HoneyDrunk.Web.Rest:

- **REST APIs** - Consistent response contracts
- **Microservices** - Correlation propagation across boundaries
- **Client Libraries** - Predictable error handling

---

## 📖 Additional Resources

### Official Documentation
- [README.md](../README.md) - Project overview and quick start
- [Abstractions README](../HoneyDrunk.Web.Rest.Abstractions/README.md) - Contracts documentation
- [AspNetCore README](../HoneyDrunk.Web.Rest.AspNetCore/README.md) - Middleware documentation

### Related Projects
- [HoneyDrunk.Kernel](https://github.com/HoneyDrunkStudios/HoneyDrunk.Kernel) - Core Grid primitives (optional integration)
- [HoneyDrunk.Auth](https://github.com/HoneyDrunkStudios/HoneyDrunk.Auth) - Authentication and authorization (optional integration)
- [HoneyDrunk.Transport](https://github.com/HoneyDrunkStudios/HoneyDrunk.Transport) - Messaging infrastructure (optional integration)
- [HoneyDrunk.Data](https://github.com/HoneyDrunkStudios/HoneyDrunk.Data) - Persistence conventions
- [HoneyDrunk.Standards](https://github.com/HoneyDrunkStudios/HoneyDrunk.Standards) - Analyzers and conventions

---

## 💡 Motto

**"Every response speaks the same language."** - Consistency across all your APIs.

---

*Last Updated: 2026-01-11*  
*Target Framework: .NET 10.0*
