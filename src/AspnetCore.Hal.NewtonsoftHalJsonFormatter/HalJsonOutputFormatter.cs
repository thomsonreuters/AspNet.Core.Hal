using AspnetCore.Hal.Configuration;
using AspnetCore.Hal.Processors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json;
using System.Buffers;
using System.Text;

namespace AspnetCore.Hal.NewtonsoftHalJsonFormatter
{
    public class HalJsonOutputFormatter(JsonSerializerSettings serializerSettings, ArrayPool<char> charPool, MvcOptions options, MvcNewtonsoftJsonOptions value) : NewtonsoftJsonOutputFormatter(serializerSettings, charPool, options,value)
    {
        private static readonly MediaTypeHeaderValue AcceptableMimeType = MediaTypeHeaderValue.Parse("application/hal+json");
        private readonly object serializerSettings = serializerSettings;
        private readonly ArrayPool<char> charPool = charPool;
        private readonly MvcOptions options = options;
        private readonly MvcNewtonsoftJsonOptions value = value;

        public override bool CanWriteResult(OutputFormatterCanWriteContext context)
        {
            if (!context.HttpContext.Request.Headers.TryGetValue(HeaderNames.Accept, out var acceptHeader))
            {
                return false;
            }
            var hasSupportedHeader = acceptHeader.Any(headerValue =>
                {
                    if (MediaTypeHeaderValue.TryParse(headerValue, out var parsedHeader))
                    {
                        var hasSupportedHeader = context.HttpContext.Request.Headers.Accept
                            .Select(a => new MediaTypeHeaderValue(new StringSegment(a)))
                            .Any(x => x.IsSubsetOf(AcceptableMimeType));


                        return hasSupportedHeader;
                    }
                    return false;
                });

            var provider = context.HttpContext.RequestServices;
            var cfg = provider.GetService<IProvideHalTypeConfiguration>();

            return hasSupportedHeader && cfg != null;
        }

        public override Task WriteAsync(OutputFormatterWriteContext context)
        {
            context.ContentType = AcceptableMimeType.ToString();
            base.WriteResponseHeaders(context);
            return WriteResponseBodyAsync(context, Encoding.UTF8);
        }

        public new Task WriteResponseBodyAsync(OutputFormatterWriteContext context, Encoding selectedEncoding)
        {
            var provider = context.HttpContext.RequestServices;

            // Transform the object using the HAL configuration
            var builder = provider.GetService<IHalJsonResponseProcessor>();

            var hypermedia = builder != null ? builder.BuildHypermedia(context.Object, context.HttpContext) : context.Object;

            var hypermediaType = builder != null && hypermedia != null ? hypermedia.GetType() : context.ObjectType;

            var newContext = new OutputFormatterWriteContext(context.HttpContext, context.WriterFactory, hypermediaType, hypermedia);

            return base.WriteResponseBodyAsync(newContext, selectedEncoding);
        }
    }
}
