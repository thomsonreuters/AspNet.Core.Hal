using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Text.Json;
using Nancy.Hal.Processors;


namespace Core.Hal.Example
{
    /// <summary>
    /// Represents the filter attribute that applies HAL document on the response JSON.
    /// </summary>
    public class SupportsHalAttribute : ResultFilterAttribute
    {
        private readonly IHalJsonResponseProcessor _halJsonResponseProcessorCore;
        private readonly JsonSerializerOptions _serializerSettings;
        public SupportsHalAttribute(IHalJsonResponseProcessor halJsonResponseProcessorCore, JsonSerializerOptions serializerSettings)
        {
            _halJsonResponseProcessorCore = halJsonResponseProcessorCore;
            _serializerSettings = serializerSettings;
        }
        public override async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
        {
            var controllerActionDescriptor = context.ActionDescriptor as ControllerActionDescriptor;

            if (context.Result is ObjectResult objectResult && objectResult.Value != null && IsHalJsonRequest(context.HttpContext.Request))
            {
                if ((objectResult.Value is not string))
                {
                    //(objectResult.Value is IEnumerable enumerableResult)
                    await _halJsonResponseProcessorCore.Process(_serializerSettings, objectResult.Value, context.HttpContext);

                }
            }
        }


        private static bool IsHalJsonRequest(HttpRequest request)
        {         // Check if the request Accept header contains "application/hal+json"
            return request.Headers.ContainsKey("Accept") && request.Headers.Accept.ToString().Contains("application/hal+json");
        }
    }
        
}
