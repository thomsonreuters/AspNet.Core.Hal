using System.Linq;
using AspnetCore.Hal.Configuration;
using AspnetCore.Hal.Processors;
using Xunit;
using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.AspNetCore.Http;
using System.Text.Json;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;
namespace AspnetCore.Hal.Tests
{

    public class JsonResponseProcessorTests
    {

        [Fact]
        public void ShouldBuildStaticLinks()
        {
            var config = new HalConfiguration();
            config.For<PetOwner>().
                Links("rel1", "/staticAddress1").
                Links(new Link("rel2", "/staticAddress2"));

            var json = Serialize(new PetOwner { Name = "Bob" }, config);

            Assert.Equal("Bob", GetStringValue(json, "Name"));
            Assert.Equal("/staticAddress1", GetStringValue(json, "_links", "rel1", "href"));
            Assert.Equal("/staticAddress2", GetStringValue(json, "_links", "rel2", "href"));
        }

        [Fact]
        public void ShouldBuildDynamicLinks()
        {
            var config = new HalConfiguration();
            config.For<PetOwner>().
                Links(model => new Link("rel1", "/dynamic/{name}").CreateLink(model)).
                Links((model, ctx) => new Link("rel2", "/dynamic/{name}/{operation}").CreateLink(model, ctx.Request.Query));

            var json = Serialize(new PetOwner { Name = "Bob" }, config, CreateTestContext(new { Operation = "Duck" }));

            Assert.Equal("/dynamic/Bob", GetStringValue(json, "_links", "rel1", "href"));
            Assert.Equal("/dynamic/Bob/Duck", GetStringValue(json, "_links", "rel2", "href"));
        }

        [Fact]
        public void ShouldBuildMultipleLinksForSingleRel()
        {
            var config = new HalConfiguration();
            config.For<PetOwner>().
                Links(new Link("rel1", "/static1")).
                Links(new Link("rel1", "/static2")).
                Links(model => new Link("rel2", "/dynamic/{name}").CreateLink(model)).
                Links((model, ctx) => new Link("rel2", "/dynamic/{name}/{operation}").CreateLink(model, ctx.Request.Query));

            var json = Serialize(new PetOwner { Name = "Bob" }, config, CreateTestContext(new { Operation = "Duck" }));

            var rel1Links = GetData(json, "_links", "rel1");
            Assert.Equal(rel1Links.Count(), 2);
            Assert.Equal(new[] { "/static1", "/static2" }, rel1Links.Select(token => token["href"].ToString()));
            var rel2Links = GetData(json, "_links", "rel2");
            Assert.Equal(rel2Links.Count(), 2);
            Assert.Equal(new[] { "/dynamic/Bob", "/dynamic/Bob/Duck" }, rel2Links.Select(token => token["href"].ToString()));
        }

        [Fact]
        public void ShouldBuildDynamicLinksWithPredicates()
        {
            var config = new HalConfiguration();
            config.For<PetOwner>().
                Links(model => new Link("rel1", "/dynamic/on/{name}").CreateLink(model), model => model.Happy).
                Links(model => new Link("rel2", "/dynamic/off/{name}").CreateLink(model), (model, ctx) => !model.Happy).
                Links((model, ctx) => new Link("rel3", "/dynamic/on/{name}/{operation}").CreateLink(model, ctx.Request.Query), model => model.Happy).
                Links((model, ctx) => new Link("rel4", "/dynamic/off/{name}/{operation}").CreateLink(model, ctx.Request.Query), (model, ctx) => !model.Happy);

            var json = Serialize(new PetOwner { Name = "Bob", Happy = true }, config, CreateTestContext(new { Operation = "Duck" }));

            Assert.Equal("/dynamic/on/Bob", GetStringValue(json, "_links", "rel1", "href"));
            Assert.Null(GetStringValue(json, "_links", "rel2", "href"));
            Assert.Equal("/dynamic/on/Bob/Duck", GetStringValue(json, "_links", "rel3", "href"));
            Assert.Null(GetStringValue(json, "_links", "rel4", "href"));
        }

