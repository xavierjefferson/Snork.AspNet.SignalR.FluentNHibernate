namespace Snork.AspNet.SignalR.FluentNHibernate
{
    internal class TablenameHelper
    {
        public static string GetIdTableName(int streamIndex)
        {
            return $"Messages_{streamIndex}_Id";
        }

        public static string GetMessageTableName(int streamIndex)
        {
            return $"Messages_{streamIndex}";
        }
    }
}