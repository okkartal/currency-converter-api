# Currency Converter API

A robust, scalable, and maintainable currency conversion API built with C# and ASP.NET Core, providing high performance, security, and resilience.

## Features

- **Latest Exchange Rates**: Fetch the most current exchange rates for any base currency
- **Currency Conversion**: Convert amounts between different currencies
- **Historical Rates with Pagination**: Access past exchange rates with page navigation
- **Secure Authentication**: JWT-based authentication with role-based access control
- **Resilient Communication**: Implements caching, retry policies, and circuit breaker patterns
- **High Observability**: Structured logging and distributed tracing
- **Rate Limiting**: Protection against API abuse
- **Horizontally Scalable**: Designed for cloud deployment and high availability

## API Endpoints

### Currency Operations

- `GET /api/v1/currency/rates?baseCurrency=EUR` - Get latest exchange rates
- `POST /api/v1/currency/convert` - Convert between currencies
- `GET /api/v1/currency/historical?baseCurrency=EUR&startDate=2020-01-01&endDate=2020-01-31&page=1&pageSize=10` - Get historical rates with pagination

### Authentication

- `POST /api/auth/login` - Obtain JWT token for API access

## Tech Stack

- **ASP.NET Core 8.0** - Modern web API framework
- **Polly** - Resilience and transient fault handling
- **Serilog** - Structured logging
- **Redis** - Distributed caching
- **OpenTelemetry** - Distributed tracing
- **JWT** - Authentication and authorization
- **xUnit & Moq** - Testing framework

## Setup Instructions

### Prerequisites

- .NET 8.0 SDK or later
- Redis (optional, for distributed caching)

### Running the API

1. Clone the repository:
   ```
   git clone https://github.com/okkartal/currency-converter-api.git
   cd currency-converter-api
   ```

2. Build the solution:
   ```
   dotnet build
   ```

3. Run the API:
   ```
   cd CurrencyConverter.API
   dotnet run
   ```

4. Access the Swagger documentation:
   ```
   https://localhost:5001/swagger
   ```

### Running Tests

```
dotnet test
```

## Assumptions Made

1. The Frankfurter API is considered the primary source of truth for exchange rates
2. Cache expiration time is set to 15 minutes for latest rates and 1 hour for historical data
3. Restricted currencies (TRY, PLN, THB, MXN) cannot be used in any operation as per requirements
4. For simplicity, a fixed set of user credentials is implemented, but this should be replaced with a proper user store in production
5. Errors from the external API are considered transient and handled via retry policies
6. API operations might be expensive, so responses are cached to improve performance

## License

MIT