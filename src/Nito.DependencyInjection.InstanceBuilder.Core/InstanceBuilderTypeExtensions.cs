using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

// General rules for writing extensions:
// 1) Extend IInstanceBuilder, not InstanceBuilder.
// 2) Name extensions "With*", where "*" is a non-zero-length string.
// 3) Do not expose IInstanceBuilder to end-users; always give them an InstanceBuilder.
// 4) Tip: You probably want to Copy the InstanceBuilder before exposing it to end-users, for two reasons:
//    A) Copies prevent additional injected instances from flowing to the source InstanceBuilder.
//    B) Copies do not have injected instances by default.

namespace Nito.DependencyInjection
{
    /// <summary>
    /// Extension methods for <see cref="InstanceBuilder"/>.
    /// </summary>
    public static class InstanceBuilderTypeExtensions
    {
        /// <summary>
        /// Injects multiple instances of <typeparamref name="T"/> for this instance builder, one per factory function passed to this method.
        /// </summary>
        /// <typeparam name="T">The type of instance to create.</typeparam>
        /// <param name="builder">The instance builder.</param>
        /// <param name="factoryFunctions">A series of factory functions.</param>
        public static InstanceBuilder WithMany<T>(this IInstanceBuilder builder, params Func<InstanceBuilder, T>[] factoryFunctions)
        {
            _ = builder ?? throw new ArgumentNullException(nameof(builder));
            _ = factoryFunctions ?? throw new ArgumentNullException(nameof(factoryFunctions));
            return builder.With(factoryFunctions.Select(factoryFunction => factoryFunction(builder.Copy())).ToList());
        }

        /// <summary>
        /// Injects multiple instances of <typeparamref name="T"/> for this instance builder, one per item in the source sequence, via the factory function passed to this method.
        /// </summary>
        /// <typeparam name="TSource">The type of items in the source sequence.</typeparam>
        /// <typeparam name="T">The type of instance to create.</typeparam>
        /// <param name="builder">The instance builder.</param>
        /// <param name="source">The source sequence.</param>
        /// <param name="function">The factory function that transforms a source sequence item to an instance of <typeparamref name="T"/>.</param>
        public static InstanceBuilder WithMany<TSource, T>(this IInstanceBuilder builder, IEnumerable<TSource> source, Func<InstanceBuilder, TSource, T> function)
        {
            _ = builder ?? throw new ArgumentNullException(nameof(builder));
            _ = source ?? throw new ArgumentNullException(nameof(source));
            _ = function ?? throw new ArgumentNullException(nameof(function));
            return builder.With(source.Select(x => function(builder.Copy(), x)).ToList());
        }
    }
}
