namespace Snork.AspNet.SignalR.FluentNHibernate
{
    internal class TableNameHelper
    {
        public static string GetIdTableName(int streamIndex)
        {
            return $"MessageId_{streamIndex}";
        }

        public static string GetPayloadTableName(int streamIndex)
        {
            return $"MessagePayload_{streamIndex}";
        }
    }
}