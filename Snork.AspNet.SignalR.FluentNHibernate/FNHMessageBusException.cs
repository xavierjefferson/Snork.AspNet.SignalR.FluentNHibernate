using System;

namespace Snork.AspNet.SignalR.FluentNHibernate
{
    [Serializable]
    public class FNHMessageBusException : Exception
    {
        public FNHMessageBusException(string message)
            : base(message)
        {
        }
    }
}