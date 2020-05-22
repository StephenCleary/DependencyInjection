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
// - I want two different instances of my service,
//   each with their own different set of options.

namespace Nito.DependencyInjection.Tests
{
    public class UseCase_Options
    {
        [Fact]
        public void MultipleSingletons_DifferentOptions()
        {
            /*
             * JSON equivalent:
             * {
             *   "services": [
             *     { "TestSetting": "first" },
             *     { "TestSetting": "second" }
             *   ]
             * }
             */
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    {"services:0:TestSetting", "first"},
                    {"services:1:TestSetting", "second"},
                })
                .Build();
            var services = new ServiceCollection();


            // *** Begin: code that belongs in startup ***
            foreach (var serviceConfig in config.GetSection("services").GetChildren())
            {
                // Configure options
                services.Configure<MyServiceOptions>(serviceConfig.Path, serviceConfig);

                // Register types
                services.AddSingleton(provider => new InstanceBuilder(provider, services)
                    .WithNamedOptions<MyServiceOptions>(serviceConfig.Path)
                    .Build<MyService>());
            }
            // *** End: code that belongs in startup ***


            var serviceProvider = services.BuildServiceProvider(SafeServiceProviderOptions);
            var myServices = serviceProvider.GetServices<MyService>().ToList();
            Assert.Equal("first", myServices[0].TestSetting);
            Assert.Equal("second", myServices[1].TestSetting);
        }

        private sealed class MyServiceOptions
        {
            public string TestSetting { get; set; }
        }

        private sealed class MyService
        {
            private readonly MyServiceOptions _options;

            public MyService(IOptions<MyServiceOptions> options)
            {
                _options = options.Value;
            }

            public string TestSetting => _options.TestSetting;
        }
    }
}
