using System;
using System.Collections.Generic;
using MathApp;
using Xunit;

namespace MathApp.Tests
{
    public class SeriesOperationsTests
    {
        [Fact]
        public void Fibonacci_GeneratesRequestedNumberOfTerms()
        {
            System.IO.File.AppendAllText("MathApp.Tests.SeriesOperationsTests.Fibonacci_GeneratesRequestedNumberOfTerms.coverage", "2f8bfe80-af64-4c3d-93e9-505c5d0547c4" + System.Environment.NewLine);
            IReadOnlyList<int> sequence = SeriesOperations.Fibonacci(6);
            System.IO.File.AppendAllText("MathApp.Tests.SeriesOperationsTests.Fibonacci_GeneratesRequestedNumberOfTerms.coverage", "65862540-0950-4997-93ed-058dcfae16b8" + System.Environment.NewLine);
            Assert.Equal(new[] { 0, 1, 1, 2, 3, 5 }, sequence);
        }

        [Fact]
        public void Fibonacci_ThrowsForNonPositiveTerms()
        {
            System.IO.File.AppendAllText("MathApp.Tests.SeriesOperationsTests.Fibonacci_ThrowsForNonPositiveTerms.coverage", "7c86a481-1961-48ce-a218-5dc84d2b1a33" + System.Environment.NewLine);
            Assert.Throws<ArgumentOutOfRangeException>(() => SeriesOperations.Fibonacci(0));
        }

        [Fact]
        public void SlidingWindowAverage_ComputesRunningAverages()
        {
            System.IO.File.AppendAllText("MathApp.Tests.SeriesOperationsTests.SlidingWindowAverage_ComputesRunningAverages.coverage", "57ed076a-9a0a-45fd-9881-11d07fa612ca" + System.Environment.NewLine);
            IReadOnlyList<double> values = new[]
            {
                3d,
                5d,
                10d,
                6d,
                7d,
                8d
            };
            System.IO.File.AppendAllText("MathApp.Tests.SeriesOperationsTests.SlidingWindowAverage_ComputesRunningAverages.coverage", "8bad3c5f-e49b-45d0-b29d-f113052da811" + System.Environment.NewLine);
            IReadOnlyList<double> averages = SeriesOperations.SlidingWindowAverage(values, 3);
            System.IO.File.AppendAllText("MathApp.Tests.SeriesOperationsTests.SlidingWindowAverage_ComputesRunningAverages.coverage", "a9ee8436-d3f6-4985-a97c-cfece5f11fb4" + System.Environment.NewLine);
            Assert.Equal(new[] { 6d, 7d, 23d / 3d, 7d }, averages);
        }

        [Fact]
        public void SlidingWindowAverage_ThrowsWhenWindowExceedsValues()
        {
            System.IO.File.AppendAllText("MathApp.Tests.SeriesOperationsTests.SlidingWindowAverage_ThrowsWhenWindowExceedsValues.coverage", "5a4e2103-2279-4a35-b98d-ed0916d6205b" + System.Environment.NewLine);
            IReadOnlyList<double> values = new[]
            {
                1d,
                2d
            };
            System.IO.File.AppendAllText("MathApp.Tests.SeriesOperationsTests.SlidingWindowAverage_ThrowsWhenWindowExceedsValues.coverage", "40f6bd34-bbe3-45bf-a308-c10108c2ff45" + System.Environment.NewLine);
            var exception = Assert.Throws<ArgumentException>(() => SeriesOperations.SlidingWindowAverage(values, 3));
            System.IO.File.AppendAllText("MathApp.Tests.SeriesOperationsTests.SlidingWindowAverage_ThrowsWhenWindowExceedsValues.coverage", "2fa69c46-397b-4b00-a227-0ac3981aacf3" + System.Environment.NewLine);
            Assert.Equal("windowSize", exception.ParamName);
            System.IO.File.AppendAllText("MathApp.Tests.SeriesOperationsTests.SlidingWindowAverage_ThrowsWhenWindowExceedsValues.coverage", "645e0e84-560e-4b15-9595-f2f1caef8e81" + System.Environment.NewLine);
            Assert.StartsWith("Window size cannot exceed the number of values.", exception.Message);
        }

        [Fact]
        public void WeightedAverage_CalculatesExpectedValue()
        {
            System.IO.File.AppendAllText("MathApp.Tests.SeriesOperationsTests.WeightedAverage_CalculatesExpectedValue.coverage", "87f6b65e-067b-4e11-ad37-123d61511142" + System.Environment.NewLine);
            IReadOnlyList<double> values = new[]
            {
                80d,
                90d,
                70d
            };
            System.IO.File.AppendAllText("MathApp.Tests.SeriesOperationsTests.WeightedAverage_CalculatesExpectedValue.coverage", "75de1cf4-bd74-4e3d-b8f5-018937ddf265" + System.Environment.NewLine);
            IReadOnlyList<double> weights = new[]
            {
                0.2d,
                0.5d,
                0.3d
            };
            System.IO.File.AppendAllText("MathApp.Tests.SeriesOperationsTests.WeightedAverage_CalculatesExpectedValue.coverage", "d8d08669-855e-451e-9486-ac9219f25c0e" + System.Environment.NewLine);
            double weightedAverage = SeriesOperations.WeightedAverage(values, weights);
            System.IO.File.AppendAllText("MathApp.Tests.SeriesOperationsTests.WeightedAverage_CalculatesExpectedValue.coverage", "e8b06631-1ee5-44cf-aaf4-ea347b55c3fe" + System.Environment.NewLine);
            Assert.Equal(82d, weightedAverage, precision: 10);
        }

        [Fact]
        public void WeightedAverage_ThrowsWhenWeightsDoNotMatchValues()
        {
            System.IO.File.AppendAllText("MathApp.Tests.SeriesOperationsTests.WeightedAverage_ThrowsWhenWeightsDoNotMatchValues.coverage", "2d64603b-cd75-450e-ad8c-acf6d67941df" + System.Environment.NewLine);
            IReadOnlyList<double> values = new[]
            {
                1d,
                2d,
                3d
            };
            System.IO.File.AppendAllText("MathApp.Tests.SeriesOperationsTests.WeightedAverage_ThrowsWhenWeightsDoNotMatchValues.coverage", "23f36955-55f9-430f-850d-f295ae64c4cf" + System.Environment.NewLine);
            IReadOnlyList<double> weights = new[]
            {
                0.5d,
                0.5d
            };
            System.IO.File.AppendAllText("MathApp.Tests.SeriesOperationsTests.WeightedAverage_ThrowsWhenWeightsDoNotMatchValues.coverage", "bab1d4c8-bb92-4e5a-8af1-39c7841f9f58" + System.Environment.NewLine);
            var exception = Assert.Throws<ArgumentException>(() => SeriesOperations.WeightedAverage(values, weights));
            System.IO.File.AppendAllText("MathApp.Tests.SeriesOperationsTests.WeightedAverage_ThrowsWhenWeightsDoNotMatchValues.coverage", "ce09c2f5-2e63-42e2-87ae-7a15d5294a72" + System.Environment.NewLine);
            Assert.Equal("Values and weights must contain the same number of items.", exception.Message);
        }
    }
}