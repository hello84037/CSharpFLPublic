using Xunit;
using MathApp;

public class AdditionTests
{
    [Fact]
    public void AdditionTest()
    {
        System.IO.File.AppendAllText("AdditionTests.AdditionTest.coverage", "06bf029d-b29a-4817-9277-7fa55846d97c");
        int result = MathApp.MathOperations.Add(5, 3);
        System.IO.File.AppendAllText("AdditionTests.AdditionTest.coverage", "06bf029d-b29a-4817-9277-7fa55846d97c");
        Assert.Equal(8, result);
    }

    [Fact]
    public void AdditionTest2()
    {
        System.IO.File.AppendAllText("AdditionTests.AdditionTest2.coverage", "fffaa8b5-3b9b-4026-964d-807c101bac2c");
        int result = MathApp.MathOperations.Add(4, 3);
        System.IO.File.AppendAllText("AdditionTests.AdditionTest2.coverage", "fffaa8b5-3b9b-4026-964d-807c101bac2c");
        Assert.Equal(7, result);
    }

    [Fact]
    public void AdditionTest3()
    {
        int result = MathApp.MathOperations.Add(7, 3);
        Assert.Equal(10, result);
    }
}