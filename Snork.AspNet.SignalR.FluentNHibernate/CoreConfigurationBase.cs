using System;
using Microsoft.AspNet.SignalR.Messaging;

namespace Snork.AspNet.SignalR.FluentNHibernate
{
    public class CoreConfigurationBase : ScaleoutConfiguration
    {
        private int _streamCount = 1;

        /// <summary>
        ///     The number of tables to store messages in. Using more tables reduces lock contention and may increase throughput.
        ///     This must be consistent between all nodes in the web farm.
        ///     Defaults to 1, max of 10 for this implementation.
        /// </summary>
        public int StreamCount
        {
            get => _streamCount;
            set
            {
                if (value < 1 || value > 10) throw new ArgumentOutOfRangeException(nameof(StreamCount));
                _streamCount = value;
            }
        }

        public TimeSpan ReceivePollInterval { get; set; } = TimeSpan.FromSeconds(5);
    }
}