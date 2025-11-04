using Xunit;
using MathApp;

public class MultiplicationTests
{
    [Fact]
    public void MultiplicationTest()
    {
        double result = MathApp.MathOperations.Multiply(10, 2);
        Assert.Equal(20.0, result);
    }

    [Fact]
    public void MultiplicationTest2()
    {
        double result = MathApp.MathOperations.Multiply(15, 2);
        Assert.Equal(30.0, result);
    }

    [Fact]
    public void MultiplicationTest3()
    {
        double result = MathApp.MathOperations.Multiply(150, 2);
        Assert.Equal(300.0, result);
    }
}