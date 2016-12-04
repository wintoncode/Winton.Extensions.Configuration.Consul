# Asp.Net Core Consul Configuration

[![Build status](https://ci.appveyor.com/api/projects/status/rfttyjwt2p4nswpf/branch/master?svg=true)](https://ci.appveyor.com/project/Choc13/aspnetcore-configuration-consul/branch/master)
[![Travis Build Status](https://travis-ci.org/Choc13/AspNetCore.Configuration.Consul.svg?branch=master)](https://travis-ci.org/Choc13/AspNetCore.Configuration.Consul)
[![NuGet version](https://img.shields.io/nuget/v/AspNetCore.Configuration.Consul.svg)](https://www.nuget.org/packages/AspNetCore.Configuration.Consul)
[![NuGet version](https://img.shields.io/nuget/vpre/AspNetCore.Configuration.Consul.svg)](https://www.nuget.org/packages/AspNetCore.Configuration.Consul)

Adds support for configuring Asp.Net Core application using Consul. It is expected that the configuration will be stored as a single object under a given key in Consul. Works great with [git2consul](https://github.com/Cimpress-MCP/git2consul).

## Installation

Add `AspNetCore.Configuration.Consul` to the `dependencies` section of your project.json

## Usage

Add the following to your `StartUp` class for the minimal setup:

```csharp
var cancellationTokenSource = new cancellationTokenSource();
var builder = new ConfigurationBuilder()
    .AddConsul(
        $"{env.ApplicationName}.{env.EnvironmentName}",
        cancellationTokenSource.Token);
Configuration = builder.Build();
```

Assuming the application is running in the development environment and the application name is Website, this will load a json configuration object from the key `Website/Development` in Consul.

The `CancellationToken` is used to cancel any current requests or watches with Consul.
It is recommended that this is cancelled during application shut down to clean up resources. This can be done like so in the `Configure` method of your `StartUp` class by injecting the `IApplicationLifetime`:

```csharp
public void Configure(IApplicationBuilder app, IHostingEnvironment env, IApplicationLifetime appLifetime)
{
    // Other app configuration

    appLifetime.ApplicationStopping.Register(_cancellationTokenSource.Cancel);
}
```

An options `Action` can be specified as a third argument to set the options outlined below.

## Configuration Options
* **`ConsulConfigurationOptions`**

   An `Action` that can be used to configure Consul options
* **`ConsulHttpClientOptions`**

   An `Action` that can be used to configure Consul HTTP options
* **`ConsulHttpClientHandlerOptions`**

   An `Action` that can be used to configure Consul HTTP handler options
* **`OnLoadException`**

   An `Action` that can be used to configure how exceptions should be handled during load
* **`OnLoadException`**

   An `Action` that can be used to configure how exceptions should be handled that are thrown when watching for changes
* **`Optional`**

   A `bool` that indicates whether the config is optional. If `false` then will throw during load if the config is missing for the given key.
* **`Parser`**

   The parser to use, should match the format of the configuration stored in Consul. Defaults to `JsonConfigurationParser`. Either use those under `Chocolate.AspNetCore.Configuration.Consul.Parsers` or create your own by implementing `IConfigurationParser`.
* **`ReloadOnChange`**

   A `bool` indicating whether to reload the config when it changes in Consul.
   If `true` it will watch the configured key for changes and then reload the config asynchronously and trigger the `IChangeToken` to raise the event that the config has been reloaded.

## Backlog
* Add more parsers for different file formats
* Add support for expanded configuration where the configuration is a tree of KV pairs under the root key