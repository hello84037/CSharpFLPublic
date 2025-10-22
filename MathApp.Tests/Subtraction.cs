using Xunit;
using MathApp;

public class SubtractionTests
{
    [Fact]
    public void SubtractionTest()
    {
        System.IO.File.AppendAllText("SubtractionTests.SubtractionTest.coverage", "7d0799ba-5318-4550-94a4-a78e6dbc345f");
        int result = MathApp.MathOperations.Subtract(5, 3);
        System.IO.File.AppendAllText("SubtractionTests.SubtractionTest.coverage", "7d0799ba-5318-4550-94a4-a78e6dbc345f");
        Assert.Equal(2, result);
    }

    [Fact]
    public void SubtractionTest2()
    {
        System.IO.File.AppendAllText("SubtractionTests.SubtractionTest2.coverage", "9738f1fb-0ff0-4752-9492-738195638edf");
        //int result = MathApp.MathOperations.Subtract(6, 4);
        int result = MathApp.MathOperations.Subtract(4, 6);
        System.IO.File.AppendAllText("SubtractionTests.SubtractionTest2.coverage", "9738f1fb-0ff0-4752-9492-738195638edf");
        Assert.Equal(2, result);
    }

    [Fact]
    public void SubtractionTest3()
    {
        int result = MathApp.MathOperations.Subtract(9, 3);
        Assert.Equal(6, result);
    }
}