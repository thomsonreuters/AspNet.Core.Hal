using AspnetCore.Hal.Processors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using System.Buffers;

namespace AspnetCore.Hal.NewtonsoftHalJsonFormatter
{
    public static class Extensions
    {
        public static void AddHalSupport(this IServiceCollection services)
        {
            services.TryAddEnumerable(ServiceDescriptor.Transient<IPostConfigureOptions<MvcOptions>, HalJsonOptionsSetup>());
            services.AddTransient<IHalJsonResponseProcessor, HalJsonResponseProcessor>();
        }
    }

    public class HalJsonOptionsSetup(IOptions<MvcNewtonsoftJsonOptions> jsonOptions, ArrayPool<char> charPool) : IPostConfigureOptions<MvcOptions>
    {
        public void PostConfigure(string? name, MvcOptions options)
        {
            var formatter = new HalJsonOutputFormatter(jsonOptions.Value.SerializerSettings, charPool, options, jsonOptions.Value);

            options.OutputFormatters.Insert(0, formatter);
        }
    }
}
