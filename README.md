# LogFlake Client .NET Framework (for EURA Web Applications) ![Version](https://img.shields.io/badge/version-1.6.0-blue.svg?cacheSeconds=2592000)

> This repository contains the sources for the client-side components of the LogFlake product suite for applications logs and performance collection for .NET Framework applications.

### 🏠 [LogFlake Website](https://logflake.io) |  🔥 [CloudPhoenix Website](https://cloudphoenix.it)

## Downloads

|NuGet Package Name|Version|Downloads|
|:-:|:-:|:-:|
| [LogFlake.Client.NetFramework](https://www.nuget.org/packages/LogFlake.Client.NetFramework) | ![NuGet Version](https://img.shields.io/nuget/v/logflake.client.netframework) | ![NuGet Downloads](https://img.shields.io/nuget/dt/logflake.client.netframework) |

## Usage
1. Retrieve your _application-key_ from Application Settings in LogFlake UI;
2. Store the _application-key_ in your `Web.config` file and name it `LogFlakeAppId`;
3. In the entry-point of your application, add the following line:    
```csharp
StaticLogFlake.Configure(ConfigurationManager.AppSettings["LogFlakeAppId"], "https://app.logflake.io/");
```
4. Use it in your service
```csharp
// SimpleService.cs

public void MyMethod()
{
    try 
    {
        doSomething();
        StaticLogFlake.Instance.SendLog(LogLevels.DEBUG, "correlation", "Hello World");
    }
    catch (MeaningfulException ex)
    {
        StaticLogFlake.Instance.SendException(e);
    }
}
```
