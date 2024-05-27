using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using AspnetCore.Hal.Processors;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AspnetCore.Hal.SystemTextHalJsonFormatter
{

    public static class Extensions
    {
        public static void AddHalSupport(this IServiceCollection services)
        {
            services.TryAddEnumerable(ServiceDescriptor.Transient<IPostConfigureOptions<MvcOptions>, HalJsonOptionsSetup>());

            services.AddTransient<IHalJsonResponseProcessor, HalJsonResponseProcessor>();
        }
    }


    public class HalJsonOptionsSetup(IOptions<JsonOptions> jsonOptions) : IPostConfigureOptions<MvcOptions>
    {
        public void PostConfigure(string? name, MvcOptions options)
        {
            var formatter = new HalJsonOutputFormatter(jsonOptions.Value.JsonSerializerOptions);

            options.OutputFormatters.Insert(0, formatter);
        }
    }
}


