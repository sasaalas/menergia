# MinunEnergia Web API

A REST API for retrieving heating consumption and cost data from MinunEnergia service.

## Overview

This API provides endpoints to access heating consumption data, costs, and temperature information for a specified period. It uses the same calculation logic as the console application's option 3.

## Endpoints

### GET /api/consumption/current-month

Returns consumption and cost data for the current month (April 1-29, 2026).

**Response:**
```json
{
  "period": "1.4.2026 - 29.4.2026",
  "totalConsumption": 0.828,
  "unit": "MWh",
  "totalCost": 122.22,
  "currency": "€",
  "previousYearComparison": {
    "previousYearConsumption": 0.0,
    "difference": 0.828,
    "differenceText": "kulutuksenne on 0,828 MWh enemmän"
  },
  "temperature": {
    "minimum": -3.3,
    "average": 4.6,
    "maximum": 15.0,
    "unit": "°C"
  }
}
```

### GET /api/consumption/health

Health check endpoint.

**Response:**
```json
{
  "status": "healthy",
  "timestamp": "2026-04-30T10:00:00Z"
}
```

## Configuration

Update `appsettings.json` with your MinunEnergia credentials:

```json
{
  "MinunEnergia": {
    "Username": "YOUR_USERNAME",
    "Password": "YOUR_PASSWORD"
  }
}
```

## Running the API

```bash
cd MinunEnergia.Api
dotnet run
```

The API will be available at:
- HTTPS: https://localhost:5001
- HTTP: http://localhost:5000
- Swagger UI: https://localhost:5001/swagger

## Cost Calculation

The API calculates total heating costs including:

1. **Energy costs**: consumption × energy price (with VAT)
2. **Sales basic charges**: monthly basic charge prorated for the period
3. **Network energy charges**: consumption × network price (with VAT)
4. **Network basic charges**: monthly network basic charge prorated for the period
5. **Energy taxes**: consumption × tax rate (with VAT)

This provides a comprehensive view of all heating-related costs.

## Dependencies

- .NET 10
- MinunEnergia console application (referenced project)
- Swashbuckle.AspNetCore (for Swagger/OpenAPI documentation)

## Features

- RESTful API design
- CORS enabled for cross-origin requests
- Swagger/OpenAPI documentation
- Comprehensive error handling
- Automatic initialization of consumption data on startup
