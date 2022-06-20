using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Messaging;
using Microsoft.Extensions.Logging;
using Snork.AspNet.SignalR.Kore.Infrastructure;
using Timer = System.Timers.Timer;

namespace Snork.AspNet.SignalR.Kore
{
    internal class KoreReceiver : IDisposable


    {
        private readonly KoreConfiguration _configuration;

        private readonly ILogger<KoreReceiver> _logger;


        private readonly string _logPrefix;
        private readonly IMessageRepository _messageRepository;
        private readonly Mutex _mutex = new Mutex(false);
        private readonly int _streamIndex;

        private long? _lastPayloadId;

        private Timer _timer;

        public KoreReceiver(int streamIndex, string logPrefix, ILogger<KoreReceiver> logger,
            KoreConfiguration configuration, IMessageRepository messageRepository)
        {
            _messageRepository = messageRepository;
            _streamIndex = streamIndex;
            _logPrefix = logPrefix;
            _logger = logger;
            _configuration = configuration;

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
                try
                {
                    _lastPayloadId = _messageRepository.GetCurrentStreamPayloadId(_streamIndex);

                    Queried();

                    _logger.LogDebug(
                        $"{_logPrefix}{nameof(KoreReceiver)} started, initial payload id={_lastPayloadId}");


                    // Complete the StartReceiving task as we've successfully initialized the payload ID
                    //tcs.TrySetResult(null);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"{_logPrefix}{nameof(KoreReceiver)} error starting");

                    //tcs.TrySetException(ex);
                    return;
                }

            TryReceive();
            if (_timer == null)
            {
                _timer = new Timer(_configuration.ReceivePollInterval.TotalMilliseconds);
                _timer.Elapsed += (a, b) => { TryReceive(); };
                _timer.Start();
            }
        }

        private void TryReceive()
        {
            _logger.LogDebug($"{_logPrefix}Trying receive");
            if (_mutex.WaitOne(_configuration.ReceivePollInterval))
            {
                try
                {
                    var messagesItems = _messageRepository.GetMessages(_streamIndex, _lastPayloadId);

                    Queried();

                    if (messagesItems.Any())
                    {
                        _logger.LogDebug($"{_logPrefix}found {messagesItems.Count} messages");


                        foreach (var messageItem in messagesItems)
                        {
                            var id = messageItem.PayloadId;
                            var scaleoutMessage = StreamHelper.FromBytes(messageItem);

                            _logger.LogDebug(
                                $"{_logPrefix}{nameof(KoreReceiver)} last payload ID={_lastPayloadId}, new payload ID={id}");

                            if (id > _lastPayloadId + 1)
                                _logger.LogError(
                                    $"{_logPrefix}Missed message(s) from database. Expected payload ID {_lastPayloadId + 1} but got {id}.");
                            else if (id <= _lastPayloadId)
                                _logger.LogInformation(
                                    "{0}Duplicate message(s) or payload ID reset from database. Last payload ID {1}, this payload ID {2}",
                                    _logPrefix, _lastPayloadId, id);

                            _lastPayloadId = id;

                            if (_logger.IsEnabled(LogLevel.Debug))
                            {
                                _logger.LogDebug("{0}Payload {1} containing {2} message(s) received", _logPrefix, id,
                                    scaleoutMessage.Messages.Count);

                                foreach (var message in scaleoutMessage.Messages)
                                    if (message.Value.Array != null)
                                    {
                                        var valueArray = message.Value.Array;
                                        _logger.LogDebug(Encoding.UTF8.GetString(valueArray));
                                    }
                            }

                            Received((ulong) id, scaleoutMessage);
                        }
                    }
                    else
                    {
                        _logger.LogDebug($"{_logPrefix}No messages found");
                    }
                }
                catch (Exception ex)
                {
                    Faulted(ex);
                }
                finally
                {
                    _mutex.ReleaseMutex();
                }
            }
            else
            {
                _logger.LogDebug($"{_logPrefix}Receive is locked out");
            }
        }
    }
}