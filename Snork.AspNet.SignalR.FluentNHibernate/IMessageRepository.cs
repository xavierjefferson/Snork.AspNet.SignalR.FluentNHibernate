using System.Collections.Generic;
using Snork.AspNet.SignalR.FluentNHibernate.Domain;

namespace Snork.AspNet.SignalR.FluentNHibernate
{
    public interface IMessageRepository
    {
        IList<MessagesItemBase> GetMessages(int streamIndex, long? minPayloadIdExclusive);
        long? GetLastPayloadId(int streamIndex);
    }
}