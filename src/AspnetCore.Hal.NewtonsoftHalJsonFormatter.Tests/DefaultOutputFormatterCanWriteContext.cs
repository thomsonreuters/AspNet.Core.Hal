using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Formatters;
using System.Text;

namespace AspnetCore.Hal.NewtonsoftHalJsonFormatter.Tests
{
    public class DefaultOutputFormatterCanWriteContext : OutputFormatterCanWriteContext
    {
        public DefaultOutputFormatterCanWriteContext(HttpContext httpContext, Func<Stream, Encoding, TextWriter> writerFactory, Type objectType)
            : base(httpContext)
        {
        }
    }
}
