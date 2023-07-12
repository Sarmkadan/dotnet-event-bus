using System;
using System.Threading.Tasks;

namespace DotnetEventBus.Benchmarks
{
    /// <summary>
    /// Provides extension methods for fluent configuration of <see cref="EventBusBenchmarks"/> instances.
    /// Enables chaining of setup, configuration, and cleanup operations in a fluent API style.
    /// </summary>
    public static class EventBusBenchmarksExtensions
    {
        /// <summary>
        /// Creates a new EventBusBenchmarks instance with the specified identifier and name.
        /// </summary>
        /// <param name="id">The unique identifier for this benchmark instance.</param>
        /// <param name="name">The descriptive name for this benchmark.</param>
        /// <returns>A new EventBusBenchmarks instance configured with the provided values.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="benchmarks"/> is <see langword="null"/></exception>
        public static EventBusBenchmarks WithIdentity(this EventBusBenchmarks benchmarks, string id, string name)
        {
            if (benchmarks == null)
                throw new ArgumentNullException(nameof(benchmarks));

            var result = new EventBusBenchmarks
            {
                Id = id ?? benchmarks.Id,
                Name = name ?? benchmarks.Name,
                Value = benchmarks.Value,
                Timestamp = benchmarks.Timestamp
            };

            return result;
        }

        /// <summary>
        /// Creates a copy of this EventBusBenchmarks instance with an updated Value.
        /// </summary>
        /// <param name="value">The new value to set.</param>
        /// <returns>A new EventBusBenchmarks instance with the updated value.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="benchmarks"/> is <see langword="null"/></exception>
        public static EventBusBenchmarks WithValue(this EventBusBenchmarks benchmarks, int value)
        {
            if (benchmarks == null)
                throw new ArgumentNullException(nameof(benchmarks));

            var result = new EventBusBenchmarks
            {
                Id = benchmarks.Id,
                Name = benchmarks.Name,
                Value = value,
                Timestamp = benchmarks.Timestamp
            };

            return result;
        }

        /// <summary>
        /// Executes the GlobalSetup method and returns the configured benchmark instance.
        /// This is useful for chaining setup operations in fluent scenarios.
        /// </summary>
        /// <param name="benchmarks">The benchmark instance to configure.</param>
        /// <returns>The benchmark instance after setup.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="benchmarks"/> is <see langword="null"/></exception>
        public static EventBusBenchmarks WithSetup(this EventBusBenchmarks benchmarks)
        {
            if (benchmarks == null)
                throw new ArgumentNullException(nameof(benchmarks));

            benchmarks.GlobalSetup();
            return benchmarks;
        }

        /// <summary>
        /// Executes the GlobalCleanup method and returns the configured benchmark instance.
        /// This is useful for chaining cleanup operations in fluent scenarios.
        /// </summary>
        /// <param name="benchmarks">The benchmark instance to configure.</param>
        /// <returns>The benchmark instance after cleanup.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="benchmarks"/> is <see langword="null"/></exception>
        public static EventBusBenchmarks WithCleanup(this EventBusBenchmarks benchmarks)
        {
            if (benchmarks == null)
                throw new ArgumentNullException(nameof(benchmarks));

            benchmarks.GlobalCleanup();
            return benchmarks;
        }
    }
}