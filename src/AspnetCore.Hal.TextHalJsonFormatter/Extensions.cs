using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using AspnetCore.Hal.Processors;
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
                var formatter = new HalJsonOutputFormatter(options);

                o.OutputFormatters.Insert(0, formatter);
            });

            services.AddTransient<IHalJsonResponseProcessor, HalJsonResponseProcessor>();


        }
    }
}


