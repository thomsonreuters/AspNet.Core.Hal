using AspnetCore.Hal.Processors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace AspnetCore.Hal.NewtonsoftHalJsonFormatter
{
    public static class Extensions
    {
        public static void AddHalSupport(this IServiceCollection services)
        {
            services.Configure<MvcOptions>(o =>
            {
                DefaultContractResolver contractResolver = new DefaultContractResolver
                {
                    NamingStrategy = new CamelCaseNamingStrategy
                    {
                        ProcessDictionaryKeys = true
                    }
                };

                var jsonSerializerSettings = new JsonSerializerSettings
                {
                    ContractResolver = contractResolver
                };

                // Create and add the custom HAL JSON output formatter
                var formatter = new HalJsonOutputFormatter(jsonSerializerSettings);
                o.OutputFormatters.Insert(0, formatter);

            });
            services.AddTransient<IHalJsonResponseProcessor, HalJsonResponseProcessor>();


        }
    }
}
