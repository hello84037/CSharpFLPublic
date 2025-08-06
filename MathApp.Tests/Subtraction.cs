using Xunit;
using MathApp;

public class SubtractionTests
{
    [Fact]
    public void SubtractionTest()
    {
        System.IO.File.AppendAllText("SubtractionTests.SubtractionTest.coverage", "92c3d384-a06d-4124-b3c6-d4fee508d2fa");
        int result = MathApp.MathOperations.Subtract(5, 3);
        System.IO.File.AppendAllText("SubtractionTests.SubtractionTest.coverage", "92c3d384-a06d-4124-b3c6-d4fee508d2fa");
        Assert.Equal(2, result);
    }

    [Fact]
    public void SubtractionTest2()
    {
        System.IO.File.AppendAllText("SubtractionTests.SubtractionTest2.coverage", "5015778f-01fd-4ea7-9184-e374ef55f7d5");
        int result = MathApp.MathOperations.Subtract(6, 4);
        System.IO.File.AppendAllText("SubtractionTests.SubtractionTest2.coverage", "5015778f-01fd-4ea7-9184-e374ef55f7d5");
        Assert.Equal(2, result);
    }

    [Fact]
    public void SubtractionTest3()
    {
        int result = MathApp.MathOperations.Subtract(9, 3);
        Assert.Equal(6, result);
    }
}