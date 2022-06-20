using System;

namespace Snork.AspNet.SignalR.Kore
{
    [Serializable]
    public class KoreMessageBusException : Exception
    {
        public KoreMessageBusException(string message)
            : base(message)
        {
        }
    }
}