using Xunit;
using MathApp;

public class DivisionTests
{
    [Fact]
    public void DivisionTest()
    {
        double result = MathApp.MathOperations.Divide(10, 2);
        Assert.Equal(5.0, result);
    }

    [Fact]
    public void DivisionTest2()
    {
        double result = MathApp.MathOperations.Divide(15, 2);
        Assert.Equal(7.5, result);
    }

    [Fact]
    public void DivisionTest3()
    {
        double result = MathApp.MathOperations.Divide(150, 2);
        Assert.Equal(75.0, result);
    }
}