// <copyright file="WeatherForecast.cs" company="HoneyDrunk Studios">
// Copyright (c) HoneyDrunk Studios. All rights reserved.
// </copyright>

namespace HoneyDrunk.Pulse.Sample.Api;

/// <summary>
/// Weather forecast record.
/// </summary>
/// <param name="Date">The forecast date.</param>
/// <param name="TemperatureC">Temperature in Celsius.</param>
/// <param name="Summary">Weather summary.</param>
internal sealed record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    /// <summary>
    /// Gets the temperature in Fahrenheit.
    /// </summary>
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
