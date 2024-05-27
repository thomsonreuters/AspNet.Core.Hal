using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Buffers;

namespace AspnetCore.Hal.NewtonsoftHalJsonFormatter
{
    internal class HalJsonOptionsSetup(IOptions<MvcNewtonsoftJsonOptions> jsonOptions, ArrayPool<char> charPool) : IPostConfigureOptions<MvcOptions>
    {
        public void PostConfigure(string? name, MvcOptions options)
        {
            var formatter = new HalJsonOutputFormatter(jsonOptions.Value.SerializerSettings, charPool, options, jsonOptions.Value);

            options.OutputFormatters.Insert(0, formatter);
        }
    }
}
