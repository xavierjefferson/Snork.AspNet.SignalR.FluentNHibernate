using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NHibernate;
using Snork.AspNet.SignalR.FluentNHibernate.Domain;

namespace Snork.AspNet.SignalR.FluentNHibernate
{
    internal class FNHStream<TMessageType, TIdType> : IStream where TMessageType : MessagesItemBase, new()
        where TIdType : MessageIdItemBase, new()
    {
        private readonly ILogger<FNHStream<TMessageType, TIdType>> _logger;
        private readonly FNHReceiver<TMessageType, TIdType> _receiver;
        private readonly FNHSender<TMessageType, TIdType> _sender;

        private readonly string _tracePrefix;

        public FNHStream(int streamIndex, ISessionFactory sessionFactory, IServiceProvider serviceProvider, FNHScaleoutConfiguration configuration)
        {
            _logger = serviceProvider.GetService<ILogger<FNHStream<TMessageType, TIdType>>>();
            StreamIndex = streamIndex;

            _tracePrefix = string.Format(CultureInfo.InvariantCulture, "Stream {0} : ", StreamIndex);

            Queried += () => { };
            Received += (_, __) => { };
            Faulted += _ => { };
            _sender = new FNHSender<TMessageType, TIdType>(sessionFactory,
                serviceProvider.GetService<ILogger<FNHSender<TMessageType, TIdType>>>());
            _receiver = new FNHReceiver<TMessageType, TIdType>(sessionFactory, _tracePrefix,
                serviceProvider.GetService<ILogger<FNHReceiver<TMessageType, TIdType>>>(), configuration);
            _receiver.Queried += () => Queried();
            _receiver.Faulted += ex => Faulted(ex);
            _receiver.Received += (id, messages) => Received(id, messages);
        }

        public int StreamIndex { get; }

        public void Dispose()
        {
            _logger.LogInformation("{0}Disposing stream {1}", _tracePrefix, StreamIndex);

            _receiver.Dispose();
        }

        public event Action Queried;

        public event Action<ulong, ScaleoutMessage> Received;

        public event Action<Exception> Faulted;

        public Task StartReceiving()
        {
            return _receiver.StartReceiving();
        }

        public Task Send(IList<Message> messages)
        {
            _logger.LogDebug("{0}Saving payload with {1} messages(s) to database", _tracePrefix, messages.Count,
                StreamIndex);

            return _sender.Send(messages);
        }
    }
}