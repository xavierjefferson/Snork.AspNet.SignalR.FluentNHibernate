using System;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Snork.FluentNHibernateTools;

namespace Snork.AspNet.SignalR.FluentNHibernate
{
    /// <summary>
    /// </summary>
    public static class DependencyResolverExtensions
    {
        /// <summary>
        ///     Use database as the messaging backplane for scaling out of ASP.NET SignalR applications in a web farm.
        /// </summary>
        /// <param name="resolver">The dependency resolver.</param>
        /// <param name="connectionString">The database connection string.</param>
        /// <param name="providerType"></param>
        /// <param name="serviceProvider"></param>
        /// <returns>The dependency resolver.</returns>
        public static IDependencyResolver UseFluentNHibernate(this IDependencyResolver resolver,
            string connectionString, ProviderTypeEnum providerType, IServiceProvider serviceProvider = null)
        {
            var config = new FNHScaleoutConfiguration(connectionString, providerType);

            return UseFluentNHibernate(resolver, config, serviceProvider);
        }

        /// <summary>
        ///     Use database as the messaging backplane for scaling out of ASP.NET SignalR applications in a web farm.
        /// </summary>
        /// <param name="resolver">The dependency resolver.</param>
        /// <param name="configuration">The SQL scale-out configuration options.</param>
        /// <param name="serviceProvider"></param>
        /// <returns>The dependency resolver.</returns>
        public static IDependencyResolver UseFluentNHibernate(this IDependencyResolver resolver,
            FNHScaleoutConfiguration configuration, IServiceProvider serviceProvider = null)
        {
            if (resolver == null) throw new ArgumentNullException(nameof(resolver));

            if (serviceProvider == null)
            {
                var sc = new ServiceCollection();
                sc.AddLogging();
                serviceProvider = sc.BuildServiceProvider();
            }
            else
            {
                var test = serviceProvider.GetService<ILogger<FNHMessageBus>>();
                if (test == null)
                    throw new ArgumentException(
                        $"Configured service provider must provide logging ({typeof(ILogger).FullName})");
            }

            var bus =
                new FNHMessageBus(resolver, configuration, serviceProvider).WithRepository(c =>
                    new FnhMessageRepository(configuration, serviceProvider.GetService<ILogger<FnhMessageRepository>>()));
            bus.Start();
            resolver.Register(typeof(IMessageBus), () => bus);

            return resolver;
        }
    }
}