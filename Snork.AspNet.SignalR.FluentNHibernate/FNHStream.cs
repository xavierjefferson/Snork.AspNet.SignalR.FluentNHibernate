using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Snork.AspNet.SignalR.FluentNHibernate
{
    internal class FNHStream : IStream
    {
        private readonly ILogger<FNHStream> _logger;
        private readonly IMessageRepository _messageRepository;
        private readonly FNHReceiver _receiver;
        private readonly string _tracePrefix;

        public FNHStream(int streamIndex, IServiceProvider serviceProvider, CoreConfigurationBase configuration,
            IMessageRepository messageRepository)
        {
            _messageRepository = messageRepository;
            _logger = serviceProvider.GetService<ILogger<FNHStream>>();
            StreamIndex = streamIndex;

            _tracePrefix = string.Format(CultureInfo.InvariantCulture, "Stream {0} : ", StreamIndex);

            Queried += () => { };
            Received += (_, __) => { };
            Faulted += _ => { };

            _receiver = new FNHReceiver(streamIndex, _tracePrefix, serviceProvider.GetService<ILogger<FNHReceiver>>(),
                configuration, messageRepository);
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

        public async Task Send(IList<Message> messages)
        {
            _logger.LogDebug("{0}Saving payload with {1} messages(s) to repository", _tracePrefix, messages.Count,
                StreamIndex);
            await _messageRepository.InsertMessage(StreamIndex, messages);
        }
    }
}