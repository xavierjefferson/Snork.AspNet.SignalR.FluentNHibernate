#  FluentNHibernate Backplane for SignalR  - An Implementation for MS SQL Server, MySQL, PostgreSQL, Oracle, Firebird, and DB/2
[![Latest version](https://img.shields.io/nuget/v/Snork.AspNet.SignalR.FluentNHibernate.svg)](https://www.nuget.org/packages/Snork.AspNet.SignalR.FluentNHibernate/) 

This is an NHibernate-backed implementation of a SignalR backplane that supports MS SQL Server, MySQL, PostgreSQL, Oracle, Firebird, and DB/2.  When deployed, this library will automatically generate tables required for storing SignalR metadata, and pass the correct SQL flavor to the database transparently.  The intention of doing an implementation like this one is to be able to share tentative improvements with a broad audience of developers.

FluentNHibernate / NHibernate enthusiasts may note that while NHibernate supports the SQLite, MS Access (Jet), and SQL Server Compact Edition desktop databases, none of these are likely to work correctly because they're wonky with multithreading, and there's no plan to support them.

## Installation

Run the following command in the NuGet Package Manager console to install Snork.AspNet.SignalR.FluentNHibernate:

```
Install-Package Snork.AspNet.SignalR.FluentNHibernate
```

You will need to install an [additional driver package](DriverPackage.md) for all RDBMS systems except SQL Server.

## Database Implementation Notes
The package includes an enumeration of database providers AND their specific flavors of SQL across various SQL versions:
```
    public enum ProviderTypeEnum
    {
        None = 0,
      
        OracleClient10 = 3,
        OracleClient9 = 4,
        PostgreSQLStandard = 5,
        PostgreSQL81 = 6,
        PostgreSQL82 = 7,
        Firebird = 8,
       
        DB2Informix1150 = 10,
        DB2Standard = 11,
        MySQL = 12,
        MsSql2008 = 13,
        MsSql2012 = 14,
        MsSql2005 = 15,
        MsSql2000 = 16,
        OracleClient10Managed = 17,
        OracleClient9Managed = 18,
    }
```
The enumeration values correspond to the list of providers NHibernate supports.  When you instantiate a provider, you'll pass the best enumeration value to the FluentNHibernateStorageFactory.For method, along with your connection string.  I wrote it this way so you don't have to be concerned with the underlying implementation details for the various providers, which can be a little messy.  


### Oracle
Be advised that two of the four Oracle options (OracleClient9Managed, OracleClient10Managed) use the **Oracle.ManagedDataAccess** client library internally, while the other two (OracleClient9, OracleClient10) use the **System.Data.OracleClient** client library.  I'm not Oracle savvy, and I could only get **Oracle.ManagedDataAccess** to work properly (the other is NHibernate's default).  You may have a different experience.

## Usage - Simple connection string and provider type approach
I may simplify the implementation later, but I think this code is pretty painless  Usage within an application would probably employ the OWin startup approach for SignalR, which is pretty well-documented.  Please note the properties, which MAY include specifying a database schema, passed to the method:
```
using Microsoft.AspNet.SignalR;
using Microsoft.Owin.Cors;
using Owin;
using Snork.AspNet.SignalR.FluentNHibernate;
using Snork.FluentNHibernateTools;

namespace SignalRSelfHost
{
    internal class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            app.UseCors(CorsOptions.AllowAll);
            GlobalHost.DependencyResolver.UseFluentNHibernate(
                "Server=myServerAddress;Database=myDataBase;Uid=myUsername;Pwd=myPassword;", ProviderTypeEnum.MySQL);
            app.MapSignalR();
        }
    }
}
```
## Usage - more verbose
```
using Microsoft.AspNet.SignalR;
using Microsoft.Owin.Cors;
using Owin;
using Snork.AspNet.SignalR.FluentNHibernate;
using Snork.FluentNHibernateTools;

namespace SignalRSelfHost
{
    internal class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            app.UseCors(CorsOptions.AllowAll);
            GlobalHost.DependencyResolver.UseFluentNHibernate(
                new FNHScaleoutConfiguration("Server=.\\sqlexpress;Database=signalrtest;Trusted_Connection=True;",
                    ProviderTypeEnum.MsSql2008) {TableCount = 2, DefaultSchema = "dbo"});
            app.MapSignalR();
        }
    }
}
```
Description of optional parameters:
- `TableCount` - increasing table count will lower latency and increase throughput, but also makes the backplane more "chatty".  Defaults to 1, and this implementation supports any number between 1 and 10.
- `DefaultSchema` - database schema into which the tables will be created.  Default is database provider specific ("dbo" for SQL Server, "public" for PostgreSQL, etc).

## Build
Please use Visual Studio or any other tool of your choice to build the solution

# Database Stuff

 - During first-time use, you'll need table-creation rights on your RDBMS.
 - You can't specify table names (yet).  But you can specify the schema.  See the sample code.
 - Since this uses an OR/M, there are no stored procedures or views to be installed.
 

 