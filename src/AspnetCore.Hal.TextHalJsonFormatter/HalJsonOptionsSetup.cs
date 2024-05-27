using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace AspnetCore.Hal.SystemTextHalJsonFormatter
{
    internal class HalJsonOptionsSetup(IOptions<JsonOptions> jsonOptions) : IPostConfigureOptions<MvcOptions>
    {
        public void PostConfigure(string? name, MvcOptions options)
        {
            var formatter = new HalJsonOutputFormatter(jsonOptions.Value.JsonSerializerOptions);

            options.OutputFormatters.Insert(0, formatter);
        }
    }
}
