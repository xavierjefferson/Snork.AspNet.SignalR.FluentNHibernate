using System;
using System.Data.SQLite;
using System.IO;
using NHibernate;
using Snork.FluentNHibernateTools;

namespace UnitTestProject1.Fixtures
{
    public class RenamerTestFixture : IDisposable
    {
        private readonly FileInfo _fileInfo;
        private readonly SessionFactoryInfo info;

        public RenamerTestFixture()
        {
            _fileInfo = new FileInfo(Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".sqlite"));
            ConnectionString = string.Format("Data Source={0};Version=3;", _fileInfo.FullName);
            SQLiteConnection.CreateFile(_fileInfo.FullName);
        }

        public string ConnectionString { get; }
        public ISessionFactory SessionFactory { get; }

        public void Dispose()
        {
            SessionFactory?.Dispose();
            _fileInfo.Delete();
        }
    }
}