using System;
using System.Collections.Generic;
using Microsoft.AspNet.SignalR.Messaging;
using Snork.AspNet.SignalR.Kore.Domain;

namespace Snork.AspNet.SignalR.Kore
{
    /// <summary>
    /// 
    /// </summary>
    public static class StreamHelper
    {
        /// <summary>
        /// </summary>
        /// <param name="messages"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static byte[] ToBytes(IList<Message> messages)
        {
            if (messages == null) throw new ArgumentNullException(nameof(messages));

            var message = new ScaleoutMessage(messages);
            return message.ToBytes();
        }

        /// <summary>
        /// </summary>
        /// <param name="record"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static ScaleoutMessage FromBytes<T>(T record) where T : IMessagePayloadContainer
        {
            var message = ScaleoutMessage.FromBytes(record.Payload);

            return message;
        }

        public static string GetStreamLogPrefix(int streamIndex)
        {
            return $"Stream {streamIndex} : ";
        }
    }
}