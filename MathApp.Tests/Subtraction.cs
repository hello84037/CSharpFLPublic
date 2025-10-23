using Xunit;
using MathApp;

public class SubtractionTests
{
    [Fact]
    public void SubtractionTest()
    {
        System.IO.File.AppendAllText("SubtractionTests.SubtractionTest.coverage", "cba36ba0-a2e7-46c2-b379-e47d995d7087");
        int result = MathApp.MathOperations.Subtract(5, 3);
        System.IO.File.AppendAllText("SubtractionTests.SubtractionTest.coverage", "cba36ba0-a2e7-46c2-b379-e47d995d7087");
        Assert.Equal(2, result);
    }

    [Fact]
    public void SubtractionTest2()
    {
        System.IO.File.AppendAllText("SubtractionTests.SubtractionTest2.coverage", "4da2c1cc-9634-4c93-941a-97f1f28eb5a3");
        //int result = MathApp.MathOperations.Subtract(6, 4);
        int result = MathApp.MathOperations.Subtract(4, 6);
        System.IO.File.AppendAllText("SubtractionTests.SubtractionTest2.coverage", "4da2c1cc-9634-4c93-941a-97f1f28eb5a3");
        Assert.Equal(2, result);
    }

    [Fact]
    public void SubtractionTest3()
    {
        int result = MathApp.MathOperations.Subtract(9, 3);
        Assert.Equal(6, result);
    }
}