namespace Snork.AspNet.SignalR.FluentNHibernate.Domain
{
    public class MessageIdBase
    {
        public virtual int RowId { get; set; }
        public virtual long PayloadId { get; set; }
    }
}