using System;
using Microsoft.AspNet.SignalR.Messaging;
using Snork.FluentNHibernateTools;

namespace Snork.AspNet.SignalR.FluentNHibernate
{
    /// <summary>
    ///     Settings for the database scale-out message bus implementation.
    /// </summary>
    public class FNHScaleoutConfiguration : ScaleoutConfiguration
    {
        private int _tableCount = 10;

        /// <summary>
        /// </summary>
        /// <param name="connectionString"></param>
        /// <param name="providerType"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public FNHScaleoutConfiguration(string connectionString, ProviderTypeEnum providerType)
        {
            if (string.IsNullOrEmpty(connectionString)) throw new ArgumentNullException("connectionString");
            ProviderType = providerType;
            ConnectionString = connectionString;
        }

        /// <summary>
        ///     The number of tables to store messages in. Using more tables reduces lock contention and may increase throughput.
        ///     This must be consistent between all nodes in the web farm.
        ///     Defaults to 1, max of 10 for this implementation.
        /// </summary>
        public int TableCount
        {
            get => _tableCount;
            set
            {
                if (value < 1 || value > 10) throw new ArgumentOutOfRangeException("value");
                _tableCount = value;
            }
        }


        public string DefaultSchema { get; set; }

        /// <summary>
        /// </summary>
        public ProviderTypeEnum ProviderType { get; }

        /// <summary>
        ///     The database connection string to use.
        /// </summary>
        public string ConnectionString { get; }
    }
}