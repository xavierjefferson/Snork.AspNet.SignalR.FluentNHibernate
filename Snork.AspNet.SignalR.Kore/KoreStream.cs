using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Snork.AspNet.SignalR.Kore
{
    internal class KoreStream : IStream
    {
        private readonly ILogger<KoreStream> _logger;
        private readonly IMessageRepository _messageRepository;
        private readonly KoreReceiver _receiver;
        private readonly string _logPrefix;

        public KoreStream(int streamIndex, IServiceProvider serviceProvider, KoreConfiguration configuration,
            IMessageRepository messageRepository)
        {
            _messageRepository = messageRepository;
            _logger = serviceProvider.GetService<ILogger<KoreStream>>();
            StreamIndex = streamIndex;

            _logPrefix = StreamHelper.GetStreamLogPrefix(streamIndex);

            Queried += () => { };
            Received += (_, __) => { };
            Faulted += _ => { };

            _receiver = new KoreReceiver(streamIndex, _logPrefix, serviceProvider.GetService<ILogger<KoreReceiver>>(),
                configuration, messageRepository);
            _receiver.Queried += () => Queried();
            _receiver.Faulted += ex => Faulted(ex);
            _receiver.Received += (id, messages) => Received(id, messages);
        }

        public int StreamIndex { get; }

        public void Dispose()
        {
            _logger.LogDebug($"{_logPrefix}Disposing");

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
            _logger.LogDebug($"{_logPrefix}Saving payload with {1} messages(s) to repository");
            await _messageRepository.InsertMessage(StreamIndex, messages);
        }
    }
}