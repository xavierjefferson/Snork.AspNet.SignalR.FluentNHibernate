using FluentNHibernate.Mapping;
using Snork.AspNet.SignalR.FluentNHibernate.Domain;

namespace Snork.AspNet.SignalR.FluentNHibernate.Mapping
{
    public class SchemaMap : ClassMap<Schema>
    {
        public SchemaMap()
        {
            Table(string.Concat("`", FNHInstaller.SchemaTableName, "`"));
            Id(i => i.SchemaVersion).GeneratedBy.Assigned();
        }
    }
}