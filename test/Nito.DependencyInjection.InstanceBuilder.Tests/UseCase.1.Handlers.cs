﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using static Utility;

// Use case covered in this file:
// - I want two different instances of my service,
//   each with their own different set of dependencies (handlers).

namespace Nito.DependencyInjection.Tests
{
    public class UseCase_Handlers
    {
        [Fact]
        public void Handlers()
        {
            var services = new ServiceCollection();


            // We should use `WithMany` instead of `With` because `Pipeline` takes a collection.
            //  (it's also possible to use `With`, but you'd have to return a collection type)

            // *** Begin: code that belongs in startup ***
            services.AddSingleton<ICommonDependency, CommonDependency>();
            services.AddSingleton(provider => new InstanceBuilder(provider, services)
                .WithMany(
                    builder => builder.Build<Handler0>() as IHandler,
                    builder => builder.Build<Handler1>() as IHandler)
                .Build<Pipeline>());
            services.AddSingleton(provider => new InstanceBuilder(provider, services)
                .WithMany(
                    builder => builder.Build<Handler2>() as IHandler,
                    builder => builder.Build<Handler3>() as IHandler)
                .Build<Pipeline>());
            // *** End: code that belongs in startup ***


            var serviceProvider = services.BuildServiceProvider(SafeServiceProviderOptions);
            var pipelines = serviceProvider.GetServices<Pipeline>().ToList();
            Assert.Equal(new[] { 0, 1 }, pipelines[0].Process());
            Assert.Equal(new[] { 2, 3 }, pipelines[1].Process());
        }

        public interface ICommonDependency
        {
        }

        public sealed class CommonDependency : ICommonDependency
        {
        }

        public interface IHandler
        {
            int Handle();
        }

        public sealed class Handler0 : IHandler
        {
            public Handler0(ICommonDependency dependency) {}
            public int Handle() => 0;
        }

        public sealed class Handler1 : IHandler
        {
            public int Handle() => 1;
        }

        public sealed class Handler2 : IHandler
        {
            public Handler2(ICommonDependency dependency) { }
            public int Handle() => 2;
        }

        public sealed class Handler3 : IHandler
        {
            public int Handle() => 3;
        }

        public sealed class Pipeline
        {
            private readonly IReadOnlyCollection<IHandler> _handlers;

            public Pipeline(IEnumerable<IHandler> handlers)
            {
                _handlers = handlers.ToList();
            }

            public List<int> Process() => _handlers.Select(handler => handler.Handle()).ToList();
        }
    }
}
