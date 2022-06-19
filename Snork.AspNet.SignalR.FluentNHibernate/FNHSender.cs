using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Messaging;
using Snork.AspNet.SignalR.FluentNHibernate.Domain;

namespace Snork.AspNet.SignalR.FluentNHibernate
{
    internal class FNHSender<TMessageType, TIdType> where TMessageType : MessagesItemBase, new()
        where TIdType : MessageIdItemBase, new()
    {
     
        private readonly IMessageRepository _messageRepository;
        private readonly int _streamIndex;

        public FNHSender(IMessageRepository messageRepository, int streamIndex)
        {
            this._messageRepository = messageRepository;
            this._streamIndex = streamIndex;
        }

        public Task Send(IList<Message> messages)
        {
            if (messages == null || messages.Count == 0) return TaskAsyncHelper.Empty;
            var count = _messageRepository.InsertMessage<TMessageType, TIdType>(_streamIndex, messages);
            return Task.FromResult(count);
        }
    }
}