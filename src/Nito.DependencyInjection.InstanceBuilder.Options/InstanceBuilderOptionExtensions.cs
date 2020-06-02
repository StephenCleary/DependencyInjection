using Microsoft.Extensions.Options;
using System;
using Microsoft.Extensions.DependencyInjection;

namespace Nito.DependencyInjection
{
    /// <summary>
    /// Extension methods for <see cref="InstanceBuilder"/>.
    /// </summary>
    public static class InstanceBuilderOptionExtensions
    {
        /// <summary>
        /// Injects an instance of type <c>IOptions&lt;TOptions&gt;</c> for this instance builder. The options are referenced by name.
        /// </summary>
        /// <typeparam name="TOptions">The options type.</typeparam>
        /// <param name="builder">The instance builder.</param>
        /// <param name="name">The name of the options.</param>
        public static InstanceBuilder WithNamedOptions<TOptions>(this IInstanceBuilder builder, string name)
            where TOptions : class, new()
        {
            _ = builder ?? throw new ArgumentNullException(nameof(builder));
            return builder.With(new OptionsWrapper<TOptions>(builder.Provider.GetRequiredService<IOptionsMonitor<TOptions>>().Get(name)));
        }

        /// <summary>
        /// Injects an instance of type <c>IOptions&lt;TOptions&gt;</c> for this instance builder. The options are created by a factory function.
        /// </summary>
        /// <typeparam name="TOptions">The options type.</typeparam>
        /// <param name="builder">The instance builder.</param>
        /// <param name="factoryFunction">The factory function used to create the options.</param>
        public static InstanceBuilder WithOptions<TOptions>(this InstanceBuilder builder, Func<TOptions> factoryFunction)
            where TOptions : class, new()
        {
            _ = builder ?? throw new ArgumentNullException(nameof(builder));
            _ = factoryFunction ?? throw new ArgumentNullException(nameof(factoryFunction));
            return builder.With(new OptionsWrapper<TOptions>(factoryFunction()));
        }
    }
}
