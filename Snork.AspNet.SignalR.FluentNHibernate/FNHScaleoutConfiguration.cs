using System;
using Snork.AspNet.SignalR.Kore;
using Snork.FluentNHibernateTools;

namespace Snork.AspNet.SignalR.FluentNHibernate
{
    /// <summary>
    ///     Settings for the scale-out message bus implementation.
    /// </summary>
    public class FNHScaleoutConfiguration : KoreConfiguration
    {
        /// <summary>
        /// </summary>
        /// <param name="connectionString"></param>
        /// <param name="providerType"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public FNHScaleoutConfiguration(string connectionString, ProviderTypeEnum providerType)
        {
            StreamCount = 10;
            if (string.IsNullOrEmpty(connectionString)) throw new ArgumentNullException(nameof(connectionString));
            ProviderType = providerType;
            ConnectionString = connectionString;
        }


        /// <summary>
        ///     The number of tables to store messages in. Using more tables reduces lock contention and may increase throughput.
        ///     This must be consistent between all nodes in the web farm.
        ///     Defaults to 1, max of 10 for this implementation.
        /// </summary>
        public override int StreamCount
        {
            get => base.StreamCount;
            set
            {
                if (value < 1 || value > 10) throw new ArgumentOutOfRangeException(nameof(StreamCount));
                base.StreamCount = value;
            }
        }

        /// <summary>
        /// </summary>
        public string DefaultSchema { get; set; }

        /// <summary>
        /// </summary>
        public ProviderTypeEnum ProviderType { get; }

        /// <summary>
        ///     The database connection string to use.
        /// </summary>
        public string ConnectionString { get; }

        /// <summary>
        /// </summary>
        public long MaxTableSize { get; set; } = 10000;

        /// <summary>
        /// </summary>
        public long MaxBlockSize { get; set; } = 2500;

        public TimeSpan DeadlockInterval { get; set; } = TimeSpan.FromSeconds(2.5);
    }
}