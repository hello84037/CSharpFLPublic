using Xunit;
using MathApp;

public class SubtractionTests
{
    [Fact]
    public void SubtractionTest()
    {
        System.IO.File.AppendAllText("SubtractionTests.SubtractionTest.coverage", "635a5245-e6d3-4dfd-aa88-7b0ae49aa237");
        int result = MathApp.MathOperations.Subtract(5, 3);
        System.IO.File.AppendAllText("SubtractionTests.SubtractionTest.coverage", "635a5245-e6d3-4dfd-aa88-7b0ae49aa237");
        Assert.Equal(2, result);
    }

    [Fact]
    public void SubtractionTest2()
    {
        System.IO.File.AppendAllText("SubtractionTests.SubtractionTest2.coverage", "90eacdf1-863f-4d26-aa2a-ac061f25f291");
        //int result = MathApp.MathOperations.Subtract(6, 4);
        int result = MathApp.MathOperations.Subtract(4, 6);
        System.IO.File.AppendAllText("SubtractionTests.SubtractionTest2.coverage", "90eacdf1-863f-4d26-aa2a-ac061f25f291");
        Assert.Equal(2, result);
    }

    [Fact]
    public void SubtractionTest3()
    {
        int result = MathApp.MathOperations.Subtract(9, 3);
        Assert.Equal(6, result);
    }
}