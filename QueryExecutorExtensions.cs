using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Infragistics.QueryBuilder.Executor
{
    public static class QueryExecutorExtensions
    {
        public static IServiceCollection AddQueryBuilder<TMyDbContext, TResults>(this IServiceCollection services) where TResults : class
        {
            services.AddScoped<QueryBuilderService<TMyDbContext, TResults>>();
            return services;
        }

        public static IEndpointRouteBuilder UseQueryBuilder<TMyDbContext, TResults>(this IEndpointRouteBuilder endpoints, string path) where TMyDbContext : class where TResults : class
        {
            endpoints.MapPost(path, ([FromBody] Query query, QueryBuilderService<TMyDbContext, TResults> queryBuilderService) =>
            {
                if (query != null)
                {
                    var result = queryBuilderService.RunQuery(query);
                    return Results.Ok(result);
                }
                else
                {
                    return Results.BadRequest("Wrong or missing query");
                }
            }).WithTags(["QueryBuilder"]).Accepts<Query>("application/json").Produces<TResults>();
            return endpoints;
        }
    }
}