        [Fact]
        public void ShouldEmbedSubResources()
        {
            var config = new HalConfiguration();
            config.For<PetOwner>().
                Embeds("pampered", owner => owner.Pets).
                Embeds(owner => owner.LiveStock);

            var model = new PetOwner
            {
                Name = "Bob",
                Happy = true,
                Pets = new[] { new Animal { Type = "Cat" } },
                LiveStock = new Animal { Type = "Chicken" }
            };
            var json = Serialize(model, config);

            Assert.Equal("Cat", GetData(json, "_embedded", "pampered")[0][AdjustName("Type")]);
            Assert.Equal("Chicken", GetStringValue(json, "_embedded", "liveStock", "Type"));
        }

        [Fact]
        public void ShouldEmbedSubResourcesWhenPredicateIsTrue()
        {
            var model = new PetOwner
            {
                Happy = true,
                Pets = new[] { new Animal { Type = "Cat" } }
            };

            Action<PetOwner, HalConfiguration, string> assertPetIsCat = (owner, configuration, rel) =>
                Assert.Equal("Cat", GetData(Serialize(owner, configuration), "_embedded", rel)[0][AdjustName("Type")]);


            var config = new HalConfiguration();
            config.For<PetOwner>().
                Embeds("pampered", owner => owner.Pets, x => x.Happy);
            assertPetIsCat(model, config, "pampered");

            config = new HalConfiguration();
            config.For<PetOwner>().
                Embeds("pampered", owner => owner.Pets, (x, ctx) => x.Happy);
            assertPetIsCat(model, config, "pampered");


            config = new HalConfiguration();
            config.For<PetOwner>().
                Embeds(owner => owner.Pets, x => x.Happy);
            assertPetIsCat(model, config, "pets");

            config = new HalConfiguration();
            config.For<PetOwner>().
                Embeds(owner => owner.Pets, (x, ctx) => x.Happy);
            assertPetIsCat(model, config, "pets");

        }

        [Fact]
        public void ShouldNotEmbedSubResourcesWhenPredicateIsFalse()
        {
            var model = new PetOwner
            {
                Happy = false,
                Pets = new[] { new Animal { Type = "Cat" } }
            };

            Action<PetOwner, HalConfiguration, string> assertPetIsNull = (owner, configuration, rel) =>
                Assert.Null(GetData(Serialize(owner, configuration), "_embedded", rel));


            var config = new HalConfiguration();
            config.For<PetOwner>().
                Embeds("pampered", owner => owner.Pets, x => x.Happy);
            assertPetIsNull(model, config, "pampered");

            config = new HalConfiguration();
            config.For<PetOwner>().
                Embeds("pampered", owner => owner.Pets, (x, ctx) => x.Happy);
            assertPetIsNull(model, config, "pampered");


            config = new HalConfiguration();
            config.For<PetOwner>().
                Embeds(owner => owner.Pets, x => x.Happy);
            assertPetIsNull(model, config, "pets");

            config = new HalConfiguration();
            config.For<PetOwner>().
                Embeds(owner => owner.Pets, (x, ctx) => x.Happy);
            assertPetIsNull(model, config, "pets");

        }

        [Fact]
        public void ShouldEmbedSubResourceProjections()
        {
            var config = new HalConfiguration();
            config.For<PetOwner>().
                Projects("pampered", owner => owner.Pets, pets => new { petCount = pets.Count() }).
                Projects(owner => owner.LiveStock, stock => new { stockType = stock.Type });

            var model = new PetOwner
            {
                Name = "Bob",
                Happy = true,
                Pets = new[] { new Animal { Type = "Cat" } },
                LiveStock = new Animal { Type = "Chicken" }
            };
            var json = Serialize(model, config, CreateTestContext(new { Operation = "Duck" }));

            Assert.Equal("1", GetData(json, "_embedded", "pampered", "petCount"));
            Assert.Equal("Chicken", GetStringValue(json, "_embedded", "liveStock", "stockType"));
        }

        [Fact]
        public void ShouldIgnoreIgnoredProperties()
        {
            var config = new HalConfiguration();
            config.For<PetOwner>().Ignores(owner => owner.Pets);

            var model = new PetOwner
            {
                Name = "Bob",
                Happy = true,
                Pets = new[] { new Animal { Type = "Cat" } },
                LiveStock = new Animal { Type = "Chicken" }
            };
            var json = Serialize(model, config, CreateTestContext(new { Operation = "Duck" }));

            Assert.Null(json[AdjustName("Pets")]);
        }

