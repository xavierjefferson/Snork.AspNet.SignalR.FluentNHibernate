namespace Snork.AspNet.SignalR.Kore.Domain
{
    public interface IMessagePayloadContainer
    {
        long PayloadId { get; set; }
        byte[] Payload { get; set; }
    }
}