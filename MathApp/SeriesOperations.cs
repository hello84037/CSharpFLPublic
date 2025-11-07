namespace MathApp
{
    public static class SeriesOperations
    {
        public static IReadOnlyList<int> Fibonacci(int terms)
        {
            if (terms <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(terms), "Number of terms must be positive.");
            }
            if (terms == 1)
            {
                return new List<int>
                {
                    0
                };
            }
            var sequence = new List<int>
            {
                0,
                1
            };
            while (sequence.Count < terms)
            {
                int nextValue = sequence[^1] + sequence[^2];
                sequence.Add(nextValue);
            }
            return sequence;
        }

        public static IReadOnlyList<double> SlidingWindowAverage(IReadOnlyList<double> values, int windowSize)
        {
            if (values == null)
            {
                throw new ArgumentNullException(nameof(values));
            }
            if (windowSize <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(windowSize), "Window size must be positive.");
            }
            if (values.Count < windowSize)
            {
                throw new ArgumentException("Window size cannot exceed the number of values.", nameof(windowSize));
            }
            var result = new List<double>();
            double windowSum = 0;
            for (int i = 0; i < values.Count; i++)
            {
                windowSum += values[i];
                if (i >= windowSize)
                {
                    windowSum -= values[i - windowSize];
                }
                if (i >= windowSize - 1)
                {
                    result.Add(windowSum / windowSize);
                }
            }
            return result;
        }

        public static double WeightedAverage(IReadOnlyList<double> values, IReadOnlyList<double> weights)
        {
            if (values == null)
            {
                throw new ArgumentNullException(nameof(values));
            }
            if (weights == null)
            {
                throw new ArgumentNullException(nameof(weights));
            }
            if (values.Count == 0)
            {
                throw new ArgumentException("Values cannot be empty.", nameof(values));
            }
            if (values.Count != weights.Count)
            {
                throw new ArgumentException("Values and weights must contain the same number of items.");
            }
            double weightedSum = 0;
            double weightTotal = 0;
            for (int i = 0; i < values.Count; i++)
            {
                weightedSum += values[i] * weights[i];
                weightTotal += weights[i];
            }
            if (weightTotal == 0)
            {
                throw new ArgumentException("Sum of weights must be greater than zero.", nameof(weights));
            }
            return weightedSum / weightTotal;
        }
    }
}