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
}


