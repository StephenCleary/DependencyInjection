using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;
using Microsoft.Extensions.DependencyInjection;
using static Utility;

// Limitation covered in this file:
// - I want instances built by InstanceBuilder to be disposed at close of DI scope.
// - However, only instances passed to services.Add* will be disposed.

namespace Nito.DependencyInjection.Tests
{
    public class Limitation_Disposal
    {
        [Fact]
        public void ServiceAndDependencyRegistered_BothAreDisposed()
        {
            MyService.DisposeCount = 0;
            MyDependency.DisposeCount = 0;
            var services = new ServiceCollection();

            services.AddScoped<MyService>();
            services.AddScoped<MyDependency>();

            var serviceProvider = services.BuildServiceProvider(SafeServiceProviderOptions);
            using (var scope = serviceProvider.CreateScope())
                _ = scope.ServiceProvider.GetRequiredService<MyService>();
            Assert.Equal(1, MyService.DisposeCount);
            Assert.Equal(1, MyDependency.DisposeCount);
        }

        [Fact]
        public void DependencyRegisteredOnDemand_BothAreDisposed()
        {
            MyService.DisposeCount = 0;
            MyDependency.DisposeCount = 0;
            var services = new ServiceCollection();

            services.AddScoped<MyService>();
            services.AddScoped(provider => new InstanceBuilder(provider, services)
                .Build<MyDependency>());

            var serviceProvider = services.BuildServiceProvider(SafeServiceProviderOptions);
            using (var scope = serviceProvider.CreateScope())
                _ = scope.ServiceProvider.GetRequiredService<MyService>();
            Assert.Equal(1, MyService.DisposeCount);
            Assert.Equal(1, MyDependency.DisposeCount);
        }

        [Fact]
        public void BothRegisteredOnDemand_DependencyIsNotDisposed()
        {
            MyService.DisposeCount = 0;
            MyDependency.DisposeCount = 0;
            var services = new ServiceCollection();

            services.AddScoped(provider => new InstanceBuilder(provider, services)
                .WithMany(builder => builder.Build<MyDependency>())
                .Build<MyService>());

            var serviceProvider = services.BuildServiceProvider(SafeServiceProviderOptions);
            using (var scope = serviceProvider.CreateScope())
                _ = scope.ServiceProvider.GetRequiredService<MyService>();
            Assert.Equal(1, MyService.DisposeCount);
            Assert.Equal(0, MyDependency.DisposeCount); // dependency not disposed!
        }

        private sealed class MyDependency : IDisposable
        {
            private bool _disposed;

            [ThreadStatic] public static int DisposeCount;

            public void Dispose()
            {
                if (_disposed)
                    return;
                _disposed = true;
                ++DisposeCount;
            }
        }

        private sealed class MyService : IDisposable
        {
            public MyService(IEnumerable<MyDependency> dependencies)
            {
            }

            private bool _disposed;

            [ThreadStatic] public static int DisposeCount;

            public void Dispose()
            {
                if (_disposed)
                    return;
                _disposed = true;
                ++DisposeCount;
            }
        }
    }
}
