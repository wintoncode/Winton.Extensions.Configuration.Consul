# Winton.Extensions.Configuration.Consul

[![Build status](https://ci.appveyor.com/api/projects/status/vlouj9n5ahqsgql9/branch/master?svg=true)](https://ci.appveyor.com/project/wintoncode/winton-extensions-configuration-consul/branch/master)
[![Travis Build Status](https://travis-ci.org/wintoncode/Winton.Extensions.Configuration.Consul.svg?branch=master)](https://travis-ci.org/wintoncode/Winton.Extensions.Configuration.Consul)
[![NuGet version](https://img.shields.io/nuget/v/Winton.Extensions.Configuration.Consul.svg)](https://www.nuget.org/packages/Winton.Extensions.Configuration.Consul)
[![NuGet version](https://img.shields.io/nuget/vpre/Winton.Extensions.Configuration.Consul.svg)](https://www.nuget.org/packages/Winton.Extensions.Configuration.Consul)

Adds support for configuring .NET Core applications using Consul. Works great with [git2consul](https://github.com/Cimpress-MCP/git2consul).

- [Installation](#installation)
- [Usage](#usage)
    - [Minimal Setup](#minimal-setup)
    - [Options](#options)
- [Configure Parsing Options](#configure-parsing-options)
    - [Consul values are JSON](#consul-values-are-json)
    - [Consul values are scalars](#consul-values-are-scalars)
    - [Consul values are a mix of JSON and scalars](#consul-values-are-a-mix-of-json-and-scalars)
    - [Customizing the `ConvertConsulKVPairToConfig` strategy](#customizing-the-ConvertConsulKVPairToConfig-strategy)

## Installation

Add `Winton.Extensions.Configuration.Consul` to your project's dependencies, either via the NuGet package manager or as a `PackageReference` in the csproj file.

## Usage

### Minimal Setup

The library provides an extension method called `AddConsul` for `IConfigurationBuilder` in the same way that other configuration providers do. The `IConfigurationBuilder` is usually configured in either the `Program` or `Startup` class for an ASP.NET Core application. See Microsoft's [documentation](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/configuration/?view=aspnetcore-3.0) for more information about `IConfigurationBuilder`.

A minimal example is shown below:

```csharp
builder
    .AddConsul($"{env.ApplicationName}/{env.EnvironmentName}");
```

Assuming the application is running in the 'Development' environment and the application name is 'Website', then this will load a JSON configuration object from the `Website/Development` key in Consul.

### Options

`AddConsul` has an overload with an additional third parameter of type `Action<IConsulConfigurationSource>` which allows the options outlined below to be set.

* **`ConsulConfigurationOptions`**

   An `Action<ConsulClientConfiguration>` that can be used to configure the underlying Consul client.
* **`ConsulHttpClientHandlerOptions`**

   An `Action<HttpClientHandler>` that can be used to configure the underlying Consul client's HTTP handler options.
* **`ConsulHttpClientOptions`**

   An `Action<HttpClient>` that can be used to configure the underlying Consul client's HTTP options.
* **`KeyToRemove`**

   The portion of the Consul key to remove from the configuration keys.
   By default, when the configuration is parsed, the keys are created by removing the root key in Consul where the configuration is located.
   This defaults to `Key`.
* **`OnLoadException`**

   An `Action<ConsulLoadExceptionContext>` that can be used to configure how exceptions thrown during the first load should be handled.
* **`OnWatchException`**

   A `Func<ConsulWatchExceptionContext, TimeSpan>` that can be used to configure how exceptions thrown when watching for changes should be handled.
   The `TimeSpan` that is returned is used to set a delay before retrying.
   The `ConsulWatchExceptionContext` provides data that can be used to implement a back-off strategy or to cancel watching altogether.
* **`Optional`**

   A `bool` that indicates whether the config is optional. If `false` then it will throw during the first load if the config is missing for the given key. Defaults to `false`.
* **`Parser`**

   The parser to use, which should match the format of the configuration stored in Consul. Defaults to `JsonConfigurationParser`. Either use those under `Winton.Extensions.Configuration.Consul.Parsers` or create your own by implementing `IConfigurationParser`.
* **`PollWaitTime`**

   The amount of time the client should wait before timing out when polling for changes.
   If this is set too low it can lead to excessive requests being issued to Consul.
   Note this setting does not affect how quickly updates propagate, because when a value changes the long polling query returns immediately.
   It is better to think of this as the frequency with which it issues calls in the long polling loop in the case where there is no change.
   Defaults to 5 minutes.
* **`ReloadOnChange`**

   A `bool` indicating whether to reload the config when it changes in Consul.
   If `true` it will watch the configured key for changes. When a change occurs the config will be asynchronously reloaded and the `IChangeToken` will be triggered to signal that the config has been reloaded. Defaults to `false`.

* **`ConvertConsulKVPairToConfig`**

   A `Func<KVPair, IEnumerable<KeyValuePair<string, string>>>` which gives you complete control over the parsing of fully qualified consul keys and raw consul values; the default implementation will:

   - Use the configured `Parser` to parse consul values
   - Remove the configured `KeyToRemove` prefix from consul keys

   When setting this member, however, you bypass the default key and value processing and `Parser` and `KeyToRemove` have no effect unless your `ConvertConsulKVPairToConfig` function uses them.

## Configure Parsing Options

### Consul values are JSON

By default this configuration provider will load all key-value pairs from Consul under the specified root key, but by default it assumes that the values of the leaf keys are encoded as JSON.

Take the following example of a particular instance of the Consul KV store:

```
- myApp/
    - auth/
        {
            "appId": "guid",
            "claims": [
                "email",
                "name"
            ]
        }
    - logging/
        {
            "level": "warn"
        }
```

In this instance we could add Consul as a configuration source like so:

```csharp
var configuration = builder
    .AddConsul("myApp", cancellationToken)
    .Build();
```

The resultant configuration would contain sections for `auth` and `logging`. As a concrete example `configuration.GetValue<string>("logging:level")` would return `"warn"` and `configuration.GetValue<string>("auth:claims:0")` would return `"email"`.

### Consul values are scalars

Sometimes however, config in Consul is stored as a set of expanded keys. For instance, tools such as `consul-cli` load config in this format.

The config in this case can be thought of as a tree under a specific root key in Consul. For instance, continuing with the example above, the config would be stored as:

```
- myApp/
    - auth/
        - appId/
            "guid"
        - claims/
            0/
                "email"
            1/
                "name"
    - logging/
        - level/
            "warn"
```

As outlined above this configuration provider deals with recursive keys by default. The only difference here is that the values are no longer encoded as JSON. Therefore, in order to load this config the parser must be changed. This can be done like so when adding the configuration provider:

```csharp
builder
    .AddConsul(
        "myApp",
        options =>
        {
            options.Parser = new SimpleConfigurationParser();
        });
```

The `SimpleConfigurationParser` expects to encounter a scalar value at each leaf key in the tree.

### Consul values are a mix of JSON and scalars

If you need to support both expanded keys and JSON values then this can be achieved by putting them under different root keys and adding multiple configuration sources. For example:

```csharp
builder
    .AddConsul(
        "myApp/expandedKeys",
        options =>
        {
            options.Parser = new SimpleConfigurationParser();
        })
    .AddConsul("myApp/jsonValues", cancellationToken);
```

### Customizing the `ConvertConsulKVPairToConfig` strategy

Sometimes you may need more control over the conversion of raw consul KV pairs into `IConfiguration` data.  In this case you can set a custom `ConvertConsulKVPairToConfig` function:

```csharp
builder
    .AddConsul(
        "myApp",
        options =>
        {
            options.ConvertConsulKVPairToConfig = kvPair =>
            {
                var normalizedKey = kvPair.Key
                    .Replace("base/key", string.Empty)
                    .Replace("__", "/")
                    .Replace("/", ":")
                    .Trim('/');

                using Stream valueStream = new MemoryStream(kvPair.Value);
                using var streamReader = new StreamReader(valueStream);
                var parsedValue = streamReader.ReadToEnd();

                return new Dictionary<string, string>()
                {
                    { normalizedKey, parsedValue }
                };
            };
        });
```

> :warning: Caution: by customizing this `ConvertConsulKVPairToConfig` strategy you bypass any automatic invocation of the configured `Parser` and `KeyToRemove` so it becomes your responsibility to use them as needed by your scenario.
