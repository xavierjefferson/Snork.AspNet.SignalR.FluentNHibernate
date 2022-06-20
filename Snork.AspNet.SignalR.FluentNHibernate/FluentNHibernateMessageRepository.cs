using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Messaging;
using Microsoft.Extensions.Logging;
using NHibernate;
using Snork.AspNet.SignalR.FluentNHibernate.Domain;
using Snork.AspNet.SignalR.FluentNHibernate.Mapping;
using Snork.AspNet.SignalR.Kore;
using Snork.AspNet.SignalR.Kore.Domain;
using Snork.FluentNHibernateTools;

namespace Snork.AspNet.SignalR.FluentNHibernate
{
    /// <summary>
    /// </summary>
    public class FluentNHibernateMessageRepository : IMessageRepository
    {
        private readonly FNHScaleoutConfiguration _configuration;
        private readonly ILogger<FluentNHibernateMessageRepository> _logger;
        private readonly ISessionFactory _sessionFactory;
        private readonly Dictionary<int, StreamInfo> _streamInfos = new Dictionary<int, StreamInfo>();

        /// <summary>
        /// </summary>
        /// <param name="configuration"></param>
        /// <param name="logger"></param>
        public FluentNHibernateMessageRepository(FNHScaleoutConfiguration configuration,
            ILogger<FluentNHibernateMessageRepository> logger)
        {
            _configuration = configuration;
            var sessionFactoryInfo = SessionFactoryBuilder.GetFromAssemblyOf<MessagePayloadMap_0>(
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
        /// <returns></returns>
        public long? GetCurrentStreamPayloadId(int streamIndex)
        {
            try
            {
                return SqlUtils.WrapForDeadlock(new CancellationToken(), () =>
                {
                    using (var session = _sessionFactory.OpenStatelessSession())
                    {
                        var queryString = _streamInfos[streamIndex].GetCurrentStreamPayloadIdHql;
                        return session.CreateQuery(queryString).UniqueResult<long?>();
                    }
                }, _configuration.DeadlockInterval);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in {nameof(GetCurrentStreamPayloadId)}");
                throw;
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
                _logger.LogDebug($"{StreamHelper.GetStreamLogPrefix(streamIndex)}inserting");
                var streamInfo = _streamInfos[streamIndex];


                var payloadId = await SqlUtils.WrapForDeadlockAsync(new CancellationToken(), async () =>
                {
                    using (var session = _sessionFactory.OpenSession())
                    {
                        long innerPayloadId;
                        using (var transaction = session.BeginTransaction(IsolationLevel.Serializable))
                        {
                            var rowCount = await session.CreateQuery(streamInfo.IncrementStreamPayloadIdHql)
                                .ExecuteUpdateAsync();
                            if (rowCount == 0)
                            {
                                //no rows exist in table.  Do an insert
                                var instance =
                                    Activator.CreateInstance(streamInfo.MessageIdType) as MessageIdBase;
                                instance.PayloadId = 1;
                                instance.RowId = 1;
                                await session.SaveAsync(instance);
                                innerPayloadId = 1;
                            }
                            else
                            {
                                //row was updated
                                innerPayloadId = await session
                                    .CreateQuery(streamInfo.GetCurrentStreamPayloadIdHql)
                                    .UniqueResultAsync<long>();
                            }

                            //create instance of payload class
                            var message =
                                Activator.CreateInstance(streamInfo.MessageType) as MessagePayloadBase;
                            message.Payload = StreamHelper.ToBytes(messages);
                            message.PayloadId = innerPayloadId;
                            message.InsertedOn = session.GetUtcNow();


                            await session.SaveAsync(message);
                            await session.FlushAsync();
                            await transaction.CommitAsync();
                        }

                        return innerPayloadId;
                    }
                }, _configuration.DeadlockInterval);

                await TryCleanTables(payloadId, streamInfo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{StreamHelper.GetStreamLogPrefix(streamIndex)}Error in {nameof(InsertMessage)}");
                throw;
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="streamIndex"></param>
        /// <param name="minPayloadIdExclusive"></param>
        /// <returns></returns>
        public IList<IMessagePayloadContainer> GetMessages(int streamIndex, long? minPayloadIdExclusive)
        {
            try
            {
                if (minPayloadIdExclusive == null) return new List<IMessagePayloadContainer>();
                return SqlUtils.WrapForDeadlock(new CancellationToken(), () =>
                {
                    using (var session = _sessionFactory.OpenStatelessSession())
                    {
                        var messagesItems = session
                            .CreateQuery(
                                _streamInfos[streamIndex].GetMessagesHql)
                            .SetParameter("last", minPayloadIdExclusive).List<MessagePayloadBase>()
                            .Select(i => (IMessagePayloadContainer) i)
                            .ToList();
                        return messagesItems;
                    }
                }, _configuration.DeadlockInterval);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{StreamHelper.GetStreamLogPrefix(streamIndex)}Error in {nameof(GetMessages)}");
                throw;
            }
        }

        private async Task TryCleanTables(long payloadId, StreamInfo streamInfo)
        {
            using (var session = _sessionFactory.OpenSession())
            {
                if (payloadId % _configuration.MaxBlockSize == 0)
                    await SqlUtils.WrapForDeadlockAsync(new CancellationToken(), async () =>
                    {
                        using (var transaction = session.BeginTransaction(IsolationLevel.Serializable))
                        {
                            var aggregates = await session.CreateQuery(streamInfo.GetAggregatesHql)
                                .UniqueResultAsync<object[]>();
                            if (aggregates[0] != null && aggregates[1] != null)
                            {
                                var rowCount = Convert.ToInt64(aggregates[0]);
                                var minPayloadId = Convert.ToInt64(aggregates[1]);
                                if (rowCount > _configuration.MaxTableSize)
                                {
                                    var overMaxBy = rowCount - _configuration.MaxTableSize;
                                    var endPayloadId = minPayloadId + _configuration.MaxBlockSize - overMaxBy;
                                    await session.CreateQuery(streamInfo.DeleteMessagesInRangeHql)
                                        .SetParameter("min", minPayloadId)
                                        .SetParameter("max", endPayloadId)
                                        .ExecuteUpdateAsync();
                                    await transaction.CommitAsync();
                                }
                            }
                        }
                    }, _configuration.DeadlockInterval);
            }
        }

        private class StreamInfo
        {
            private static readonly string idTablePayloadId = nameof(MessageIdBase.PayloadId);
            private static readonly string payloadTablePayloadId = nameof(MessagePayloadBase.PayloadId);

            public StreamInfo(int streamIndex)
            {
                var payloadTableName = TableNameHelper.GetPayloadTableName(streamIndex);
                var idTableName = TableNameHelper.GetIdTableName(streamIndex);


                GetAggregatesHql =
                    $"select count({payloadTablePayloadId}), min({payloadTablePayloadId}) from `{payloadTableName}`";
                DeleteMessagesInRangeHql =
                    $"delete from `{payloadTableName}` where {payloadTablePayloadId} between :min and :max";
                GetMessagesHql =
                    $"select z from `{payloadTableName}` z where z.{payloadTablePayloadId}>:last order by {payloadTablePayloadId}";

                GetCurrentStreamPayloadIdHql = $"select {idTablePayloadId} from `{idTableName}`";
                IncrementStreamPayloadIdHql = $"update `{idTableName}` set {idTablePayloadId}={idTablePayloadId}+1";

                var idType = typeof(MessageId_0);
                MessageIdType =
                    Type.GetType(
                        idType.AssemblyQualifiedName.Replace(idType.Name, idTableName));

                var payloadType = typeof(MessagePayload_0);
                MessageType =
                    Type.GetType(
                        payloadType.AssemblyQualifiedName.Replace(payloadType.Name,
                            payloadTableName));
            }


            public string GetAggregatesHql { get; }
            public Type MessageIdType { get; }
            public string DeleteMessagesInRangeHql { get; }
            public string GetMessagesHql { get; }
            public string GetCurrentStreamPayloadIdHql { get; }
            public string IncrementStreamPayloadIdHql { get; }
            public Type MessageType { get; }
        }
    }
}