using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Messaging;
using Microsoft.Extensions.Logging;
using NHibernate;
using Snork.AspNet.SignalR.FluentNHibernate.Domain;
using Snork.AspNet.SignalR.FluentNHibernate.Mapping;
using Snork.FluentNHibernateTools;

namespace Snork.AspNet.SignalR.FluentNHibernate
{
    public class FnhMessageRepository : IMessageRepository
    {
        private readonly FNHScaleoutConfiguration _configuration;
        private readonly ILogger<FnhMessageRepository> _logger;
        private readonly ISessionFactory _sessionFactory;
        private readonly Dictionary<int, StreamInfo> _streamInfos = new Dictionary<int, StreamInfo>();

        public FnhMessageRepository(FNHScaleoutConfiguration configuration, ILogger<FnhMessageRepository> logger)
        {
            _configuration = configuration;
            var sessionFactoryInfo = SessionFactoryBuilder.GetFromAssemblyOf<Message0Map>(
                configuration.ProviderType, configuration.ConnectionString,
                new FluentNHibernatePersistenceBuilderOptions {DefaultSchema = configuration.DefaultSchema});
            _logger = logger;
            _sessionFactory = sessionFactoryInfo.SessionFactory;
            for (var streamIndex = 0; streamIndex < configuration.StreamCount; streamIndex++)
                _streamInfos[streamIndex] = new StreamInfo(streamIndex);
        }

        /// <summary>
        /// </summary>
        /// <param name="streamIndex"></param>
        /// <param name="minPayloadIdExclusive"></param>
        /// <returns></returns>
        public IList<MessagesItemBase> GetMessages(int streamIndex, long? minPayloadIdExclusive)
        {
            if (minPayloadIdExclusive == null) return new List<MessagesItemBase>();

            using (var session = _sessionFactory.OpenStatelessSession())
            {
                var messagesItems = session
                    .CreateQuery(
                        _streamInfos[streamIndex].GetMessages)
                    .SetParameter("last", minPayloadIdExclusive).List<MessagesItemBase>();
                return messagesItems;
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="streamIndex"></param>
        /// <returns></returns>
        public long? GetCurrentStreamPayloadId(int streamIndex)
        {
            using (var session = _sessionFactory.OpenStatelessSession())
            {
                var queryString = _streamInfos[streamIndex].GetCurrentStreamPayloadId;
                return session.CreateQuery(queryString).UniqueResult<long?>();
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="streamIndex"></param>
        /// <param name="messages"></param>
        /// <returns></returns>
        public async Task InsertMessage(int streamIndex, IList<Message> messages)
        {
            try
            {
                _logger.LogDebug("inserting");

                using (var session = _sessionFactory.OpenSession())
                {
                    long newPayloadId;
                    var streamInfo = _streamInfos[streamIndex];
                    using (var tx = session.BeginTransaction(IsolationLevel.Serializable))
                    {
                        var query = streamInfo.IncrementStreamPayloadId;
                        var rowCount = await session.CreateQuery(query).ExecuteUpdateAsync();
                        if (rowCount == 0)
                        {
                            var instance =
                                Activator.CreateInstance(streamInfo.MessageIdType) as MessageIdItemBase;
                            instance.PayloadId = 1;
                            instance.RowId = 1;
                            await session.SaveAsync(instance);
                            newPayloadId = 1;
                        }
                        else
                        {
                            newPayloadId = await session
                                .CreateQuery(streamInfo.GetCurrentStreamPayloadId)
                                .UniqueResultAsync<long>();
                        }

                        var message =
                            Activator.CreateInstance(streamInfo.MessageType) as MessagesItemBase;
                        message.Payload = FNHPayload.ToBytes(messages);
                        message.PayloadId = newPayloadId;


                        await session.SaveAsync(message);
                        await session.FlushAsync();
                        await tx.CommitAsync();
                    }


                    if (newPayloadId % _configuration.MaxBlockSize == 0)
                        using (var tx = session.BeginTransaction(IsolationLevel.Serializable))
                        {
                            var query = streamInfo.GetAggregates;
                            var aggregates = await session.CreateQuery(query).UniqueResultAsync<object[]>();
                            if (aggregates[0] != null && aggregates[1] != null)
                            {
                                var rowCount = Convert.ToInt64(aggregates[0]);
                                var minPayloadId = Convert.ToInt64(aggregates[1]);
                                if (rowCount > _configuration.MaxTableSize)
                                {
                                    var overMaxBy = rowCount - _configuration.MaxTableSize;
                                    var endPayloadId = minPayloadId + _configuration.MaxBlockSize - overMaxBy;
                                    await session.CreateQuery(streamInfo.DeleteMessagesInRange)
                                        .SetParameter("min", minPayloadId)
                                        .SetParameter("max", endPayloadId)
                                        .ExecuteUpdateAsync();
                                    await tx.CommitAsync();
                                }
                            }
                        }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Issue with send");
                throw;
            }
        }

        private class StreamInfo
        {
            public StreamInfo(int streamIndex)
            {
                var messageTableName = TableNameHelper.GetMessageTableName(streamIndex);
                var idTableName = TableNameHelper.GetIdTableName(streamIndex);
                GetAggregates =
                    $"select count({nameof(MessagesItemBase.PayloadId)}), min({nameof(MessagesItemBase.PayloadId)}) from {TableNameHelper.GetMessageTableName(streamIndex)}";
                DeleteMessagesInRange =
                    $"delete from `{messageTableName}` where {nameof(MessagesItemBase.PayloadId)} between :min and :max";
                GetMessages =
                    $"select z from {messageTableName} z where z.{nameof(MessagesItemBase.PayloadId)}>:last order by {nameof(MessagesItemBase.PayloadId)}";

                GetCurrentStreamPayloadId =
                    $"select {nameof(MessageIdItemBase.PayloadId)} from {idTableName}";
                IncrementStreamPayloadId =
                    $"update {idTableName} set {nameof(MessageIdItemBase.PayloadId)}={nameof(MessageIdItemBase.PayloadId)}+1";

                MessageIdType = Type.GetType(typeof(MessageIdItemBase).AssemblyQualifiedName.Replace(
                    nameof(MessageIdItemBase),
                    idTableName));

                MessageType = Type.GetType(typeof(MessagesItemBase).AssemblyQualifiedName.Replace(
                    nameof(MessagesItemBase),
                    messageTableName));
            }

            public string GetAggregates { get; }
            public Type MessageIdType { get; }
            public string DeleteMessagesInRange { get; }
            public string GetMessages { get; }
            public string GetCurrentStreamPayloadId { get; }
            public string IncrementStreamPayloadId { get; }
            public Type MessageType { get; }
        }
    }
}