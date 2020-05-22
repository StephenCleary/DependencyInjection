using System;
using Microsoft.Extensions.DependencyInjection;

namespace Nito.DependencyInjection
{
    /// <summary>
    /// An interface for instance builders. This should be used when writing instance builder extensions.
    /// </summary>
    public interface IInstanceBuilder
    {
        /// <summary>
        /// Inject the argument(s) for this instance builder when <see cref="Build{TService}"/> is invoked.
        /// </summary>
        /// <param name="injectedArguments">The arguments to inject.</param>
        InstanceBuilder With(params object[] injectedArguments);

        /// <summary>
        /// Creates a copy of this instance builder with the same <see cref="Provider"/> and <see cref="Services"/>. Optionally copies the injected arguments.
        /// </summary>
        /// <param name="copyInjectedArguments">Whether to copy the injected arguments. Defaults to <c>false</c>.</param>
        InstanceBuilder Copy(bool copyInjectedArguments = false);

        /// <summary>
        /// Gets the provider used to resolve services.
        /// </summary>
        IServiceProvider Provider { get; }

        /// <summary>
        /// Gets the collection of services as they were defined in startup.
        /// </summary>
        IServiceCollection Services { get; }

        /// <summary>
        /// Builds an instance of type <typeparamref name="TService"/>, using the service provider and any injected arguments that have been defined for this instance builder.
        /// </summary>
        /// <typeparam name="TService">The type of instance to create.</typeparam>
        TService Build<TService>();

        /// <summary>
        /// Builds an instance of type <paramref name="instanceType"/>, using the service provider and any injected arguments that have been defined for this instance builder.
        /// </summary>
        /// <param name="instanceType">The type of instance to create.</param>
        object Build(Type instanceType);
    }
}