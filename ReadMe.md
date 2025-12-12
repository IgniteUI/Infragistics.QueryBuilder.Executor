# Infragistics.QueryBuilder.Executor

A .NET 8, .NET 9 library for dynamic, strongly-typed query building and execution over Entity Framework Core data sources. Supports advanced filtering, projection, and SQL generation.

[![NuGet](https://img.shields.io/nuget/v/Infragistics.QueryBuilder.Executor.svg)](https://www.nuget.org/packages/Infragistics.QueryBuilder.Executor/)

## Installation

Install via NuGet Package Manager:

`dotnet add package Infragistics.QueryBuilder.Executor`

## Features

- **Dynamic Query Execution**: Compose and execute queries at runtime using a flexible object model.
- **Advanced Filtering**: Nested filters, logical operators (AND/OR), and rich condition support.
- **Projection**: Select specific fields or project to DTOs.
- **SQL Generation**: Generate SQL from the query model for diagnostics or analysis.
- **ASP.NET Core Integration**: Easily expose query endpoints.

## Getting Started

### 1. Register the QueryBuilder Service

In your `Startup.cs` or Program configuration:

`services.AddQueryBuilder<MyDbContext, MyResultDto>();`

### 2. Expose a Query Endpoint

`app.UseEndpoints(endpoints => { endpoints.UseQueryBuilder<MyDbContext, MyResultDto>("/api/query"); });`

### 3. Example Query Payload

`{ "Entity": "Users", "ReturnFields": ["Id", "Name"], "Operator": "And", "FilteringOperands": [ { "FieldName": "IsActive", "Condition": { "Name": "equals" }, "SearchVal": true } ] }`

### 4. Generate SQL (Optional)

`var sql = SqlGenerator.GenerateSql(query);`

## Query Model

- **Query**: Describes the entity, fields, logical operator, and filters.
- **QueryFilter**: Represents a filter or group of filters.
- **QueryFilterCondition**: Specifies the comparison type (e.g., equals, contains).

## Dependencies

- .NET 8 or .NET 9
- Microsoft.EntityFrameworkCore
- AutoMapper
- builder.Services.AddControllers().AddNewtonsoftJson(o => o.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore) for working SwaggerUI

## License

[MIT](/LICENSE).
