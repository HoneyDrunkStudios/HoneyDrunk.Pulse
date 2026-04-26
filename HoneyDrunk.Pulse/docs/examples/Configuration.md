# ⚙️ Configuration - Options and Settings

[← Back to File Guide](FILE_GUIDE.md)

---

## Table of Contents

- [Overview](#overview)
- [RestOptions.cs](#restoptionscs)
- [JsonOptionsDefaults.cs](#jsonoptionsdefaultscs)

---

## Overview

Configuration classes for customizing REST behavior.

**Location:** `HoneyDrunk.Web.Rest.AspNetCore/Configuration/` and `HoneyDrunk.Web.Rest.AspNetCore/Serialization/`

**Components:**
- **RestOptions** - Main configuration for middleware and filters
- **JsonOptionsDefaults** - JSON serialization defaults

[↑ Back to top](#table-of-contents)

---

## RestOptions.cs

**Main configuration options**

```csharp
public sealed class RestOptions
{
    public string CorrelationIdHeaderName { get; set; } = "X-Correlation-Id";
    public bool IncludeExceptionDetails { get; set; }
    public bool IncludeTraceId { get; set; } = true;
    public bool ReturnCorrelationIdInResponseHeader { get; set; } = true;
    public bool GenerateCorrelationIdIfMissing { get; set; } = true;
    public bool EnableRequestLoggingScope { get; set; } = true;
    public bool EnableExceptionMapping { get; set; } = true;
    public bool EnableModelStateValidationFilter { get; set; } = true;
    public bool ConfigureJsonDefaults { get; set; } = true;
    public bool EnableAuthFailureShaping { get; set; } = true;
}
```

### Purpose

Central configuration for all REST middleware and filters. Configure once during startup.

### Properties

#### Correlation ID Settings

| Property | Default | Description |
|----------|---------|-------------|
| `CorrelationIdHeaderName` | `"X-Correlation-Id"` | HTTP header name for correlation ID |
| `GenerateCorrelationIdIfMissing` | `true` | Generate new GUID if header not present |
| `ReturnCorrelationIdInResponseHeader` | `true` | Include correlation ID in response headers |

#### Error Handling Settings

| Property | Default | Description |
|----------|---------|-------------|
| `EnableExceptionMapping` | `true` | Enable exception-to-error mapping middleware |
| `IncludeExceptionDetails` | `false` | Include stack traces in error responses |
| `IncludeTraceId` | `true` | Include trace ID in responses (sourced from `System.Diagnostics.Activity.Current`) |

#### Feature Toggles

| Property | Default | Description |
|----------|---------|-------------|
| `EnableRequestLoggingScope` | `true` | Enable logging scope enrichment middleware |
| `EnableModelStateValidationFilter` | `true` | Enable MVC model validation filter |
| `ConfigureJsonDefaults` | `true` | Apply JSON serialization defaults |
| `EnableAuthFailureShaping` | `true` | Shape 401/403 as ApiErrorResponse |

### Usage Examples

#### Full Configuration

```csharp
builder.Services.AddRest(options =>
{
    // Correlation ID
    options.CorrelationIdHeaderName = "X-Correlation-Id";
    options.GenerateCorrelationIdIfMissing = true;
    options.ReturnCorrelationIdInResponseHeader = true;
    
    // Error handling
    options.EnableExceptionMapping = true;
    options.IncludeExceptionDetails = builder.Environment.IsDevelopment();
    options.IncludeTraceId = true;
    
    // Features
    options.EnableRequestLoggingScope = true;
    options.EnableModelStateValidationFilter = true;
    options.ConfigureJsonDefaults = true;
    options.EnableAuthFailureShaping = true;
});
```

#### Development Configuration

```csharp
builder.Services.AddRest(options =>
{
    // Show exception details in development
    options.IncludeExceptionDetails = true;
});
```

#### Minimal Configuration

```csharp
builder.Services.AddRest(options =>
{
    // Disable features you don't need
    options.EnableRequestLoggingScope = false;
    options.EnableModelStateValidationFilter = false;
    options.ConfigureJsonDefaults = false;
});
```

#### Custom Correlation Header

```csharp
builder.Services.AddRest(options =>
{
    // Use a different header name
    options.CorrelationIdHeaderName = "X-Request-Id";
});
```

### Environment-Based Configuration

```csharp
builder.Services.AddRest(options =>
{
    // Always enabled
    options.EnableExceptionMapping = true;
    options.EnableRequestLoggingScope = true;
    
    // Development only
    options.IncludeExceptionDetails = builder.Environment.IsDevelopment();
});
```

[↑ Back to top](#table-of-contents)

---

## JsonOptionsDefaults.cs

**JSON serialization defaults**

```csharp
public static class JsonOptionsDefaults
{
    public static JsonSerializerOptions SerializerOptions { get; }
    
    public static void Configure(JsonSerializerOptions options);
}
```

### Purpose

Provides consistent JSON serialization settings for all REST responses.

### Default Settings

| Setting | Value | Effect |
|---------|-------|--------|
| `PropertyNamingPolicy` | `CamelCase` | `FirstName` → `firstName` |
| `PropertyNameCaseInsensitive` | `true` | Accept `FirstName` or `firstName` |
| `DefaultIgnoreCondition` | `WhenWritingNull` | Omit null properties |
| `WriteIndented` | `false` | Compact output |
| `JsonStringEnumConverter` | `CamelCase` | Enums as strings |

### Automatic Registration

When `ConfigureJsonDefaults = true` (default), these settings are applied automatically to ASP.NET Core's MVC JSON serialization:

> **Note:** These defaults apply to MVC controllers and Minimal APIs using the framework's JSON handling. For manual serialization, use `JsonOptionsDefaults.SerializerOptions` explicitly.

```csharp
// ServiceCollectionExtensions.cs
if (options.ConfigureJsonDefaults)
{
    services.Configure<JsonOptions>(jsonOptions =>
    {
        JsonOptionsDefaults.Configure(jsonOptions.JsonSerializerOptions);
    });
}
```

### Manual Usage

```csharp
// Use the pre-configured options directly
var json = JsonSerializer.Serialize(data, JsonOptionsDefaults.SerializerOptions);

// Or configure your own options
var options = new JsonSerializerOptions();
JsonOptionsDefaults.Configure(options);
// Add custom settings
options.WriteIndented = true;
```

### Output Examples

These examples illustrate the difference between default .NET JSON serialization and HoneyDrunk REST conventions:

#### Before (hypothetical default .NET settings)

```json
{
  "Status": "Success",
  "CorrelationId": "abc-123",
  "Data": {
    "OrderId": "order-123",
    "CustomerName": "John Doe"
  }
}
```

#### After (REST defaults)

```json
{
  "status": "success",
  "correlationId": "abc-123",
  "data": {
    "orderId": "order-123",
    "customerName": "John Doe"
  }
}
```

**Key differences:**
- Property names are camelCase
- Enum is string (`"success"`) not number (`0`)
- Null properties (`Error`) are omitted
- More compact format

### Disabling JSON Defaults

If you have your own JSON configuration:

```csharp
builder.Services.AddRest(options =>
{
    options.ConfigureJsonDefaults = false;  // Use your own settings
});

// Configure your own JSON options
builder.Services.Configure<JsonOptions>(options =>
{
    options.JsonSerializerOptions.PropertyNamingPolicy = null;  // PascalCase
    options.JsonSerializerOptions.WriteIndented = true;
});
```

> **Note:** Disabling JSON defaults affects ASP.NET Core serialization settings only. JSON attributes on contract types (e.g., `[JsonIgnore]`) still apply regardless of this setting.

---

[← Back to File Guide](FILE_GUIDE.md) | [↑ Back to top](#table-of-contents)
