namespace MathApp.Tests
{
    public class SeriesOperationsTests
    {
        [Fact]
        public void Fibonacci_GeneratesRequestedNumberOfTerms()
        {
            IReadOnlyList<int> sequence = SeriesOperations.Fibonacci(6);

            Assert.Equal(new[] { 0, 1, 1, 2, 3, 5 }, sequence);
        }

        [Fact]
        public void Fibonacci_ThrowsForNonPositiveTerms()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => SeriesOperations.Fibonacci(0));
        }

        [Fact]
        public void SlidingWindowAverage_ComputesRunningAverages()
        {
            IReadOnlyList<double> values = new[] { 3d, 5d, 10d, 6d, 7d, 8d };

            IReadOnlyList<double> averages = SeriesOperations.SlidingWindowAverage(values, 3);

            Assert.Equal(new[] { 6d, 7d, 23d / 3d, 7d }, averages);
        }

        [Fact]
        public void SlidingWindowAverage_ThrowsWhenWindowExceedsValues()
        {
            IReadOnlyList<double> values = new[] { 1d, 2d };

            var exception = Assert.Throws<ArgumentException>(() => SeriesOperations.SlidingWindowAverage(values, 3));
            Assert.Equal("windowSize", exception.ParamName);
            Assert.StartsWith("Window size cannot exceed the number of values.", exception.Message);
        }

        [Fact]
        public void WeightedAverage_CalculatesExpectedValue()
        {
            IReadOnlyList<double> values = new[] { 80d, 90d, 70d };
            IReadOnlyList<double> weights = new[] { 0.2d, 0.5d, 0.3d };

            double weightedAverage = SeriesOperations.WeightedAverage(values, weights);

            Assert.Equal(82d, weightedAverage, precision: 10);
        }

        [Fact]
        public void WeightedAverage_ThrowsWhenWeightsDoNotMatchValues()
        {
            IReadOnlyList<double> values = new[] { 1d, 2d, 3d };
            IReadOnlyList<double> weights = new[] { 0.5d, 0.5d };

            var exception = Assert.Throws<ArgumentException>(() => SeriesOperations.WeightedAverage(values, weights));
            Assert.Equal("Values and weights must contain the same number of items.", exception.Message);
        }
    }
}
