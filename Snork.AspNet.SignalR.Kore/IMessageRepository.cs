using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Messaging;
using Snork.AspNet.SignalR.Kore.Domain;

namespace Snork.AspNet.SignalR.Kore
{
    public interface IMessageRepository
    {
        Task InsertMessage(int streamIndex, IList<Message> messages);
        IList<IMessagePayloadContainer> GetMessages(int streamIndex, long? minPayloadIdExclusive);
        long? GetCurrentStreamPayloadId(int streamIndex);
    }
}