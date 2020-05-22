using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.DependencyInjection;

namespace Nito.DependencyInjection
{
    /// <summary>
    /// Provides a fluent API for constructing types.
    /// Each instance builder has a collection of injected arguments, which are used along with its service provider to create an instance when <see cref="Build{TService}"/> is invoked.
    /// </summary>
    public sealed class InstanceBuilder : IInstanceBuilder
    {
        private readonly IServiceProvider _provider;
        private readonly IServiceCollection _services;
        private readonly List<object> _injectedArguments;

        /// <summary>
        /// Creates a new instance builder, using the specified provider and service collection.
        /// </summary>
        /// <param name="provider">The provider, used to resolve constructor arguments that are not included in this instance builder as injected arguments.</param>
        /// <param name="services">The service collection.</param>
        public InstanceBuilder(IServiceProvider provider, IServiceCollection services)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
            _services = services ?? throw new ArgumentNullException(nameof(services));
            _injectedArguments = new List<object>();
        }

        /// <inheritdoc />
        public InstanceBuilder With(params object[] injectedArguments)
        {
            _ = injectedArguments ?? throw new ArgumentNullException(nameof(injectedArguments));
            _injectedArguments.AddRange(injectedArguments);
            return this;
        }

        InstanceBuilder IInstanceBuilder.Copy(bool copyInjectedArguments)
        {
            var result = new InstanceBuilder(_provider, _services);
            if (copyInjectedArguments)
                result._injectedArguments.AddRange(_injectedArguments);
            return result;
        }

        IServiceProvider IInstanceBuilder.Provider => _provider;
        IServiceCollection IInstanceBuilder.Services => _services;

        /// <inheritdoc />
        public TService Build<TService>() => ActivatorUtilities.CreateInstance<TService>(_provider, _injectedArguments.ToArray());

        object IInstanceBuilder.Build(Type instanceType)
        {
            _ = instanceType ?? throw new ArgumentNullException(nameof(instanceType));
            return ActivatorUtilities.CreateInstance(_provider, instanceType, _injectedArguments.ToArray());
        }
    }
}