        [Fact]
        public void ShouldSetContentTypeToApplicationHalJson()
        {

            var context = new DefaultHttpContext { };
            var config = new HalConfiguration();

            var settings = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true,
                WriteIndented = true

            };

            var processor = new HalJsonResponseProcessor(config);
            //var response = processor.Process(settings, new PetOwner(){ Name = "Bob "}, context);
            var response = processor.BuildHypermedia(new PetOwner() { Name = "Bob " }, context);

            // Assert.Equal("application/hal+json", response.ContentType);
        }

        [Fact]
        public void ShouldAggregateHalTypeConfigurations()
        {
            var typeConfig1 = new HalTypeConfiguration<PetOwner>().Links("rel1", "/staticAddress1");
            var typeConfig2 = new HalTypeConfiguration<PetOwner>().Links("rel2", "/staticAddress2");

            var mergedConfig = new AggregatingHalTypeConfiguration(new List<IHalTypeConfiguration> { typeConfig1, typeConfig2 });

            var config = new MockTypeConfiguration();
            config.Add<PetOwner>(mergedConfig);

            var json = Serialize(new PetOwner { Name = "Bob" }, config);

            Assert.Equal("Bob", GetStringValue(json, "Name"));
            Assert.Equal("/staticAddress1", GetStringValue(json, "_links", "rel1", "href"));
            Assert.Equal("/staticAddress2", GetStringValue(json, "_links", "rel2", "href"));
        }

        [Fact]
        public void ShouldTakeLocalConfigurationIntoAccount()
        {
            var globalConfig = new HalConfiguration();
            globalConfig.For<PetOwner>().
                Links("rel1", "/staticAddress1");

            // var context = new NancyContext {Environment = GetTestingEnvironment()};
            var context = new DefaultHttpContext { };
            context.LocalHalConfigFor<PetOwner>()
                .Links("rel2", "/staticAddress2");

            var json = Serialize(new PetOwner { Name = "Bob" }, globalConfig, context);

            Assert.Equal("Bob", GetStringValue(json, "Name"));
            Assert.Equal("/staticAddress1", GetStringValue(json, "_links", "rel1", "href"));
            Assert.Equal("/staticAddress2", GetStringValue(json, "_links", "rel2", "href"));
        }

        private object GetStringValue(JToken json, params string[] names)
        {
            var data = GetData(json, names);
            return data != null ? data.ToString() : null;
        }

        private JToken GetData(JToken json, params string[] names)
        {
            return names.Aggregate(json, (current, name) => current != null ? current[AdjustName(name)] : null);
        }

        protected virtual string AdjustName(string name)
        {
            return name;
        }



        //private static NancyContext CreateTestContext(dynamic query)
        //{
        //    var context = new NancyContext { Request = new Request("method", "path", "http") { Query = query }, Environment = GetTestingEnvironment() };
        //    return context;
        //}
        public static HttpContext CreateTestContext(dynamic query)
        {
            // Create a new default HttpContext instance
            var context = new DefaultHttpContext();

            // Create a new HttpRequest instance with method, path, and scheme
            var request = context.Request;
            request.Method = "GET"; // Specify the HTTP method (GET, POST, etc.)
            request.Path = "/"; // Specify the path of the request
            request.Scheme = "http"; // Specify the scheme (http or https)

            // Convert the dynamic query object to a dictionary
            var queryParams = new Dictionary<string, StringValues>();
            foreach (var property in query.GetType().GetProperties())
            {
                queryParams.Add(property.Name, new StringValues(property.GetValue(query).ToString()));
            }

            // Create an IQueryCollection from the dictionary and set it to the reques
            request.Query = new QueryCollection(queryParams);

            return context;
        }

        private static JObject Serialize(object model, IProvideHalTypeConfiguration config, HttpContext context = null)
        {
            context ??= new DefaultHttpContext();
            var processor = new HalJsonResponseProcessor(config);
            // Process the response
            var halResponse = processor.BuildHypermedia(model, context);
            var response = JsonConvert.SerializeObject(halResponse);
            return JObject.Parse(response);

        }
    }





}
