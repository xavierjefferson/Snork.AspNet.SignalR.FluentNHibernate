using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Messaging;

namespace Snork.AspNet.SignalR.Kore
{
    internal interface IStream : IDisposable
    {
        int StreamIndex { get; }

        event Action Queried;
        event Action<ulong, ScaleoutMessage> Received;
        event Action<Exception> Faulted;
        Task StartReceiving();
        Task Send(IList<Message> messages);
    }
}