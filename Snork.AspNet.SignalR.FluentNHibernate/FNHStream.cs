using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Messaging;
using NHibernate;
using Snork.AspNet.SignalR.FluentNHibernate.Domain;

namespace Snork.AspNet.SignalR.FluentNHibernate
{
    internal class FNHStream<TMessageType, TIdType> : IStream where TMessageType : MessageItemBase, new()
        where TIdType : MessageIdItemBase, new()
    {
        private readonly FNHReceiver<TMessageType, TIdType> _receiver;
        private readonly FNHSender<TMessageType, TIdType> _sender;
        private readonly TraceSource _trace;
        private readonly string _tracePrefix;

        public FNHStream(int streamIndex, ISessionFactory sessionFactory,
            TraceSource traceSource)
        {
            StreamIndex = streamIndex;
            _trace = traceSource;
            _tracePrefix = string.Format(CultureInfo.InvariantCulture, "Stream {0} : ", StreamIndex);

            Queried += () => { };
            Received += (_, __) => { };
            Faulted += _ => { };
            _sender = new FNHSender<TMessageType, TIdType>(sessionFactory, _trace);
            _receiver = new FNHReceiver<TMessageType, TIdType>(sessionFactory, _trace, _tracePrefix);
            _receiver.Queried += () => Queried();
            _receiver.Faulted += ex => Faulted(ex);
            _receiver.Received += (id, messages) => Received(id, messages);
        }

        public int StreamIndex { get; }

        public void Dispose()
        {
            _trace.TraceInformation("{0}Disposing stream {1}", _tracePrefix, StreamIndex);

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
            _trace.TraceVerbose("{0}Saving payload with {1} messages(s) to database", _tracePrefix, messages.Count,
                StreamIndex);

            return _sender.Send(messages);
        }
    }
}