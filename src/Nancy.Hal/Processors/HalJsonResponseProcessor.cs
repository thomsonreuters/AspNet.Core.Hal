using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using System.Text.Json;
using System.Threading.Tasks;
using AspnetCore.Hal.Configuration;

namespace AspnetCore.Hal.Processors
{
    public class HalJsonResponseProcessor : IHalJsonResponseProcessor
    {
        private const string ContentType = "application/hal+json";
        private readonly IProvideHalTypeConfiguration _configuration;

        public HalJsonResponseProcessor(IProvideHalTypeConfiguration configuration)
        {
            _configuration = configuration;
        }

        //public async Task<HttpContext> Process(JsonSerializerOptions jsonSerializerSettings, dynamic model, HttpContext context)
        //{
        //    var halResponse = BuildHypermedia(model, context);
        //    string jsonResponse = JsonSerializer.Serialize(halResponse, jsonSerializerSettings);
        //    context.Response.ContentType = ContentType;
        //    context.Response.StatusCode = StatusCodes.Status200OK;
        //    await context.Response.WriteAsync(jsonResponse);
        //    return context;
        //}


        public dynamic BuildHypermedia(object model, HttpContext context)
        {
            if (model == null) return null;

            if (model is IEnumerable)
            {
                //how to handle a collection at the root resource level?
                return ((IEnumerable)model).Cast<object>().Select(x => BuildHypermedia(x, context));
            }

            IDictionary<string, object> halModel = model.ToDynamic();
            var globalTypeConfig = _configuration.GetTypeConfiguration(model.GetType());
            var localTypeConfig = context.LocalHalConfig().GetTypeConfiguration(model.GetType());

            var typeConfig = new AggregatingHalTypeConfiguration(new List<IHalTypeConfiguration> { globalTypeConfig, localTypeConfig });

            var links = typeConfig.LinksFor(model, context).ToArray();
            if (links.Any())
                halModel["_links"] = links.GroupBy(l => l.Rel).ToDictionary(grp => grp.Key, grp => BuildDynamicLinksOrLink(grp));

            var embeddedResources = typeConfig.EmbedsFor(model, context).ToArray();
            if (embeddedResources.Any())
            {
                // Remove original objects from the model (if they exist)
                foreach (var embedded in embeddedResources)
                    halModel.Remove(embedded.OriginalPropertyName);
                halModel["_embedded"] = embeddedResources.ToDictionary(info => info.Rel, info => BuildHypermedia(info.GetEmbeddedResource(model), context));
            }

            var ignoredProperties = typeConfig.Ignored().ToArray();
            if (ignoredProperties.Any())
            {
                //remove ignored properties from the output
                foreach (var ignored in ignoredProperties) halModel.Remove(ignored);
            }
            return halModel;
        }

        private static dynamic BuildDynamicLinksOrLink(IEnumerable<Link> grp)
        {
            return grp.Count() > 1 ? grp.Select(l => BuildDynamicLink(l)) : BuildDynamicLink(grp.First());
        }

        private static dynamic BuildDynamicLink(Link link)
        {
            dynamic dynamicLink = new ExpandoObject();
            dynamicLink.href = link.Href;
            if (link.IsTemplated) dynamicLink.templated = true;
            if (!string.IsNullOrEmpty(link.Title)) dynamicLink.title = link.Title;
            return dynamicLink;
        }
        
    }

    public interface IHalJsonResponseProcessor
    {
        //Task<HttpContext> Process(JsonSerializerOptions jsonSerializerSettings, dynamic model, HttpContext context);


        dynamic BuildHypermedia(object model, HttpContext context);
    }
}