using Xunit;
using MathApp;

public class AdditionTests
{
    [Fact]
    public void AdditionTest()
    {
        System.IO.File.AppendAllText("AdditionTests.AdditionTest.coverage", "195d79c1-1390-42cb-98a7-f948571b4a29");
        int result = MathApp.MathOperations.Add(5, 3);
        System.IO.File.AppendAllText("AdditionTests.AdditionTest.coverage", "195d79c1-1390-42cb-98a7-f948571b4a29");
        Assert.Equal(8, result);
    }

    [Fact]
    public void AdditionTest2()
    {
        System.IO.File.AppendAllText("AdditionTests.AdditionTest2.coverage", "25df85ef-9ca7-41a9-b673-59acc7f8381b");
        int result = MathApp.MathOperations.Add(4, 3);
        System.IO.File.AppendAllText("AdditionTests.AdditionTest2.coverage", "25df85ef-9ca7-41a9-b673-59acc7f8381b");
        Assert.Equal(7, result);
    }

    [Fact]
    public void AdditionTest3()
    {
        int result = MathApp.MathOperations.Add(7, 3);
        Assert.Equal(10, result);
    }
}