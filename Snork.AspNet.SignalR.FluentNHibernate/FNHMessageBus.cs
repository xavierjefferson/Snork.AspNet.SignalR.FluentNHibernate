using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Messaging;
using Microsoft.AspNet.SignalR.Tracing;
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

        private readonly List<IStream> _streams = new List<IStream>();

        // This is the specific TraceSource for the SqlMessageBus. The Trace property from the base type traces from ScaleoutMessageBus
        // so we generally don't want to use that from here.
        private readonly TraceSource _trace;

        internal FNHMessageBus(IDependencyResolver resolver, FNHScaleoutConfiguration configuration)
            : base(resolver, configuration)
        {
            _configuration = configuration ?? throw new ArgumentNullException("configuration");

            var traceManager = resolver.Resolve<ITraceManager>();
            _trace = traceManager[typeof(FNHMessageBus).FullName];

            ThreadPool.QueueUserWorkItem(Initialize);
        }

        protected override int StreamCount
        {
            get { return _configuration.TableCount; }
        }

        protected override Task Send(int streamIndex, IList<Message> messages)
        {
            return _streams[streamIndex].Send(messages);
        }

        protected override void Dispose(bool disposing)
        {
            _trace.TraceInformation("Message bus disposing, disposing streams");

            for (var i = 0; i < _streams.Count; i++)
            {
                _streams[i].Dispose();
            }

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
                _trace.TraceInformation("Message bus initializing.");

                while (true)
                {
                    try
                    {
                        var installer = new FNHInstaller(sessionFactory, _trace);
                        installer.Install();
                        break;
                    }
                    catch (Exception ex)
                    {
                        _trace.TraceError("Error trying to install database objects, trying again in 2 seconds: {0}",
                            ex);

                        // Try again in a little bit
                        Thread.Sleep(2000);
                    }
                }

                var collection = new List<IStream>
                {
                    new FNHStream<Messages_0, Messages_0_Id>(0, sessionFactory, _trace),
                    new FNHStream<Messages_1, Messages_1_Id>(1, sessionFactory, _trace),
                    new FNHStream<Messages_2, Messages_2_Id>(2, sessionFactory, _trace),
                    new FNHStream<Messages_3, Messages_3_Id>(3, sessionFactory, _trace),
                    new FNHStream<Messages_4, Messages_4_Id>(4, sessionFactory, _trace),
                    new FNHStream<Messages_5, Messages_5_Id>(5, sessionFactory, _trace),
                    new FNHStream<Messages_6, Messages_6_Id>(6, sessionFactory, _trace),
                    new FNHStream<Messages_7, Messages_7_Id>(7, sessionFactory, _trace),
                    new FNHStream<Messages_8, Messages_8_Id>(8, sessionFactory, _trace),
                    new FNHStream<Messages_9, Messages_9_Id>(9, sessionFactory, _trace)
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
                _trace.TraceError("Issue", ex);
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
                            _trace.TraceError("Issue in StartReceiving", ex);
                            OnError(stream.StreamIndex, ex);

                            // Try again in a little bit
                            Thread.Sleep(2000);
                            StartReceiving(stream);
                        },
                        _trace);
            }
            catch (Exception ex)
            {
                _trace.TraceError("Issue 2 in StartReceiving", ex);
                throw;
            }
        }
    }
}