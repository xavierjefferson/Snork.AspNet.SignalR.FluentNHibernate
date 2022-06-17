using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Messaging;
using Microsoft.AspNet.SignalR.Tracing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Snork.AspNet.SignalR.FluentNHibernate.Domain;
using Snork.AspNet.SignalR.FluentNHibernate.Mapping;
using Snork.FluentNHibernateTools;

namespace Snork.AspNet.SignalR.FluentNHibernate
{
    /// <summary>
    ///     Uses database tables to scale-out SignalR applications in web farms.
    /// </summary>
    public class FNHMessageBus : ScaleoutMessageBus
    {
        private readonly FNHScaleoutConfiguration _configuration;
        private readonly ILogger<FNHMessageBus> _logger;

        private readonly IServiceProvider _serviceProvider;

        private readonly List<IStream> _streams = new List<IStream>();

        // This is the specific TraceSource for the SqlMessageBus. The Trace property from the base type traces from ScaleoutMessageBus
        // so we generally don't want to use that from here.
        private readonly TraceSource _trace;

        internal FNHMessageBus(IDependencyResolver resolver, FNHScaleoutConfiguration configuration,
            IServiceProvider serviceProvider)
            : base(resolver, configuration)
        {
            _serviceProvider = serviceProvider;
            _logger = serviceProvider.GetService<ILogger<FNHMessageBus>>();
            _configuration = configuration ?? throw new ArgumentNullException("configuration");
            var traceManager = resolver.Resolve<ITraceManager>();
            _trace = traceManager[typeof(FNHMessageBus).FullName];
            _trace.Listeners.Add(new LoggerListener(_logger));
            ThreadPool.QueueUserWorkItem(Initialize);
        }

        protected override int StreamCount => _configuration.TableCount;

        /// <summary>
        /// </summary>
        /// <param name="streamIndex"></param>
        /// <param name="messages"></param>
        /// <returns></returns>
        protected override Task Send(int streamIndex, IList<Message> messages)
        {
            return _streams[streamIndex].Send(messages);
        }

        /// <summary>
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            _logger.LogInformation("Message bus disposing, disposing streams");

            for (var i = 0; i < _streams.Count; i++) _streams[i].Dispose();

            base.Dispose(disposing);
        }


        private void Initialize(object state)
        {
            try
            {
                var sessionFactoryInfo = SessionFactoryBuilder.GetFromAssemblyOf<Message0Map>(
                    _configuration.ProviderType, _configuration.ConnectionString,
                    new FluentNHibernatePersistenceBuilderOptions {DefaultSchema = _configuration.DefaultSchema});
                var sessionFactory = sessionFactoryInfo.SessionFactory;
                // NOTE: Called from a ThreadPool thread
                _logger.LogInformation("Message bus initializing.");

                var collection = new List<IStream>
                {
                    new FNHStream<Messages_0, Messages_0_Id>(0, sessionFactory, _serviceProvider),
                    new FNHStream<Messages_1, Messages_1_Id>(1, sessionFactory, _serviceProvider),
                    new FNHStream<Messages_2, Messages_2_Id>(2, sessionFactory, _serviceProvider),
                    new FNHStream<Messages_3, Messages_3_Id>(3, sessionFactory, _serviceProvider),
                    new FNHStream<Messages_4, Messages_4_Id>(4, sessionFactory, _serviceProvider),
                    new FNHStream<Messages_5, Messages_5_Id>(5, sessionFactory, _serviceProvider),
                    new FNHStream<Messages_6, Messages_6_Id>(6, sessionFactory, _serviceProvider),
                    new FNHStream<Messages_7, Messages_7_Id>(7, sessionFactory, _serviceProvider),
                    new FNHStream<Messages_8, Messages_8_Id>(8, sessionFactory, _serviceProvider),
                    new FNHStream<Messages_9, Messages_9_Id>(9, sessionFactory, _serviceProvider)
                };
                _streams.AddRange(collection.Take(_configuration.TableCount));
                foreach (var stream in _streams)
                {
                    stream.Queried += () => Open(stream.StreamIndex);
                    stream.Faulted += ex => OnError(stream.StreamIndex, ex);
                    stream.Received += (id, messages) => OnReceived(stream.StreamIndex, id, messages);
                    StartReceiving(stream);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Issue");
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
                        _trace);
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
                this._logger = logger;
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