using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Messaging;
using Snork.AspNet.SignalR.FluentNHibernate.Domain;

namespace Snork.AspNet.SignalR.FluentNHibernate
{
    public interface IMessageRepository
    {
        Task InsertMessage(int streamIndex, IList<Message> messages);
        IList<MessagesItemBase> GetMessages(int streamIndex, long? minPayloadIdExclusive);
        long? GetCurrentStreamPayloadId(int streamIndex);
    }
}