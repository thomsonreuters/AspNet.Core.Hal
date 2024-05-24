using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.AspNetCore.Http;

namespace AspnetCore.Hal.Configuration
{
    public interface IHalTypeConfiguration
    {
        IEnumerable<Link> LinksFor(object model, HttpContext context);
        IEnumerable<IEmbeddedResourceInfo> EmbedsFor(object model, HttpContext context);
        IEnumerable<string> Ignored();
    }

    public class AggregatingHalTypeConfiguration : IHalTypeConfiguration
    {
        private readonly IEnumerable<IHalTypeConfiguration> _delegates;

        public AggregatingHalTypeConfiguration(IEnumerable<IHalTypeConfiguration> delegates)
        {
            _delegates = delegates.Where(el => el != null);
        }

        public IEnumerable<Link> LinksFor(object model, HttpContext context)
        {
            return _delegates.SelectMany(c => c.LinksFor(model, context));
        }

        public IEnumerable<IEmbeddedResourceInfo> EmbedsFor(object model, HttpContext context)
        {
            return _delegates.SelectMany(c => c.EmbedsFor(model, context));
        }

        public IEnumerable<string> Ignored()
        {
            return _delegates.SelectMany(c => c.Ignored());
        }
    }

    public class HalTypeConfiguration<T> : IHalTypeConfiguration
    {
        private readonly IList<Func<T, HttpContext, IEmbeddedResourceInfo>> embedded = new List<Func<T, HttpContext, IEmbeddedResourceInfo>>();
        private readonly IList<Func<T, HttpContext, Link>> links = new List<Func<T, HttpContext, Link>>();
        private readonly IList<string> ignoredProperties = new List<string>();
        private readonly object syncRoot = new object();

        public IEnumerable<Link> LinksFor(object obj, HttpContext context)
        {
            var model = (T)obj;
            return links.Select(x => x(model, context)).Where(x => x != null);
        }

        public IEnumerable<IEmbeddedResourceInfo> EmbedsFor(object obj, HttpContext context)
        {
            var model = (T)obj;
            return embedded.Select(x => x(model, context)).Where(x => x != null);
        }

        public IEnumerable<string> Ignored()
        {
            return ignoredProperties;
        }

        private void AddLinkFn(Func<T, HttpContext, Link> getter)
        {
            lock (syncRoot)
            {
                links.Add(getter);
            }
        }

        public HalTypeConfiguration<T> Links(Link link)
        {
            AddLinkFn((_, __) => link);
            return this;
        }

        public HalTypeConfiguration<T> Links(string rel, string href, string title = null)
        {
            return Links(new Link(rel, href, title));
        }

        public HalTypeConfiguration<T> Links(Func<T, Link> linkGetter)
        {
            AddLinkFn((o, ctx) => linkGetter(o));
            return this;
        }

        public HalTypeConfiguration<T> Links(Func<T, HttpContext, Link> linkGetter)
        {
            AddLinkFn(linkGetter);
            return this;
        }

        public HalTypeConfiguration<T> Links(Func<T, Link> linkGetter, Func<T, bool> predicate)
        {
            Links((model, ctx) => predicate(model) ? linkGetter(model) : null);
            return this;
        }

        public HalTypeConfiguration<T> Links(Func<T, Link> linkGetter, Func<T, HttpContext, bool> predicate)
        {
            Links((model, ctx) => predicate(model, ctx) ? linkGetter(model) : null);
            return this;
        }

        public HalTypeConfiguration<T> Links(Func<T, HttpContext, Link> linkGetter, Func<T, bool> predicate)
        {
            Links((model, ctx) => predicate(model) ? linkGetter(model, ctx) : null);
            return this;
        }

        public HalTypeConfiguration<T> Links(Func<T, HttpContext, Link> linkGetter, Func<T, HttpContext, bool> predicate)
        {
            Links((model, ctx) => predicate(model, ctx) ? linkGetter(model, ctx) : null);
            return this;
        }

        private HalTypeConfiguration<T> AddEmbeds(IEmbeddedResourceInfo embed)
        {
            return AddEmbeds((model, context) => embed);
        }

        private HalTypeConfiguration<T> AddEmbeds(Func<T, IEmbeddedResourceInfo> predicate)
        {
            return AddEmbeds((model, context) => predicate(model));
        }

        private HalTypeConfiguration<T> AddEmbeds(Func<T, HttpContext, IEmbeddedResourceInfo> predicate)
        {
            lock (syncRoot)
            {
                embedded.Add(predicate);
            }
            return this;
        }

        public HalTypeConfiguration<T> Embeds(Expression<Func<T, dynamic>> property)
        {
            var propName = property.ExtractPropertyInfo().Name;
            return AddEmbeds(new EmbeddedResourceInfo<T>(propName.ToCamelCaseString(), propName, property.Compile()));
        }

        public HalTypeConfiguration<T> Embeds(Expression<Func<T, dynamic>> property, Func<T, bool> predicate)
        {
            var propName = property.ExtractPropertyInfo().Name;
            return AddEmbeds(model => predicate(model) ? new EmbeddedResourceInfo<T>(propName.ToCamelCaseString(), propName, property.Compile()) : null);
        }

        public HalTypeConfiguration<T> Embeds(Expression<Func<T, dynamic>> property, Func<T, HttpContext, bool> predicate)
        {
            var propName = property.ExtractPropertyInfo().Name;
            return AddEmbeds((model, ctx) => predicate(model, ctx) ? new EmbeddedResourceInfo<T>(propName.ToCamelCaseString(), propName, property.Compile()) : null);
        }

        public HalTypeConfiguration<T> Embeds(string rel, Expression<Func<T, dynamic>> property, Func<T, bool> predicate)
        {
            return AddEmbeds(model => predicate(model) ? new EmbeddedResourceInfo<T>(rel, property.ExtractPropertyInfo().Name, property.Compile()) : null);
        }

        public HalTypeConfiguration<T> Embeds(string rel, Expression<Func<T, dynamic>> property, Func<T, HttpContext, bool> predicate)
        {
            return AddEmbeds((model, ctx) => predicate(model, ctx) ? new EmbeddedResourceInfo<T>(rel, property.ExtractPropertyInfo().Name, property.Compile()) : null);
        }

        public HalTypeConfiguration<T> Embeds(string rel, Expression<Func<T, dynamic>> property)
        {
            return AddEmbeds(new EmbeddedResourceInfo<T>(rel, property.ExtractPropertyInfo().Name, property.Compile()));
        }

        public HalTypeConfiguration<T> Projects<TEmbedded>(string rel, Expression<Func<T, TEmbedded>> property, Func<TEmbedded, dynamic> projection)
        {
            var getter = property.Compile();
            return AddEmbeds(new EmbeddedResourceInfo<T>(rel, property.ExtractPropertyInfo().Name, model => projection(getter(model))));
        }

        public HalTypeConfiguration<T> Projects<TEmbedded>(Expression<Func<T, TEmbedded>> property, Func<TEmbedded, dynamic> projection)
        {
            var getter = property.Compile();
            var propName = property.ExtractPropertyInfo().Name;
            return AddEmbeds(new EmbeddedResourceInfo<T>(propName.ToCamelCaseString(), propName, model => projection(getter(model))));
        }

        public HalTypeConfiguration<T> Ignores(Expression<Func<T, dynamic>> property)
        {
            var propName = property.ExtractPropertyInfo().Name;
            return AddIgnores(propName);
        }

        private HalTypeConfiguration<T> AddIgnores(string propertyName)
        {
            lock (syncRoot)
            {
                ignoredProperties.Add(propertyName);
            }
            return this;
        }
    }
}