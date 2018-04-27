using System;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Messaging;
using Snork.FluentNHibernateTools;

namespace Snork.AspNet.SignalR.FluentNHibernate
{
    /// <summary>
    /// 
    /// </summary>
    public static class DependencyResolverExtensions
    {
        /// <summary>
        ///     Use database as the messaging backplane for scaling out of ASP.NET SignalR applications in a web farm.
        /// </summary>
        /// <param name="resolver">The dependency resolver.</param>
        /// <param name="connectionString">The database connection string.</param>
        /// <param name="providerType"></param>
        /// <returns>The dependency resolver.</returns>
        public static IDependencyResolver UseFluentNHibernate(this IDependencyResolver resolver,
            string connectionString, ProviderTypeEnum providerType)
        {
            var config = new FNHScaleoutConfiguration(connectionString, providerType);

            return UseFluentNHibernate(resolver, config);
        }

        /// <summary>
        ///     Use database as the messaging backplane for scaling out of ASP.NET SignalR applications in a web farm.
        /// </summary>
        /// <param name="resolver">The dependency resolver.</param>
        /// <param name="configuration">The SQL scale-out configuration options.</param>
        /// <returns>The dependency resolver.</returns>
        public static IDependencyResolver UseFluentNHibernate(this IDependencyResolver resolver,
            FNHScaleoutConfiguration configuration)
        {
            if (resolver == null)
            {
                throw new ArgumentNullException("resolver");
            }


            var bus = new Lazy<FNHMessageBus>(() => { return new FNHMessageBus(resolver, configuration); });
            resolver.Register(typeof(IMessageBus), () => bus.Value);

            return resolver;
        }
    }
}