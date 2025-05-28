using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Reflection;

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
                .FirstOrDefault(p => p.PropertyType.IsGenericType && p.Name.ToLower(CultureInfo.InvariantCulture) == t && p.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>));
            if (propInfo != null)
            {
                var methods = typeof(QueryExecutor).GetMethods(BindingFlags.Static | BindingFlags.Public);
                var method = methods?.FirstOrDefault(m => m.CustomAttributes.Count() == 2);

                var dbSet = propInfo.GetValue(db);
                var dbGenericType = dbSet?.GetType()?.GenericTypeArguments.FirstOrDefault();
                if (dbGenericType != null && dbSet != null)
                {
                    var dtoGenericType = typeof(TResults).GetProperty(propInfo.Name)?.PropertyType.GetElementType();
                    if (dtoGenericType != null)
                    {
                        var genericMethod = method?.MakeGenericMethod(dbGenericType, dtoGenericType);

                        var asQueryableMethod = dbSet.GetType().GetMethod("AsQueryable");
                        var queryable = asQueryableMethod?.Invoke(dbSet, null);
                        if (queryable != null)
                        {
                            if (genericMethod?.Invoke(null, [queryable, query, mapper]) is object[] propRes)
                            {
                                return new Dictionary<string, object[]> { { propInfo.Name, propRes } };
                            }
                        }
                    }
                }
            }

            throw new InvalidOperationException($"Unknown entity {t}");
        }
    }
}
