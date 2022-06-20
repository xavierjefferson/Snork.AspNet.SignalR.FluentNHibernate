using System;
using Snork.AspNet.SignalR.Kore.Domain;

namespace Snork.AspNet.SignalR.FluentNHibernate.Domain
{
    /// <summary>
    /// 
    /// </summary>
    public class MessagePayloadBase : IMessagePayloadContainer
    {
        /// <summary>
        /// 
        /// </summary>
        public MessagePayloadBase()
        {
            InsertedOn = DateTime.UtcNow;
        }

        public virtual long PayloadId { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public virtual DateTime InsertedOn { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public virtual byte[] Payload { get; set; }
    }
}