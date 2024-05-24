using AspnetCore.Hal.Configuration;
using AspnetCore.Hal.Processors;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json;
using System.Text;

namespace AspnetCore.Hal.NewtonsoftHalJsonFormatter
{
    public class HalJsonOutputFormatter : TextOutputFormatter
    {
        private static readonly MediaTypeHeaderValue AcceptableMimeType = MediaTypeHeaderValue.Parse("application/hal+json; charset=utf-8");

        public HalJsonOutputFormatter(JsonSerializerSettings serializerSettings)
        {
            SerializerSettings = serializerSettings;

            SupportedMediaTypes.Add(AcceptableMimeType);
            SupportedEncodings.Add(Encoding.UTF8);
        }

        public JsonSerializerSettings SerializerSettings { get; }

        public override bool CanWriteResult(OutputFormatterCanWriteContext context)
        {
            if (context.HttpContext.Request.Headers.TryGetValue(HeaderNames.Accept, out var acceptHeader))
            {
                var hasSupportedHeader = acceptHeader.Any(headerValue =>
                {
                    if (MediaTypeHeaderValue.TryParse(headerValue, out var parsedHeader))
                    {
                        return AcceptableMimeType.SubType.Equals(parsedHeader.SubType);
                    }
                    return false;
                });

                var provider = context.HttpContext.RequestServices;
                var cfg = provider.GetService<IProvideHalTypeConfiguration>();

                return hasSupportedHeader && cfg != null;
            }
            return false;
        }

        public override async Task WriteResponseBodyAsync(OutputFormatterWriteContext context, Encoding selectedEncoding)
        {
            var provider = context.HttpContext.RequestServices;

            // Transform the object using the HAL configuration
            var builder = provider.GetService<IHalJsonResponseProcessor>();

            var hypermedia = builder != null ? builder.BuildHypermedia(context.Object, context.HttpContext) : context.Object;
            var hypermediaType = builder != null && hypermedia != null ? hypermedia.GetType() : context.ObjectType;

            var newContext = new OutputFormatterWriteContext(context.HttpContext, context.WriterFactory, hypermediaType, hypermedia);

            context.HttpContext.Response.ContentType = AcceptableMimeType.ToString();

            await using var writer = context.WriterFactory(context.HttpContext.Response.Body, selectedEncoding);
            var json = JsonConvert.SerializeObject(hypermedia, SerializerSettings);
            await writer.WriteAsync(json);
        }

        protected override bool CanWriteType(System.Type type)
        {
            return true;
        }
    }
}
