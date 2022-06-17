using System;
using System.Threading;

namespace UnitTestProject1.Fixtures
{
    public class OneTestAtATimeFixture : IDisposable
    {
        private static readonly object GlobalLock = new object();

        public OneTestAtATimeFixture()
        {
            Monitor.Enter(GlobalLock);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }


        protected virtual void Dispose(bool disposing)
        {
            if (disposing) Monitor.Exit(GlobalLock);
        }
    }
}