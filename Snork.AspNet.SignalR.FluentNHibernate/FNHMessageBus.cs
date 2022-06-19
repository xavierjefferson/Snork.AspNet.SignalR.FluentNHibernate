using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Messaging;
using Microsoft.AspNet.SignalR.Tracing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Snork.AspNet.SignalR.FluentNHibernate
{
    /// <summary>
    ///     Uses database tables to scale-out SignalR applications in web farms.
    /// </summary>
    public class FNHMessageBus : ScaleoutMessageBus
    {
        private readonly CoreConfigurationBase _configuration;
        private readonly ILogger<FNHMessageBus> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly List<IStream> _streams = new List<IStream>();

        // This is the specific TraceSource for the SqlMessageBus. The Trace property from the base type traces from ScaleoutMessageBus
        // so we generally don't want to use that from here.
        private readonly TraceSource _traceSource;

        private IMessageRepository _messageRepository;

        internal FNHMessageBus(IDependencyResolver resolver, CoreConfigurationBase configuration,
            IServiceProvider serviceProvider)
            : base(resolver, configuration)
        {
            _serviceProvider = serviceProvider;
            _logger = serviceProvider.GetService<ILogger<FNHMessageBus>>();
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            var traceManager = resolver.Resolve<ITraceManager>();
            _traceSource = traceManager[typeof(FNHMessageBus).FullName];
            _traceSource.Listeners.Add(new LoggerListener(_logger));
        }

        protected override int StreamCount => _configuration.StreamCount;

        /// <summary>
        /// </summary>
        /// <param name="repoFunc"></param>
        /// <returns></returns>
        public FNHMessageBus WithRepository(Func<ScaleoutConfiguration, IMessageRepository> repoFunc)
        {
            _messageRepository = repoFunc(_configuration);
            return this;
        }

        /// <summary>
        /// 
        /// </summary>
        public void Start()
        {
            ThreadPool.QueueUserWorkItem(Initialize);
        }

        /// <summary>
        /// </summary>
        /// <param name="streamIndex"></param>
        /// <param name="messages"></param>
        /// <returns></returns>
        protected override async Task Send(int streamIndex, IList<Message> messages)
        {
            await _streams[streamIndex].Send(messages);
        }

        /// <summary>
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            _logger.LogInformation("Message bus disposing, disposing streams");

            foreach (var stream in _streams) stream.Dispose();


            base.Dispose(disposing);
        }

        private void Initialize(object state)
        {
            // NOTE: Called from a ThreadPool thread
            _logger.LogInformation("Message bus initializing.");

            for (var streamIndex = 0; streamIndex < _configuration.StreamCount; streamIndex++)
            {
                var stream = new FNHStream(streamIndex, _serviceProvider, _configuration, _messageRepository);
                stream.Queried += () => Open(stream.StreamIndex);
                stream.Faulted += ex => OnError(stream.StreamIndex, ex);
                stream.Received += (id, messages) => OnReceived(stream.StreamIndex, id, messages);
                StartReceiving(stream);
                _streams.Add(stream);
            }
        }

        private void StartReceiving(IStream stream)
        {
            try
            {
                stream.StartReceiving()
                    // Open the stream once receiving has started
                    .Then(() => Open(stream.StreamIndex))
                    // Starting the receive loop failed
                    .Catch(ex =>
                        {
                            _logger.LogError(ex, "Issue in StartReceiving");
                            OnError(stream.StreamIndex, ex);

                            // Try again in a little bit
                            Thread.Sleep(2000);
                            StartReceiving(stream);
                        },
                        _traceSource);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Issue 2 in StartReceiving");
                throw;
            }
        }

        private class LoggerListener : TraceListener
        {
            private readonly ILogger<FNHMessageBus> _logger;

            public LoggerListener(ILogger<FNHMessageBus> logger)
            {
                _logger = logger;
            }

            public override void Fail(string message)
            {
                _logger.LogError(message);
            }

            public override void Fail(string message, string detailMessage)
            {
                _logger.LogError($"Message: {message}\r\nDetail:{detailMessage}");
            }

            public override void Write(string message)
            {
                _logger.LogInformation(message);
            }

            public override void WriteLine(string message)
            {
                _logger.LogInformation(message);
            }
        }
    }
}