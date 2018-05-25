using System;
using System.Collections.Generic;
using Microsoft.AspNet.SignalR.Messaging;
using Snork.AspNet.SignalR.FluentNHibernate.Domain;

namespace Snork.AspNet.SignalR.FluentNHibernate
{
    public static class FNHPayload
    {
        public static byte[] ToBytes(IList<Message> messages)
        {
            if (messages == null)
            {
                throw new ArgumentNullException("messages");
            }

            var message = new ScaleoutMessage(messages);
            return message.ToBytes();
        }

        public static ScaleoutMessage FromBytes<T>(T record) where T : MessagesItemBase
        {
            var message = ScaleoutMessage.FromBytes(record.Payload);

            return message;
        }
    }
}