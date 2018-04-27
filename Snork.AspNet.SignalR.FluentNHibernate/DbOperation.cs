// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Infrastructure;
using NHibernate;
using Snork.FluentNHibernateTools;

namespace Microsoft.AspNet.SignalR.SqlServer
{
    internal class ZDbOperation
    {
        private readonly List<IDataParameter> _parameters = new List<IDataParameter>();

        public ZDbOperation(string connectionString, string commandText, TraceSource traceSource)
            : this(connectionString, commandText, traceSource, SqlClientFactory.Instance.AsIDbProviderFactory())
        {
        }

        public ZDbOperation(string connectionString, string commandText, TraceSource traceSource,
            IDbProviderFactory dbProviderFactory)
        {
            CommandText = commandText;
            Trace = traceSource;

            SessionFactoryInfo = SessionFactoryFactory.GetSessionFactoryInfo(connectionString);
        }

        public ZDbOperation(string connectionString, string commandText, TraceSource traceSource,
            params IDataParameter[] parameters)
            : this(connectionString, commandText, traceSource)
        {
            if (parameters != null)
            {
                _parameters.AddRange(parameters);
            }
        }

        public SessionFactoryInfo SessionFactoryInfo { get; }

        public string TracePrefix { get; set; }

        public IList<IDataParameter> Parameters
        {
            get { return _parameters; }
        }

        protected TraceSource Trace { get; }


        protected string CommandText { get; }

        public virtual object ExecuteScalar()
        {
            return Execute(cmd => cmd.UniqueResult());
        }

        public virtual int ExecuteNonQuery()
        {
            return Execute(cmd => cmd.ExecuteUpdate());
        }

        public virtual Task<int> ExecuteNonQueryAsync()
        {
            var tcs = new DispatchingTaskCompletionSource<int>();
            return new Task<int>(() =>
            {
                int a;
                try
                {
                    a = ExecuteNonQuery();
                    tcs.SetResult(a);
                    return a;
                }
                catch (Exception ex)
                {
                    tcs.SetUnwrappedException(ex);
                    throw;
                }
            });
        }

        public virtual int ExecuteReader<T>(Action<T, ZDbOperation> processRecord)
        {
            return ExecuteReader(processRecord, null);
        }

