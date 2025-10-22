using Xunit;
using MathApp;

public class AdditionTests
{
    [Fact]
    public void AdditionTest()
    {
        System.IO.File.AppendAllText("AdditionTests.AdditionTest.coverage", "eb160241-f975-4935-9d76-cfbbf9f2180a");
        int result = MathApp.MathOperations.Add(5, 3);
        System.IO.File.AppendAllText("AdditionTests.AdditionTest.coverage", "eb160241-f975-4935-9d76-cfbbf9f2180a");
        Assert.Equal(8, result);
    }

    [Fact]
    public void AdditionTest2()
    {
        System.IO.File.AppendAllText("AdditionTests.AdditionTest2.coverage", "db8749c9-69fa-44b4-95ae-f0149087aea9");
        int result = MathApp.MathOperations.Add(4, 3);
        System.IO.File.AppendAllText("AdditionTests.AdditionTest2.coverage", "db8749c9-69fa-44b4-95ae-f0149087aea9");
        Assert.Equal(7, result);
    }

    [Fact]
    public void AdditionTest3()
    {
        int result = MathApp.MathOperations.Add(7, 3);
        Assert.Equal(10, result);
    }
}