using System.Linq;
using System.Text;
using System.Text.Json;
using AspnetCore.Hal.Configuration;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.DependencyInjection;
using AspnetCore.Hal.Processors;
using Microsoft.Net.Http.Headers;
using Microsoft.Extensions.Primitives;
using System.Net.Http.Headers;


namespace AspnetCore.Hal.SystemTextHalJsonFormatter;

internal class HalJsonOutputFormatter : SystemTextJsonOutputFormatter
{
    private static readonly Microsoft.Net.Http.Headers.MediaTypeHeaderValue AcceptableMimeType = Microsoft.Net.Http.Headers.MediaTypeHeaderValue.Parse("application/hal+json");

    public HalJsonOutputFormatter(JsonSerializerOptions jsonSerializerOptions) : base(jsonSerializerOptions)
    {
        SupportedMediaTypes.Clear();

        SupportedMediaTypes.Add(AcceptableMimeType);
    }

    public override bool CanWriteResult(OutputFormatterCanWriteContext context)
    {
        if (!context.HttpContext.Request.Headers.TryGetValue(HeaderNames.Accept, out var acceptHeader))
        {
            return false;
        }

        var acceptHeaders = context.HttpContext.Request.Headers["Accept"].ToString().Split(',')
                .Select(h => MediaTypeWithQualityHeaderValue.Parse(h.Trim()))
                .OrderByDescending(h => h.Quality ?? 1.0)  // Sort by quality factor in descending order
                .ToList();


        // Check if the top value matches the acceptable MIME type
        var qualityHeader = acceptHeaders.FirstOrDefault();

        var hasSupportedHeader = qualityHeader != null &&
                                 Microsoft.Net.Http.Headers.MediaTypeHeaderValue.Parse(new StringSegment(qualityHeader.MediaType))
                                 .IsSubsetOf(Microsoft.Net.Http.Headers.MediaTypeHeaderValue.Parse(new StringSegment(AcceptableMimeType.ToString())));

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