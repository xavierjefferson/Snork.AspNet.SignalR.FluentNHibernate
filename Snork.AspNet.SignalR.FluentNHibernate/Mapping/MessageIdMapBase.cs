using System.Text.RegularExpressions;
using FluentNHibernate.Mapping;
using Snork.AspNet.SignalR.FluentNHibernate.Domain;
using Snork.AspNet.SignalR.Kore.Domain;

namespace Snork.AspNet.SignalR.FluentNHibernate.Mapping
{
    public abstract class MessageIdMapBase<T> : ClassMap<T> where T : MessageIdBase
    {
        private static readonly Regex rx = new Regex("(\\d+)", RegexOptions.Compiled);

        public MessageIdMapBase()
        {
            var tableId = rx.Match(typeof(T).Name).Groups[1].Value;
            Table(string.Format("SignalR_Messages_{0}_Id", tableId));
            Id(i => i.RowId).GeneratedBy.Assigned();
            Map(i => i.PayloadId).Not.Nullable();
        }
    }
}