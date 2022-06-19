using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using Microsoft.AspNet.SignalR.Messaging;
using Microsoft.Extensions.Logging;
using NHibernate;
using Snork.AspNet.SignalR.FluentNHibernate.Domain;

namespace Snork.AspNet.SignalR.FluentNHibernate
{
    public class MessageRepository : IMessageRepository
    {
        private readonly ISessionFactory _sessionFactory;
        private readonly ILogger<MessageRepository> _logger;

        public MessageRepository(ISessionFactory sessionFactory, ILogger<MessageRepository> logger)
        {
            _logger = logger;
            _sessionFactory = sessionFactory;
        }

        /// <summary>
        /// </summary>
        /// <param name="streamIndex"></param>
        /// <param name="minPayloadIdExclusive"></param>
        /// <returns></returns>
        public IList<MessagesItemBase> GetMessages(int streamIndex, long? minPayloadIdExclusive)
        {
            try
            {
                if (minPayloadIdExclusive == null) return new List<MessagesItemBase>();

                using (var session = _sessionFactory.OpenStatelessSession())
                {
                    using (var tx = session.BeginTransaction(IsolationLevel.Serializable))
                    {
                        var messagesItems = session
                            .CreateQuery(
                                $"select z from {TablenameHelper.GetMessageTableName(streamIndex)} z where z.{nameof(MessagesItemBase.PayloadId)}>:last order by {nameof(MessagesItemBase.PayloadId)}")
                            .SetParameter("last", minPayloadIdExclusive).List<MessagesItemBase>();
                        return messagesItems;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.Print("");
                throw;
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="streamIndex"></param>
        /// <returns></returns>
        public long? GetLastPayloadId(int streamIndex)
        {
            try
            {
                using (var session = _sessionFactory.OpenStatelessSession())
                {
                    using (var tx = session.BeginTransaction(IsolationLevel.Serializable))
                    {
                        var queryString =
                            $"select {nameof(MessageIdItemBase.PayloadId)} from {TablenameHelper.GetIdTableName(streamIndex)}";
                        var tmp = session.CreateQuery(queryString).UniqueResult();

                        //var tmp = session.CreateQuery(queryString).UniqueResult();

                        var _lastPayloadId = tmp as long?;
                        return _lastPayloadId;

                        _lastPayloadId = session
                            .CreateQuery(
                                queryString)
                            .UniqueResult<long?>();
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.Print("");
                throw;
            }
        }

        public int InsertMessage<TMessageType, TIdType>(int streamIndex, IList<Message> messages)
            where TMessageType : MessagesItemBase, new()
            where TIdType : MessageIdItemBase, new()

        {
            try
            {
                _logger.LogDebug("inserting");
                int result;
                using (var session = _sessionFactory.OpenSession())
                {
                    long newPayloadId;
                    using (var tx = session.BeginTransaction(IsolationLevel.Serializable))
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
                        using (var tx = session.BeginTransaction(IsolationLevel.Serializable))
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
                                var sql =
                                    $"delete from `{TablenameHelper.GetMessageTableName(streamIndex)}` where {nameof(MessagesItemBase.PayloadId)} between :min and :max";
                                result = session.CreateQuery(sql)
                                    .SetParameter("min", minPayloadId)
                                    .SetParameter("max", endPayloadId)
                                    .ExecuteUpdate();
                            }

                            tx.Commit();
                        }
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Issue with send");
                throw;
            }
        }
    }
}