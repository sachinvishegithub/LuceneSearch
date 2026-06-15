using LuceneSearchHelper.Configuration;
using LuceneSearchHelper.Interceptors;
using LuceneSearchHelper.Mapping;
using LuceneSearchHelper.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LuceneSearchHelper;

public static class LuceneServiceCollectionExtensions
{
  public static IServiceCollection AddLucene(
      this IServiceCollection services,
      IConfiguration configuration)
  {
        services.Configure<LuceneOptions>(configuration.GetSection(LuceneOptions.SectionName));
        services.Configure<LuceneEntityMappingOptions>(configuration.GetSection(LuceneEntityMappingOptions.SectionName));

        services.AddSingleton<ILuceneIndexProvider, LuceneIndexProvider>();
        services.AddSingleton<ILuceneIndexManager, LuceneIndexManager>();
        services.AddSingleton<LuceneQueryBuilder>();
        services.AddSingleton<IEntityDocumentMapper, EntityDocumentMapper>();
        services.AddScoped<ILuceneIndexingService, LuceneIndexingService>();
        services.AddScoped<ILuceneSearchService, LuceneSearchService>();
        services.AddSingleton<LuceneSaveChangesInterceptor>();
        services.AddHostedService<LuceneBootstrapHostedService>();


        // Set buffer to 256MB or 512MB depending on your system RAM
        //services.SetRAMBufferSizeMB(256.0);
        return services;
    }
}
