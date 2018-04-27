using System.Diagnostics;
using System.Linq;
using NHibernate;
using NHibernate.Linq;
using Snork.AspNet.SignalR.FluentNHibernate.Domain;

namespace Snork.AspNet.SignalR.FluentNHibernate
{
    internal class FNHInstaller
    {
        private const int SchemaVersion = 1;
        public const string SchemaTableName = "SignalR_Schema";


        private readonly ISessionFactory _sessionFactory;
        private readonly TraceSource _trace;

        public FNHInstaller(ISessionFactory sessionFactoryInfo, TraceSource traceSource)
        {
            _sessionFactory = sessionFactoryInfo;

            _trace = traceSource;
        }


        public void Install()
        {
            _trace.TraceInformation("Start installing SignalR database objects");

            using (var session = _sessionFactory.OpenStatelessSession())
            {
                using (var tx = session.BeginTransaction())
                {
                    if (!session.Query<Schema>().Any())
                    {
                        session.Insert(new Schema {SchemaVersion = SchemaVersion});
                    }
                    tx.Commit();
                }
            }

            _trace.TraceInformation("SignalR database objects installed");
        }
    }
}