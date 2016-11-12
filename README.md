# Asp.Net Core Consul Configuration

## Installation

Add `AspNetCore.Configuration.Consul` to the `dependencies` section of your project.json

## Usage

Add the following to your `StartUp` class for the minimal setup:

```
var builder = new ConfigurationBuilder()
    .AddConsul($"{env.ApplicationName}.{env.EnvironmentName}");
Configuration = builder.Build();
```

Assuming the application is running in the development environment, this will load a json configuration object from the path `ApplicationName/Development`

## Configuration Options