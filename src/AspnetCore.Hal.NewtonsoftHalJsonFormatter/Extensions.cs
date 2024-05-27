using AspnetCore.Hal.Processors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

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
}
