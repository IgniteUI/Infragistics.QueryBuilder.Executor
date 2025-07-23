using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Reflection;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infragistics.QueryBuilder.Executor
{
    public class QueryBuilderService<TMyDbContext, TResults>(TMyDbContext db, IMapper mapper, ILogger<QueryBuilderService<TMyDbContext, TResults>>? logger) where TResults : class
    {
        public Dictionary<string, object[]> RunQuery(Query query)
        {
            var sanitizedEntity = query.Entity.Replace("\r", string.Empty).Replace("\n", string.Empty);
            logger?.LogInformation("Executing query for entity: {Entity}", sanitizedEntity);
            var t = query.Entity.ToLower(CultureInfo.InvariantCulture);

            var propInfo = db?.GetType().GetProperties()
                .FirstOrDefault(p =>
                    p.PropertyType.IsGenericType &&
                    p.Name.ToLower(CultureInfo.InvariantCulture) == t &&
                    p.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>))
                ?? throw new InvalidOperationException($"Unknown entity {t}");

            var dbSet = propInfo.GetValue(db) ?? throw new ValidationException($"DbSet property '{propInfo.Name}' is null in DbContext.");
            var dbGenericType = dbSet.GetType().GenericTypeArguments.FirstOrDefault() ?? throw new ValidationException($"Missing DbSet generic type");

            var resultProperty = typeof(TResults).GetProperty(t, BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase)
                ?? throw new ValidationException($"Unknown entity {t}");

            var dtoGenericType = resultProperty.PropertyType.GetElementType() ?? throw new ValidationException($"Missing Dto generic type");

            var queryable = dbSet.GetType().GetMethod("AsQueryable")?.Invoke(dbSet, null)
                ?? throw new InvalidOperationException($"DbSet '{propInfo.Name}' does not support AsQueryable().");

            var propRes = QueryExecutor.InvokeRunMethod([dbGenericType, dtoGenericType], [queryable, query, mapper]);
            return new Dictionary<string, object[]> { { propInfo.Name.ToLowerInvariant(), propRes } };
        }
    }
}
