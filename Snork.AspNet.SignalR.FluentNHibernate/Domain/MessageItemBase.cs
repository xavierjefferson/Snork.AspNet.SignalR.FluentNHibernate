using System;

namespace Snork.AspNet.SignalR.FluentNHibernate.Domain
{
    public abstract class MessageItemBase
    {
        public MessageItemBase()
        {
            InsertedOn = DateTime.UtcNow;
        }

        public virtual long PayloadId { get; set; }
        public virtual DateTime InsertedOn { get; set; }
        public virtual byte[] Payload { get; set; }
    }
}