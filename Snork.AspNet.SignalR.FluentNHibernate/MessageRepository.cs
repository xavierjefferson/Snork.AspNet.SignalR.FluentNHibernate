using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using NHibernate;
using Snork.AspNet.SignalR.FluentNHibernate.Domain;

namespace Snork.AspNet.SignalR.FluentNHibernate
{
    public class MessageRepository : IMessageRepository
    {
        private readonly ISessionFactory _sessionFactory;

        public MessageRepository(ISessionFactory sessionFactory)
        {
            this._sessionFactory = sessionFactory;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="streamIndex"></param>
        /// <param name="minPayloadIdExclusive"></param>
        /// <returns></returns>
        public IList<MessagesItemBase> GetMessages(int streamIndex, long? minPayloadIdExclusive)
        {
            try
            {
                if (minPayloadIdExclusive == null)
                {
                    return new List<MessagesItemBase>();
                }

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
        /// 
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
    }
}