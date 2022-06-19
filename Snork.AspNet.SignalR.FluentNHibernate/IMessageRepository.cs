using System.Collections.Generic;
using Microsoft.AspNet.SignalR.Messaging;
using Snork.AspNet.SignalR.FluentNHibernate.Domain;

namespace Snork.AspNet.SignalR.FluentNHibernate
{
    public interface IMessageRepository
    {
        int InsertMessage<TMessageType, TIdType>(int streamIndex, IList<Message> messages)
            where TMessageType : MessagesItemBase, new()
            where TIdType : MessageIdItemBase, new();
   
           IList<MessagesItemBase> GetMessages(int streamIndex, long? minPayloadIdExclusive);
        long? GetLastPayloadId(int streamIndex);
    }
}