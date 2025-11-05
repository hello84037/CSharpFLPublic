namespace MathApp.Tests
{
    public class MathOperationsTest
    {
        [Theory]
        [InlineData(10, 3, 1)]
        [InlineData(-10, 3, -1)]
        [InlineData(10, -3, 1)]
        [InlineData(-10, -3, -1)]
        [InlineData(5, 5, 0)]
        public void ComputesRemainderForValidDivisors(double dividend, double divisor, double expected)
        {
            double result = MathApp.MathOperations.Modulus(dividend, divisor);
            Assert.Equal(expected, result);
        }

        [Fact]
        public void ThrowsWhenDivisorIsZero()
        {
            Assert.Throws<ArgumentException>(() => MathApp.MathOperations.Modulus(10, 0));
        }
    }
}
