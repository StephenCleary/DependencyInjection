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
            where TOptions : class, new() =>
            builder.With(new OptionsWrapper<TOptions>(builder.Provider.GetRequiredService<IOptionsMonitor<TOptions>>().Get(name)));

        private sealed class OptionsWrapper<T> : IOptions<T>
            where T : class, new()
        {
            public OptionsWrapper(T value) => Value = value;
            public T Value { get; }
        }
    }
}
