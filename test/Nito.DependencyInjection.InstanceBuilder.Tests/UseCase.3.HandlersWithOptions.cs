using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Extensions.Configuration;
using Xunit;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;
using static Utility;

// Use case covered in this file:
// - I want any number of different instances of my service,
//   each with their own different set of dependencies (handlers).
// - Each handler dependency takes the same kind of options.

namespace Nito.DependencyInjection.Tests
{
    public class UseCase_HandlersWithOptions
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
             *         { "TestSetting": "first" },
             *         { "TestSetting": "second" }
             *       ]
             *     },
             *     {
             *       "name": "SecondPipeline",
             *       "handlers": [
             *         { "TestSetting": "third" },
             *         { "TestSetting": "fourth" }
             *       ]
             *     }
             *   ]
             * }
             */
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    {"pipelines:0:name", "FirstPipeline"},
                    {"pipelines:0:handlers:0:TestSetting", "first"},
                    {"pipelines:0:handlers:1:TestSetting", "second"},
                    {"pipelines:1:name", "SecondPipeline"},
                    {"pipelines:1:handlers:0:TestSetting", "third"},
                    {"pipelines:1:handlers:1:TestSetting", "fourth"},
                })
                .Build();
            var services = new ServiceCollection();


            // *** Begin: code that belongs in startup ***
            foreach (var pipelineConfig in config.GetSection("pipelines").GetChildren())
            {
                var handlerConfigurations = pipelineConfig.GetSection("handlers").GetChildren().ToList();

                // Configure options for pipeline and handlers
                services.Configure<PipelineOptions>(pipelineConfig.Path, pipelineConfig);
                foreach (var handlerConfig in handlerConfigurations)
                    services.Configure<HandlerOptions>(handlerConfig.Path, handlerConfig);

                // Then register types
                services.AddSingleton(provider => new InstanceBuilder(provider, services)
                    .WithNamedOptions<PipelineOptions>(pipelineConfig.Path) // Pipeline gets its options
                    .WithMany(handlerConfigurations, (builder, handlerConfig) =>
                        builder
                            .WithNamedOptions<HandlerOptions>(handlerConfig.Path) // Each handler gets its own options
                            .Build<Handler>())
                    .Build<Pipeline>());
            }
            // *** End: code that belongs in startup ***


            var serviceProvider = services.BuildServiceProvider(SafeServiceProviderOptions);
            var pipelines = serviceProvider.GetServices<Pipeline>().ToList();
            Assert.Equal("FirstPipeline", pipelines[0].Name);
            Assert.Equal("SecondPipeline", pipelines[1].Name);
            Assert.Equal(new[] { "first", "second" }, pipelines[0].Process());
            Assert.Equal(new[] { "third", "fourth" }, pipelines[1].Process());
        }

        public sealed class HandlerOptions
        {
            public string TestSetting { get; set; }
        }

        public interface IHandler
        {
            string Handle();
        }

        public sealed class Handler : IHandler
        {
            private readonly HandlerOptions _options;
            public Handler(IOptions<HandlerOptions> options) => _options = options.Value;
            public string Handle() => _options.TestSetting;
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
