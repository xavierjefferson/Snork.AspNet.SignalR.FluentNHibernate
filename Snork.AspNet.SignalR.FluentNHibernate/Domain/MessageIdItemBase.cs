namespace Snork.AspNet.SignalR.FluentNHibernate.Domain
{
    public abstract class MessageIdItemBase
    {
        public virtual int RowId { get; set; }
        public virtual long PayloadId { get; set; }
    }
}