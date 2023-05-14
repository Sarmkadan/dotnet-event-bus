using System;
using System.Threading.Tasks;

namespace DotnetEventBus.Benchmarks
{
    public static class EventBusBenchmarksExtensions
    {
        /// <summary>
        /// Creates a new EventBusBenchmarks instance with the specified identifier and name.
        /// </summary>
        /// <param name="id">The unique identifier for this benchmark instance.</param>
        /// <param name="name">The descriptive name for this benchmark.</param>
        /// <returns>A new EventBusBenchmarks instance configured with the provided values.</returns>
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
        /// <returns>The benchmark instance after setup.</returns>
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
        /// <returns>The benchmark instance after cleanup.</returns>
        public static EventBusBenchmarks WithCleanup(this EventBusBenchmarks benchmarks)
        {
            if (benchmarks == null)
                throw new ArgumentNullException(nameof(benchmarks));

            benchmarks.GlobalCleanup();
            return benchmarks;
        }
    }
}