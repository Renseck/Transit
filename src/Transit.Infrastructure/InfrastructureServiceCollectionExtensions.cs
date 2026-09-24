using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Transit.Core.Abstractions;
using Transit.Core.Services;
using Transit.Infrastructure.Configuration;
using Transit.Infrastructure.Excel;
using Transit.Infrastructure.Pdf;
using Transit.Infrastructure.Persistence;

namespace Transit.Infrastructure;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddTransitInfrastructure(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<StorageOptions>(configuration.GetSection(StorageOptions.SectionName));
        services.Configure<PdfConverterOptions>(configuration.GetSection(PdfConverterOptions.SectionName));
        services.Configure<SignatureOptions>(configuration.GetSection(SignatureOptions.SectionName));

        // Context factory: the Blazor Server-safe way to use EF (no shared scoped context across a circuit).
        services.AddDbContextFactory<TransitDbContext>((sp, options) =>
        {
            var storage = sp.GetRequiredService<IOptions<StorageOptions>>().Value;
            options.UseSqlite($"Data Source={storage.DatabasePath}");
        });

        services.AddScoped<IAttendanceStore, SqliteAttendanceStore>();
        services.AddScoped<ILocationStore, SqliteLocationStore>();
        services.AddScoped<IRouteStore, SqliteRouteStore>();
        services.AddScoped<ITemplateProfileStore, SqliteTemplateProfileStore>();
        services.AddScoped<IExcelWriter, ClosedXmlTemplateWriter>();
        services.AddScoped<IPdfConverter, LibreOfficePdfConverter>();
        services.AddScoped<IArtifactStore, FileSystemArtifactStore>();
        services.AddScoped<IDeclarationRunStore, SqliteDeclarationRunStore>();
        services.AddScoped<ISheetPreviewReader, ClosedXmlSheetPreviewReader>();
        services.AddScoped<LocationService>();
        services.AddScoped<RouteService>();
        services.AddScoped<DeclarationService>();
        services.AddScoped<DeclarationHistoryService>();
        services.AddScoped<GenerationService>();
        services.TryAddSingleton(TimeProvider.System);

        return services;
    }
}