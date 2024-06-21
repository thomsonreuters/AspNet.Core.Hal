using AspnetCore.Hal.Configuration;
using AspnetCore.Hal.Processors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json.Linq;
using System.Text;
using Moq;
using System.Text.Json;
using AspnetCore.Hal.SystemTextHalJsonFormatter.Models;
using Microsoft.AspNetCore.WebUtilities;
using System.Text.Json.Serialization.Metadata;

namespace AspnetCore.Hal.SystemTextHalJsonFormatter.Tests
{
    public class HalJsonOutputFormatterTests
    {
        private readonly HalJsonOutputFormatter _formatter;
        private readonly Mock<IProvideHalTypeConfiguration> _halConfigProviderMock;
        private readonly Mock<IHalJsonResponseProcessor> _halJsonResponseProcessorMock;
        private readonly IServiceCollection _services;
        private readonly IServiceProvider _serviceProvider;

        public HalJsonOutputFormatterTests()
        {

            var _jsonOptions = new JsonSerializerOptions()
            {
                TypeInfoResolver = new DefaultJsonTypeInfoResolver()
            };

            _formatter = new HalJsonOutputFormatter(_jsonOptions);
            _halConfigProviderMock = new Mock<IProvideHalTypeConfiguration>();
            _halJsonResponseProcessorMock = new Mock<IHalJsonResponseProcessor>();
            _services = new ServiceCollection();
            _services.AddSingleton(_halConfigProviderMock.Object);
            _services.AddSingleton(_halJsonResponseProcessorMock.Object);
            _serviceProvider = _services.BuildServiceProvider();
        }

        [Fact]
        public void CanWriteResult_ShouldReturnTrue_WhenAcceptHeaderIsValidAndHalConfigProviderIsAvailable()
        {
            // Arrange
            var context = CreateOutputFormatterCanWriteContext("application/hal+json");

            // Act
            var result = _formatter.CanWriteResult(context);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void CanWriteResult_ShouldReturnFalse_WhenAcceptHeaderIsInvalid()
        {
            // Arrange
            var context = CreateOutputFormatterCanWriteContext("application/json");

            // Act
            var result = _formatter.CanWriteResult(context);

            // Assert
            Assert.False(result);
        }
        [Fact]
        public void CanWriteResult_ShouldReturnTrue_WhenAcceptHeaderIsValidBasedOnQualityFactor()
        {
            // Arrange
            var context = CreateOutputFormatterCanWriteContext("application/hal+json, application/json;q=0.667 , application/import+json; q=0.333");

            // Act
            var result = _formatter.CanWriteResult(context);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void CanWriteResult_ShouldReturnFalse_WhenAcceptHeaderIsNotValidBasedOnQualityFactor()
        {
            // Arrange
            var context = CreateOutputFormatterCanWriteContext("application/hal+json;q=0.667, application/json;q=1.0 , application/import+json; q=0.333");

            // Act
            var result = _formatter.CanWriteResult(context);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task WriteAsync_ShouldSetContentTypeAndCallBaseMethods()
        {
            // Arrange
            var context = CreateOutputFormatterWriteContext(new { Name = "Test" });

            // Act
            await _formatter.WriteAsync(context);

            // Assert
            Assert.Equal("application/hal+json", context.ContentType.ToString());
        }

        [Fact]
        public async Task WriteResponseBodyAsync_ShouldTransformObjectAndCallBaseMethod()
        {
            // Arrange
            var userData = new User { Id = 1, Name = "Test User" };
            var halResponse = new HalUser { Id = 1, Name = "Test User", Links = new[] { new AspnetCore.Hal.SystemTextHalJsonFormatter.Models.Link { Href = "/users/1", Rel = "self" } } };

            var context = CreateOutputFormatterWriteContext(userData);
            var responseStream = new MemoryStream();
            context.HttpContext.Response.Body = responseStream;

            _halJsonResponseProcessorMock
                .Setup(p => p.BuildHypermedia(userData, context.HttpContext))
                .Returns(halResponse);

            // Act
            await _formatter.WriteResponseBodyAsync(context, Encoding.UTF8);

            // Assert
            responseStream.Seek(0, SeekOrigin.Begin);
            var responseBody = new StreamReader(responseStream).ReadToEnd();

            var actualJson = JObject.Parse(responseBody);
            var expectedObject = JObject.FromObject(halResponse);
            Assert.Equal(expectedObject, actualJson);

            _halJsonResponseProcessorMock.Verify(p => p.BuildHypermedia(userData, context.HttpContext), Times.Once);
        }

        private OutputFormatterCanWriteContext CreateOutputFormatterCanWriteContext(string acceptHeader)
        {
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers[HeaderNames.Accept] = new StringValues(acceptHeader);
            httpContext.RequestServices = _serviceProvider;

            return new DefaultOutputFormatterCanWriteContext(
                httpContext,
                (stream, encoding) => new HttpResponseStreamWriter(stream, encoding),
                typeof(object)
            );
        }

        private OutputFormatterWriteContext CreateOutputFormatterWriteContext(object @object)
        {
            var httpContext = new DefaultHttpContext
            {
                RequestServices = _serviceProvider
            };

            return new OutputFormatterWriteContext(
                httpContext,
                (stream, encoding) => new HttpResponseStreamWriter(stream, encoding),
                @object.GetType(),
                @object
            );
        }
    }
}