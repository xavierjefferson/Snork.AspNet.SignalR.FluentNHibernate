using System;
using Microsoft.AspNet.SignalR.Messaging;

namespace Snork.AspNet.SignalR.Kore
{
    public class KoreConfiguration : ScaleoutConfiguration
    {
        private int _streamCount = 1;

        /// <summary>
        ///     The number of streams to store messages in.
        ///     Defaults to 1 
        /// </summary>
        public virtual int StreamCount
        {
            get => _streamCount;
            set
            {
                if (value < 1) throw new ArgumentOutOfRangeException(nameof(StreamCount));
                _streamCount = value;
            }
        }

        public TimeSpan ReceivePollInterval { get; set; } = TimeSpan.FromSeconds(5);
    }
}