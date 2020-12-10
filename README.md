# RabbitMQ.Fakes.DotNetStandard

[![NuGet Version and Downloads count](https://buildstats.info/nuget/RabbitMQ.Fakes.DotNetStandard)](https://www.nuget.org/packages/RabbitMQ.Fakes.DotNetStandard)

__RabbitMQ.Fakes.DotNetStandard__ is a .NET Standard 2.0 library (forked from the [RabbitMQ.Fakes](https://github.com/Parametric/RabbitMQ.Fakes) library) that contains fake implementations of the interfaces defined in the __[RabbitMQ.Client](https://github.com/rabbitmq/rabbitmq-dotnet-client)__ library.

The implementation is entirely in-memory, which eliminates the dependency on a live RabbitMQ server for unit testing.

__NOTE:__ This library is inteded to be used for testing purposes, __only__ (a la [Microsoft.EntityFrameworkCore.InMemory](https://www.nuget.org/packages/Microsoft.EntityFrameworkCore.InMemory/)).

# Differences between RabbitMQ.Fakes and RabbitMQ.Fakes.DotNetStandard

__RabbitMQ.Fakes__ is a .NET __Framework__ 4.5 library, which is only compatible with .NET Framework.

__RabbitMQ.Fakes.DotNetStandard__ is a .NET __Standard__ 2.0 library, which is compatible with __both__ .NET Framework and .NET Core.

# RabbitMQ.Client versions

__RabbitMQ.Fakes.DotNetStandard__ has a dependency on the __[RabbitMQ.Client](https://github.com/rabbitmq/rabbitmq-dotnet-client)__ library.

This may cause conflicts if you are referencing __RabbitMQ.Fakes.DotNetStandard__ and __RabbitMQ.Client__ (e.g. in a test project) with different versions of __RabbitMQ.Client__.

Below is a table of the version mappings between __RabbitMQ.Fakes.DotNetStandard__ and __RabbitMQ.Client__.

| RabbitMQ.Fakes.DotNetStandard Version | RabbitMQ.Client Version |
| --- | --- |
| 1.0.1 - 1.0.4 | 5.1.0 |
| 2.0.0+ | 6.2.1 |

# Requirements
* [.NET runtime that supports .NET standard 2.0 libraries](https://docs.microsoft.com/en-us/dotnet/standard/net-standard)
  * .NET Core 2.0+
  * .NET Framework 4.6.1+ (Technically the minimum version supported, but you should use .NET Framework 4.7.2+. See the previous link.)
* Nuget Package Manger

# Projects
* __RabbitMQ.Fakes.DotNetStandard:__ Implementation of the fakes using __.NET Standard 2.0__.
* __RabbitMQ.Fakes.DotNetCoreTests:__ Unit tests around the fake implementation using __.NET Core 3.1__.

# Fakes
* __RabbitServer:__ In memory representation of a RabbitMQ server. This is where the Exchanges / Queues / Bindings / Messages are held.
* __FakeConnectionFactory:__ Fake implementation of the RabbitMQ `IConnectionFactory`. Returns a `FakeConnection` when the `CreateConnection()` method is called.
* __FakeConnection:__ Fake implementation of the RabbitMQ `IConnection`. Returns a `FakeModel` when the `CreateModel()` method is called.
* __FakeModel:__ Fake implementation of the RabbitMQ `IModel`. Interacts with the `RabbitServer` instance passed into the `FakeConnectionFactory`.

# Sample Usage
See the UseCases in the RabbitMQ.Fakes.DotNetCoreTests project for sample usage.
