﻿using System.Linq;
using System.Text;
using System.Text.Json;
using AspnetCore.Hal.Configuration;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.DependencyInjection;
using AspnetCore.Hal.Processors;
using Microsoft.Net.Http.Headers;

namespace AspnetCore.Hal.SystemTextHalJsonFormatter;

public class HalJsonOutputFormatter : SystemTextJsonOutputFormatter
{
    private static MediaTypeHeaderValue AcceptableMimeType = MediaTypeHeaderValue.Parse("application/hal+json; charset=utf-8");

    public HalJsonOutputFormatter(JsonSerializerOptions jsonSerializerOptions) : base(jsonSerializerOptions)
    {
        SupportedMediaTypes.Clear();

        SupportedMediaTypes.Add(AcceptableMimeType);
    }

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