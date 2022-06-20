using System.Text.RegularExpressions;
using FluentNHibernate.Mapping;
using Snork.AspNet.SignalR.FluentNHibernate.Domain;
using Snork.AspNet.SignalR.Kore.Domain;

namespace Snork.AspNet.SignalR.FluentNHibernate.Mapping
{
    public abstract class MessagePayloadMapBase<T> : ClassMap<T> where T : MessagePayloadBase
    {
        private static readonly Regex rx = new Regex("(\\d+)", RegexOptions.Compiled);

        public MessagePayloadMapBase()
        {
            var tableId = rx.Match(typeof(T).Name).Groups[1].Value;
            Table(string.Format("SignalR_Messages_{0}", tableId));
            Id(i => i.PayloadId).GeneratedBy.Assigned();
            Map(i => i.InsertedOn).Column("InsertedOn").Not.Nullable();
            Map(i => i.Payload).Length(4096).Not.Nullable();
        }
    }
}