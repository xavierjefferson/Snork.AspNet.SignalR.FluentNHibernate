using System.Text.RegularExpressions;
using FluentNHibernate.Mapping;
using Snork.AspNet.SignalR.FluentNHibernate.Domain;

namespace Snork.AspNet.SignalR.FluentNHibernate.Mapping
{
    public abstract class MessageIdItemMap<T> : ClassMap<T> where T : MessageIdItemBase
    {
        private static readonly Regex rx = new Regex("(\\d+)", RegexOptions.Compiled);

        public MessageIdItemMap()
        {
            var tableId = rx.Match(typeof(T).Name).Groups[1].Value;
            Table(string.Format("SignalR_Messages_{0}_Id", tableId));
            Id(i => i.RowId).GeneratedBy.Assigned();
            Map(i => i.PayloadId).Not.Nullable();
        }
    }
}