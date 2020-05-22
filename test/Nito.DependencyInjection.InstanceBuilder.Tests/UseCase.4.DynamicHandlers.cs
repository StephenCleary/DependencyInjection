using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Extensions.Configuration;
using Xunit;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;
using static Utility;

// Use cases covered in this file:
// - I want two different instances of my service,
//   each with their own different set of dependencies (handlers).
// - Each handler dependency takes different options.
// - The type of the dependencies is determined by configuration settings.

// NOTE: If you are doing this, rethink your design.
// Defining types as strings in configuration files never ends well.

namespace Nito.DependencyInjection.Tests
{
    public class UseCase_DynamicHandlers
    {
        [Fact]
        public void Handlers()
        {
            /*
             * JSON equivalent:
             * {
             *   "pipelines": [
             *     {
             *       "name": "FirstPipeline",
             *       "handlers": [
             *         { "Type": "Handler0", "Settings": { "Handler0Setting": "first" } },
             *         { "Type": "Handler1", "Settings": { "Handler1Setting": "second" } }
             *       ]
             *     },
             *     {
             *       "name": "SecondPipeline",
             *       "handlers": [
             *         { "Type": "Handler2" },
             *         { "Type": "Handler3", "Settings": { "Handler3Setting": "fourth" } }
             *       ]
             *     }
             *   ]
             * }
             */
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    {"pipelines:0:name", "FirstPipeline"},
                    {"pipelines:0:handlers:0:Type", "Handler0"},
                    {"pipelines:0:handlers:0:Settings:Handler0Setting", "first"},
                    {"pipelines:0:handlers:1:Type", "Handler1"},
                    {"pipelines:0:handlers:1:Settings:Handler1Setting", "second"},
                    {"pipelines:1:name", "SecondPipeline"},
                    {"pipelines:1:handlers:0:Type", "Handler2"},
                    {"pipelines:1:handlers:1:Type", "Handler3"},
                    {"pipelines:1:handlers:1:Settings:Handler3Setting", "fourth"},
                })
                .Build();
            var services = new ServiceCollection();


            // *** Begin: code that belongs in startup ***
            foreach (var pipelineConfig in config.GetSection("pipelines").GetChildren())
            {
                var handlerConfigurations = pipelineConfig.GetSection("handlers").GetChildren()
                    .Select(handlerConfig => (HandlerConfig: handlerConfig.GetSection("Settings"), HandlerTypeName: handlerConfig["Type"]))
                    .ToList();

                // Configure pipeline and handler options
                services.Configure<PipelineOptions>(pipelineConfig.Path, pipelineConfig);
                foreach (var (handlerConfig, handlerTypeName) in handlerConfigurations)
                {
                    _ = handlerTypeName switch
                    {
                        "Handler0" => services.Configure<Handler0Options>(handlerConfig.Path, handlerConfig),
                        "Handler1" => services.Configure<Handler1Options>(handlerConfig.Path, handlerConfig),
                        "Handler2" => services, // Handler2 has no options
                        "Handler3" => services.Configure<Handler3Options>(handlerConfig.Path, handlerConfig),
                        _ => throw new NotImplementedException($"Unknown handler type {handlerTypeName}"),
                    };
                }

                // Register types
                services.AddSingleton(provider => new InstanceBuilder(provider, services)
                    .WithNamedOptions<PipelineOptions>(pipelineConfig.Path) // Pipeline gets its options
                    .WithMany(handlerConfigurations, (builder, x) =>
                        x.HandlerTypeName switch
                        {
                            // Each handler gets its options
                            "Handler0" => builder.WithNamedOptions<Handler0Options>(x.HandlerConfig.Path).Build<Handler0>() as IHandler,
                            "Handler1" => builder.WithNamedOptions<Handler1Options>(x.HandlerConfig.Path).Build<Handler1>() as IHandler,
                            "Handler2" => builder.Build<Handler2>() as IHandler, // Handler2 has no options
                            "Handler3" => builder.WithNamedOptions<Handler3Options>(x.HandlerConfig.Path).Build<Handler3>() as IHandler,
                            _ => throw new NotImplementedException($"Unknown handler type {x.HandlerTypeName}"),
                        }
                    )
                    .Build<Pipeline>());
            }
            // *** End: code that belongs in startup ***


            var serviceProvider = services.BuildServiceProvider(SafeServiceProviderOptions);
            var pipelines = serviceProvider.GetServices<Pipeline>().ToList();
            Assert.Equal("FirstPipeline", pipelines[0].Name);
            Assert.Equal("SecondPipeline", pipelines[1].Name);
            Assert.Equal(new[] { "first", "second" }, pipelines[0].Process());
            Assert.Equal(new[] { "Handler2 has no options", "fourth" }, pipelines[1].Process());
        }

        public sealed class Handler0Options
        {
            public string Handler0Setting { get; set; }
        }

        public sealed class Handler1Options
        {
            public string Handler1Setting { get; set; }
        }

        public sealed class Handler3Options
        {
            public string Handler3Setting { get; set; }
        }

        public interface IHandler
        {
            string Handle();
        }

        public sealed class Handler0 : IHandler
        {
            private readonly Handler0Options _options;
            public Handler0(IOptions<Handler0Options> options) => _options = options.Value;
            public string Handle() => _options.Handler0Setting;
        }

        public sealed class Handler1 : IHandler
        {
            private readonly Handler1Options _options;
            public Handler1(IOptions<Handler1Options> options) => _options = options.Value;
            public string Handle() => _options.Handler1Setting;
        }

        public sealed class Handler2 : IHandler
        {
            public string Handle() => "Handler2 has no options";
        }

        public sealed class Handler3 : IHandler
        {
            private readonly Handler3Options _options;
            public Handler3(IOptions<Handler3Options> options) => _options = options.Value;
            public string Handle() => _options.Handler3Setting;
        }

        public sealed class PipelineOptions
        {
            public string Name { get; set; }
        }

        public sealed class Pipeline
        {
            private readonly PipelineOptions _options;
            private readonly IReadOnlyCollection<IHandler> _handlers;

            public Pipeline(IEnumerable<IHandler> handlers, IOptions<PipelineOptions> options)
            {
                _handlers = handlers.ToList();
                _options = options.Value;
            }

            public string Name => _options.Name;
            public List<string> Process() => _handlers.Select(handler => handler.Handle()).ToList();
        }
    }
}
