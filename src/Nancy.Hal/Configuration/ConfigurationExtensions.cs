using Microsoft.AspNetCore.Http;
using System;

namespace AspnetCore.Hal.Configuration
{
    public static class ConfigurationExtensions
    {
        /// <summary>
        /// Defines the string-key to use to store the local HalConfiguration in the Items-dictionary of the HttpContext.
        /// </summary>
        public static string HttpContextKey { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Request a HalTypeConfiguration instance from the local HalConfiguration
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="context"></param>
        /// <returns></returns>
        public static HalTypeConfiguration<T> LocalHalConfigFor<T>(this HttpContext context)
        {
            context.EnsureHalConfiguration();

            return ((HalConfiguration)context.Items[HttpContextKey]).For<T>();
        }

        /// <summary>
        /// Retrieve the local HalConfiguration from the NancyContext
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public static IProvideHalTypeConfiguration LocalHalConfig(this HttpContext context)
        {
            context.EnsureHalConfiguration();

            return (IProvideHalTypeConfiguration)context.Items[HttpContextKey];
        }

        /// <summary>
        /// Internal use.
        /// Ensures that the current NancyContext stores a HalConfiguration instance
        /// </summary>
        /// <param name="context"></param>
        private static void EnsureHalConfiguration(this HttpContext context)
        {
            bool contextStoresHalConfig = context.Items.ContainsKey(HttpContextKey);

            if (!contextStoresHalConfig)
            {
                context.Items[HttpContextKey] = new HalConfiguration();
            }
        }
    }
}
