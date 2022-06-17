using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Client;
using Microsoft.Owin.Cors;
using Microsoft.Owin.Hosting;
using Owin;
using Snork.AspNet.SignalR.FluentNHibernate;
using Snork.FluentNHibernateTools;
using UnitTestProject1.Fixtures;
using Xunit;

namespace UnitTestProject1
{
    [Collection(Constants.RenamerTestFixtureCollectionName)]
    public class Program
    {
        // Initialize the trace source.

        public Program(RenamerTestFixture fixture)
        {
            _fixture = fixture;
            var abc = new StreamWriter("abc.txt");
            Trace.AutoFlush = true;
            Trace.Listeners.Add(new TextWriterTraceListener(abc));
        }

        private readonly RenamerTestFixture _fixture;
        private static readonly TraceSource ts = new TraceSource("TraceTest");

        [Fact]
        [SwitchAttribute("SourceSwitch", typeof(SourceSwitch))]
        public async Task Main()
        {
            var sourceSwitch = new SourceSwitch("SourceSwitch", "Verbose");
            ts.Switch = sourceSwitch;
            try
            {
                // This will *ONLY* bind to localhost, if you want to bind to all addresses
                // use http://*:8080 to bind to all addresses. 
                // See http://msdn.microsoft.com/library/system.net.httplistener.aspx 
                // for more information.
                var url = "http://localhost:8080";
                using (WebApp.Start(url, app =>
                {
                    app.UseCors(CorsOptions.AllowAll);
                    GlobalHost.DependencyResolver.UseFluentNHibernate(_fixture.ConnectionString,
                        ProviderTypeEnum.SQLite);
                    app.MapSignalR();
                }))
                {
                    Console.WriteLine("Server running on {0}", url);


                    //var hubContext = GlobalHost.ConnectionManager.GetHubContext<MyHub>();

                    var clients = new Dictionary<int, ClientInfo>();
                    for (var i = 0; i < 3; i++)
                    {
                        var cLient = new ClientInfo {Id = i};
                        clients[i] = cLient;
                        cLient.Init($"{url}/signalr");
                        cLient.HubProxy.On<string, string>("AddMessage",
                            (name, message) => Debug.Print($"{cLient.Id} Received {name}:{message}"));
                        await cLient.HubConnection.Start();
                    }


                    for (var i = 0; i < 1; i++) await clients[0].HubProxy.Invoke("Send", "Client", $"Message #{i}");

                    Console.ReadLine();
                }
            }
            finally
            {
                {
                    Trace.Flush();
                }
            }
        }
    }


    public class ClientInfo
    {
        public HubConnection HubConnection { get; set; }
        public string Url { get; set; }
        public IHubProxy HubProxy { get; set; }
        public int Id { get; set; }

        public void Init(string url)
        {
            Url = url;
            HubConnection = new HubConnection($"{url}");

            //Get a proxy object that will be used to interact with the specific hub on the server
            //There may be many hubs hosted on the server, so provide the type name for the hub
            HubProxy = HubConnection.CreateHubProxy(nameof(MyHub));
        }
    }

    public class MyHub : Hub
    {
        public void Send(string name, string message)
        {
            Clients.All.addMessage(name, message);
        }
    }
}