        protected virtual int ExecuteReader<T>(Action<T, ZDbOperation> processRecord,
            Action<IQuery> commandAction)
        {
            return Execute(cmd =>
            {
                commandAction?.Invoke(cmd);

                var reader = cmd.Enumerable<T>();
                var count = 0;

                foreach (var x in reader)
                {
                    count++;
                    processRecord(x, this);
                }


                return count;
            });
        }

        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification =
            "It's the caller's responsibility to dispose as the command is returned")]
        [SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities", Justification =
            "General purpose SQL utility command")]
        protected virtual IQuery CreateCommand(ISession session)
        {
            var command = session.CreateQuery(CommandText);
            foreach (var i in Parameters)
            {
                command.SetParameter(i.ParameterName, i.Value);
            }
            return command;
        }

        [SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times", Justification =
            "False positive?")]
        private T Execute<T>(Func<IQuery, T> commandFunc)
        {
            var result = default(T);


            try
            {
                using (var session = SessionFactoryInfo.SessionFactory.OpenSession())
                {
                    var command = CreateCommand(session);
                    TraceCommand(command);
                    result = commandFunc(command);
                }
            }
            finally
            {
            }

            return result;
        }

        private void TraceCommand(IQuery command)
        {
            if (Trace.Switch.ShouldTrace(TraceEventType.Verbose))
            {
                Trace.TraceVerbose("Created DbCommand: CommandType={0}, CommandText={1}, Parameters={2}",
                    CommandType.Text, command.QueryString,
                    Parameters.Cast<IDataParameter>()
                        .Aggregate(string.Empty,
                            (msg, p) => string.Format(CultureInfo.InvariantCulture, "{0} [Name={1}, Value={2}]", msg,
                                p.ParameterName, p.Value))
                );
            }
        }

        [SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times", Justification =
            "Disposed in async Finally block")]
        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification =
            "Disposed in async Finally block")]
        private void Execute<T>(Func<IQuery, Task<T>> commandFunc, DispatchingTaskCompletionSource<T> tcs)
        {
            using (var session = SessionFactoryInfo.SessionFactory.OpenSession())
            {
                var command = CreateCommand(session);
                commandFunc(command)
                    .Then(result => tcs.SetResult(result))
                    .Catch(ex => tcs.SetUnwrappedException(ex), Trace);
            }
        }
    }

    internal class DbOperation
    {
        private readonly IDbProviderFactory _dbProviderFactory;
        private readonly List<IDataParameter> _parameters = new List<IDataParameter>();

        public DbOperation(string connectionString, string commandText, TraceSource traceSource)
            : this(connectionString, commandText, traceSource, SqlClientFactory.Instance.AsIDbProviderFactory())
        {
        }

        public DbOperation(string connectionString, string commandText, TraceSource traceSource,
            IDbProviderFactory dbProviderFactory)
        {
            ConnectionString = connectionString;
            CommandText = commandText;
            Trace = traceSource;
            _dbProviderFactory = dbProviderFactory;
            SessionFactoryInfo = SessionFactoryFactory.GetSessionFactoryInfo(connectionString);
        }

        public DbOperation(string connectionString, string commandText, TraceSource traceSource,
            params IDataParameter[] parameters)
            : this(connectionString, commandText, traceSource)
        {
            if (parameters != null)
            {
                _parameters.AddRange(parameters);
            }
        }

        public SessionFactoryInfo SessionFactoryInfo { get; }

        public string TracePrefix { get; set; }

        public IList<IDataParameter> Parameters
        {
            get { return _parameters; }
        }

        protected TraceSource Trace { get; }

        protected string ConnectionString { get; }

        protected string CommandText { get; }

        public virtual object ExecuteScalar()
        {
            return Execute(cmd => cmd.ExecuteScalar());
        }

        public virtual int ExecuteNonQuery()
        {
            return Execute(cmd => cmd.ExecuteNonQuery());
        }

        public virtual Task<int> ExecuteNonQueryAsync()
        {
            var tcs = new DispatchingTaskCompletionSource<int>();
            Execute(cmd => cmd.ExecuteNonQueryAsync(), tcs);
            return tcs.Task;
        }

        public virtual int ExecuteReader(Action<IDataRecord, DbOperation> processRecord)
        {
            return ExecuteReader(processRecord, null);
        }

        protected virtual int ExecuteReader(Action<IDataRecord, DbOperation> processRecord,
            Action<IDbCommand> commandAction)
        {
            return Execute(cmd =>
            {
                if (commandAction != null)
                {
                    commandAction(cmd);
                }

                var reader = cmd.ExecuteReader();
                var count = 0;

                while (reader.Read())
                {
                    count++;
                    processRecord(reader, this);
                }

                return count;
            });
        }

        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification =
            "It's the caller's responsibility to dispose as the command is returned")]
        [SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities", Justification =
            "General purpose SQL utility command")]
        protected virtual IDbCommand CreateCommand(IDbConnection connection)
        {
            var command = connection.CreateCommand();
            command.CommandText = CommandText;

            if (Parameters != null && Parameters.Count > 0)
            {
                for (var i = 0; i < Parameters.Count; i++)
                {
                    command.Parameters.Add(Parameters[i].Clone(_dbProviderFactory));
                }
            }

            return command;
        }

        [SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times", Justification =
            "False positive?")]
        private T Execute<T>(Func<IDbCommand, T> commandFunc)
        {
            var result = default(T);
            IDbConnection connection = null;

            try
            {
                connection = _dbProviderFactory.CreateConnection();
                connection.ConnectionString = ConnectionString;
                var command = CreateCommand(connection);
                connection.Open();
                TraceCommand(command);
                result = commandFunc(command);
            }
            finally
            {
                if (connection != null)
                {
                    connection.Dispose();
                }
            }

            return result;
        }

        private void TraceCommand(IDbCommand command)
        {
            if (Trace.Switch.ShouldTrace(TraceEventType.Verbose))
            {
                Trace.TraceVerbose("Created DbCommand: CommandType={0}, CommandText={1}, Parameters={2}",
                    command.CommandType, command.CommandText,
                    command.Parameters.Cast<IDataParameter>()
                        .Aggregate(string.Empty,
                            (msg, p) => string.Format(CultureInfo.InvariantCulture, "{0} [Name={1}, Value={2}]", msg,
                                p.ParameterName, p.Value))
                );
            }
        }

        [SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times", Justification =
            "Disposed in async Finally block")]
        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification =
            "Disposed in async Finally block")]
        private void Execute<T>(Func<IDbCommand, Task<T>> commandFunc, DispatchingTaskCompletionSource<T> tcs)
        {
            IDbConnection connection = null;

            try
            {
                connection = _dbProviderFactory.CreateConnection();
                connection.ConnectionString = ConnectionString;
                var command = CreateCommand(connection);

                connection.Open();

                commandFunc(command)
                    .Then(result => tcs.SetResult(result))
                    .Catch(ex => tcs.SetUnwrappedException(ex), Trace)
                    .Finally(state =>
                    {
                        var conn = (DbConnection) state;
                        if (conn != null)
                        {
                            conn.Dispose();
                        }
                    }, connection);
            }
            catch (Exception)
            {
                if (connection != null)
                {
                    connection.Dispose();
                }
                throw;
            }
        }
    }
}