using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using AspnetCore.Hal.Processors;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Formatters;
using System.Linq;

namespace AspnetCore.Hal.SystemTextHalJsonFormatter
{
    public static class Extensions
    {
        public static void AddHalSupport(this IServiceCollection services)
        {
            
            services.Configure<MvcOptions>(o =>
            {
                var options = o.OutputFormatters.OfType<SystemTextJsonOutputFormatter>().First().SerializerOptions;

                options.WriteIndented = true;
                options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                options.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
                options.PropertyNameCaseInsensitive = true;
                var formatter = new HalJsonOutputFormatter(options);

                o.OutputFormatters.Insert(0, formatter);
            });

            services.AddTransient<IHalJsonResponseProcessor, HalJsonResponseProcessor>();


        }
    }
}


