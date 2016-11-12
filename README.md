# Asp.Net Core Consul Configuration

Add support for configuring Asp.Net Core application using Consul. It is expected that configuration will be stored as a single object under a given key. Works great with [git2consul](https://github.com/Cimpress-MCP/git2consul).

## Installation

Add `AspNetCore.Configuration.Consul` to the `dependencies` section of your project.json

## Usage

Add the following to your `StartUp` class for the minimal setup:

```csharp
var builder = new ConfigurationBuilder()
    .AddConsul($"{env.ApplicationName}.{env.EnvironmentName}");
Configuration = builder.Build();
```

Assuming the application is running in the development environment and the application name is Website, this will load a json configuration object under the key `Website/Development` in Consul.

Or specify an options `Action` as a second argument to set the options specified below.

## Configuration Options
* **`ConsulConfigurationOptions`**

   An `Action` that can be used to configure Consul options
* **`ConsulHttpClientOptions`**

   An `Action` that can be used to configure Consul HTTP options
* **`ConsulHttpClientHandlerOptions`**

   An `Action` that can be used to configure Consul HTTP handler options
* **`OnLoadException`**

   An `Action` that can be used to configure how exceptions should be handled during load
* **`Optional`**

   A `bool` that indicates whether the config is optional. If `false` then will throw during load if the config is missing for the given key.
* **`Parser`**

   The parser to use, should match the format of the configuration stored in Consul. Defaults to `JsonConfigurationParser`. Either use those under `Chocolate.AspNetCore.Configuration.Consul.Parsers` or create your own by implementing `IConfigurationParser`.
* **`ReloadOnChange`**

   A `bool` indicating whether to reload the config when it changes in Consul. *NOT CURRENTLY SUPPORTED*

## Backlog
* Add more parsers for different file formats
* Add support for reloading the configuration when it changes.
* Add support for expanded configuration where the configuration is a tree of KV pairs under the root key