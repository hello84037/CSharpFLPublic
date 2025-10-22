using Xunit;
using MathApp;

public class SubtractionTests
{
    [Fact]
    public void SubtractionTest()
    {
        System.IO.File.AppendAllText("SubtractionTests.SubtractionTest.coverage", "dbe335d2-bae8-4660-92ec-189f445f9a5b");
        int result = MathApp.MathOperations.Subtract(5, 3);
        System.IO.File.AppendAllText("SubtractionTests.SubtractionTest.coverage", "dbe335d2-bae8-4660-92ec-189f445f9a5b");
        Assert.Equal(2, result);
    }

    [Fact]
    public void SubtractionTest2()
    {
        System.IO.File.AppendAllText("SubtractionTests.SubtractionTest2.coverage", "74bc2c10-6480-4d90-aaba-4095d4bd1d99");
        //int result = MathApp.MathOperations.Subtract(6, 4);
        int result = MathApp.MathOperations.Subtract(4, 6);
        System.IO.File.AppendAllText("SubtractionTests.SubtractionTest2.coverage", "74bc2c10-6480-4d90-aaba-4095d4bd1d99");
        Assert.Equal(2, result);
    }

    [Fact]
    public void SubtractionTest3()
    {
        int result = MathApp.MathOperations.Subtract(9, 3);
        Assert.Equal(6, result);
    }
}