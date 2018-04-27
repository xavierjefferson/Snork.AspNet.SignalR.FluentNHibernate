using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Messaging;
using NHibernate;
using NHibernate.Linq;
using Snork.AspNet.SignalR.FluentNHibernate.Domain;

namespace Snork.AspNet.SignalR.FluentNHibernate
{
    internal class FNHSender<TMessageType, TIdType> where TMessageType : MessageItemBase, new()
        where TIdType : MessageIdItemBase, new()
    {
        private readonly ISessionFactory _sessionFactory;
        private readonly TraceSource _trace;

        public FNHSender(ISessionFactory sessionFactory, TraceSource traceSource)
        {
            _sessionFactory = sessionFactory;

            _trace = traceSource;
        }

        public Task Send(IList<Message> messages)
        {
            if (messages == null || messages.Count == 0)
            {
                return TaskAsyncHelper.Empty;
            }
            var count = InsertMessage(messages);
            return Task.FromResult(count);
        }


        private int InsertMessage(IList<Message> messages)

        {
            try
            {
                _trace.TraceVerbose("inserting");
                int result;
                using (var session = _sessionFactory.OpenSession())
                {
                    long newPayloadId;
                    using (var tx = session.BeginTransaction(IsolationLevel.RepeatableRead))
                    {
                        var messageId = session.Query<TIdType>().FirstOrDefault();
                        if (messageId == null)
                        {
                            messageId = new TIdType {PayloadId = 1, RowId = 1};
                            session.Save(messageId);
                        }
                        else
                        {
                            messageId.PayloadId++;
                            session.Save(messageId);
                        }
                        newPayloadId = messageId.PayloadId;
                        session.Save(new TMessageType
                        {
                            Payload = FNHPayload.ToBytes(messages),
                            PayloadId = newPayloadId
                        });
                        tx.Commit();
                    }
                    result = 1;
                    var maxTableSize = 10000;
                    var blockSize = 2500;
                    if (newPayloadId % blockSize == 0)
                    {
                        using (var tx = session.BeginTransaction(IsolationLevel.ReadCommitted))
                        {
                            var queryable = session.Query<TMessageType>();
                            var aggregates = queryable
                                .Select(m => new {Count = queryable.Count(), Min = queryable.Min(i => i.PayloadId)})
                                .First();

                            var rowCount = aggregates.Count;
                            var minPayloadId = aggregates.Min;
                            if (rowCount > maxTableSize)
                            {
                                var overMaxBy = rowCount - maxTableSize;
                                var endPayloadId = minPayloadId + blockSize - overMaxBy;
                                var sql = string.Format("delete from `{0}` where {1} between :min and :max",
                                    nameof(TMessageType),
                                    nameof(MessageItemBase.PayloadId));
                                result = session.CreateQuery(sql)
                                    .SetParameter("min", minPayloadId)
                                    .SetParameter("max", endPayloadId)
                                    .ExecuteUpdate();
                            }
                            tx.Commit();
                        }
                    }
                }
                return result;
            }
            catch (Exception ex)
            {
                _trace.TraceError("Issue with send", ex);
                throw;
            }
        }
    }
}