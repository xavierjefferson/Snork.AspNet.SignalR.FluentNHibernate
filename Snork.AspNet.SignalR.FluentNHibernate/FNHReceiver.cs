using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Messaging;
using NHibernate;
using NHibernate.Linq;
using NHibernate.Util;
using Snork.AspNet.SignalR.FluentNHibernate.Domain;
using Snork.AspNet.SignalR.FluentNHibernate.Infrastructure;
using Timer = System.Timers.Timer;

namespace Snork.AspNet.SignalR.FluentNHibernate
{
    internal class FNHReceiver<TMessageType, TIdType> : IDisposable
        where TMessageType : MessageItemBase where TIdType : MessageIdItemBase

    {
        private const int interval = 5000;

        private readonly ISessionFactory _sessionFactory;

        private readonly TraceSource _trace;
        private readonly string _tracePrefix;
        private readonly Mutex mutex = new Mutex(false);


        private long? _lastPayloadId;

        private Timer _timer;

        public FNHReceiver(ISessionFactory sessionFactory, TraceSource traceSource,
            string tracePrefix)
        {
            _sessionFactory = sessionFactory;
            _tracePrefix = tracePrefix;
            _trace = traceSource;


            Queried += () => { };
            Received += (_, __) => { };
            Faulted += _ => { };
        }

        public void Dispose()
        {
            if (_timer != null)
            {
                _timer.Stop();
                _timer = null;
            }
        }

        public event Action Queried;

        public event Action<ulong, ScaleoutMessage> Received;

        public event Action<Exception> Faulted;

        public Task StartReceiving()
        {
            var tcs = new DispatchingTaskCompletionSource<object>();

            ThreadPool.QueueUserWorkItem(Receive, tcs);

            return tcs.Task;
        }

        private void Receive(object state)
        {
            if (!_lastPayloadId.HasValue)
            {
                try
                {
                    using (var session = _sessionFactory.OpenStatelessSession())
                    {
                        _lastPayloadId = (long?) session.Query<TIdType>().Select(i => i.PayloadId).FirstOrNull();
                    }
                    //_lastPayloadId = (long?)lastPayloadIdOperation.ExecuteScalar();
                    Queried();

                    _trace.TraceVerbose("{0}SqlReceiver started, initial payload id={1}", _tracePrefix, _lastPayloadId);


                    // Complete the StartReceiving task as we've successfully initialized the payload ID
                    //tcs.TrySetResult(null);
                }
                catch (Exception ex)
                {
                    _trace.TraceError("{0}SqlReceiver error starting: {1}", _tracePrefix, ex);

                    //tcs.TrySetException(ex);
                    return;
                }
            }
            TryReceive();
            if (_timer == null)
            {
                _timer = new Timer(interval);
                _timer.Elapsed += (a, b) => { TryReceive(); };
                _timer.Start();
            }
        }

        private void TryReceive()
        {
            _trace.TraceVerbose("Trying receive");
            if (mutex.WaitOne(interval))
            {
                _trace.TraceVerbose("Not locked out");
                try
                {
                    using (var session = _sessionFactory.OpenStatelessSession())
                    {
                        var messages = session.Query<TMessageType>()
                            .OrderBy(i => i.PayloadId)
                            .Where(i => i.PayloadId > _lastPayloadId);

                        Queried();

                        if (messages.Any())
                        {
                            _trace.TraceVerbose("found messages");
                        }
                        foreach (var record in messages)
                        {
                            var id = record.PayloadId;
                            var message = FNHPayload.FromBytes(record);

                            _trace.TraceVerbose("{0}SqlReceiver last payload ID={1}, new payload ID={2}", _tracePrefix,
                                _lastPayloadId, id);

                            if (id > _lastPayloadId + 1)
                            {
                                _trace.TraceError(
                                    "{0}Missed message(s) from database. Expected payload ID {1} but got {2}.",
                                    _tracePrefix, _lastPayloadId + 1, id);
                            }
                            else if (id <= _lastPayloadId)
                            {
                                _trace.TraceInformation(
                                    "{0}Duplicate message(s) or payload ID reset from database. Last payload ID {1}, this payload ID {2}",
                                    _tracePrefix, _lastPayloadId, id);
                            }

                            _lastPayloadId = id;

                            _trace.TraceVerbose("{0}Payload {1} containing {2} message(s) received", _tracePrefix, id,
                                message.Messages.Count);

                            Received((ulong) id, message);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Faulted(ex);
                }
                finally
                {
                    mutex.ReleaseMutex();
                }
            }
            else
            {
                _trace.TraceVerbose("Receive is locked out");
            }
        }
    }
}