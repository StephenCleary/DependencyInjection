# Nito.DependencyInjection.InstanceBuilder

`Nito.DependencyInjection.InstanceBuilder` is a convenient API for .NET Core's Dependency Injection, making some scenarios easier to do than the API normally provides.

# Usage

`InstanceBuilder` is a fluent API for factory methods which can pull some arguments specific for that instance, and pull other arguments out of the service provider. To use `InstanceBuilder`, create an instance builder by passing the service provider and the service collection. E.g., if you wanted to use `InstanceBuilder` to build a singleton instance, you would start with `service.AddSingleton(provider => new InstanceBuilder(provider, services).` and then use the fluent API from there.

Fluent API methods include:

- `With(params object[])` to inject constructor arguments that are specific to this instance.
- `WithNamedOptions<TOptions>(string name)` to inject an `IOptions<TOptions>` constructor argument that is specific to this instance. The options should be configured as a named option.
- `WithMany<T>(params Func<InstanceBuilder, T>[] factoryFunctions)` to inject a single constructor argument that is a collection of dependencies specific to this instance. Each factory function gets its own `InstanceBuilder` to generate the dependency.
- `WithMany<TSource, T>(IEnumerable<TSource> source, Func<InstanceBuilder, TSource, T> function)` to inject a single constructor argument that is a mapping of some source collection to a collection of dependencies specific to this instance.
- `Build<T>` takes all the instance-specific injected constructor arguments, combines them with the service provider, and constructs the actual `T` instance.

As a reminder, `InstanceBuilder` injects constructor arguments either from instance-specific values (`With*`) *or* pulling them from the service provider. So this means that unspecified dependencies (including dependencies of dependencies, etc) are all resolved through the service provider.

`InstanceBuilder` supports singleton and scoped lifetimes. *Technically* it supports transient lifetimes as well, but I'm not aware of any use case for transient lifetimes with `InstanceBuilder`. If you have one, please open an issue and let me know!

# Use Cases

## Multiple Instances, Different Options

This is when you have a service defined taking just a normal `IOptions<MyServiceOptions>` argument:

```C#
private sealed class MyService
{
    public MyService(IOptions<MyServiceOptions> options)
    {
        ...
    }
}
```

But you want two different instances, each with their own options. This is most common with singletons. `Nito.DependencyInjection.InstanceBuilder` makes this simple:

```C#
// For each "services" configuration defined in my config:
foreach (var serviceConfig in config.GetSection("services").GetChildren())
{
    // Configure options for that service
    services.Configure<MyServiceOptions>(serviceConfig.Path, serviceConfig);

    // Register an instance of that service as a singleton
    services.AddSingleton(provider => new InstanceBuilder(provider, services)
        .WithNamedOptions<MyServiceOptions>(serviceConfig.Path)
        .Build<MyService>());
}
```

Note that `MyService` can also have other dependencies, which are resolved automatically by the service provider without any changes to the above code.

## Sets of Handlers

This is when you have a service that takes a collection of dependencies (e.g., a set of "handlers"):

```C#
public sealed class Pipeline
{
    public Pipeline(IEnumerable<IHandler> handlers)
    {
        ...
    }
}
```

But you want different instances of your service, each with their own set of handlers:

```C#
// One Pipeline uses Handler0 and Handler1
services.AddSingleton(provider => new InstanceBuilder(provider, services)
    .WithMany(
        builder => builder.Build<Handler0>() as IHandler,
        builder => builder.Build<Handler1>() as IHandler)
    .Build<Pipeline>());

// The other Pipeline uses Handler2 and Handler3
services.AddSingleton(provider => new InstanceBuilder(provider, services)
    .WithMany(
        builder => builder.Build<Handler2>() as IHandler,
        builder => builder.Build<Handler3>() as IHandler)
    .Build<Pipeline>());
```

Note that `Pipeline` and any of the `Handler*` types can also have other dependencies, which are resolved automatically by the service provider without any changes to the above code.