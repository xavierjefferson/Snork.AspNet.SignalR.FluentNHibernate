using System;
using Microsoft.AspNet.SignalR.Messaging;
using Snork.FluentNHibernateTools;

namespace Snork.AspNet.SignalR.FluentNHibernate
{
    /// <summary>
    ///     Settings for the database scale-out message bus implementation.
    /// </summary>
    public class FNHScaleoutConfiguration : CoreConfigurationBase
    {
        /// <summary>
        /// </summary>
        /// <param name="connectionString"></param>
        /// <param name="providerType"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public FNHScaleoutConfiguration(string connectionString, ProviderTypeEnum providerType)
        {
            if (string.IsNullOrEmpty(connectionString)) throw new ArgumentNullException(nameof(connectionString));
            ProviderType = providerType;
            ConnectionString = connectionString;
        }


        public string DefaultSchema { get; set; }

        /// <summary>
        /// </summary>
        public ProviderTypeEnum ProviderType { get; }

        /// <summary>
        ///     The database connection string to use.
        /// </summary>
        public string ConnectionString { get; }

        public long MaxTableSize { get; set; } = 10000;
        public long MaxBlockSize { get; set; } = 2500;
    }
}