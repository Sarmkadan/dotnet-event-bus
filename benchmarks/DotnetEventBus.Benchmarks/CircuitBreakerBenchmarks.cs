[Benchmark]
        public async Task ExecuteCircuitBreakerAsync()
        {
            // Arrange
            var circuitBreaker = new CircuitBreaker(3, TimeSpan.FromMilliseconds(100));

            // Act
            await circuitBreaker.ExecuteAsync(async () =>
            {
                // Simulate a failing operation
                await Task.Delay(10);
                throw new Exception();
            });

            // Assert
            Assert.Throws<CircuitBreakerOpenException>(async () => circuitBreaker.ExecuteAsync(async () =>
            {
                // Simulate another failing operation
                await Task.Delay(10);
                throw new Exception();
            }));
        }
