using Xunit;
using MathApp;

public class AdditionTests
{
    [Fact]
    public void AdditionTest()
    {
        System.IO.File.AppendAllText("AdditionTests.AdditionTest.coverage", "eda9e673-f280-4aab-8c17-924f5dc0995b");
        int result = MathApp.MathOperations.Add(5, 3);
        System.IO.File.AppendAllText("AdditionTests.AdditionTest.coverage", "eda9e673-f280-4aab-8c17-924f5dc0995b");
        Assert.Equal(8, result);
    }

    [Fact]
    public void AdditionTest2()
    {
        System.IO.File.AppendAllText("AdditionTests.AdditionTest2.coverage", "b8bbb953-fe17-4478-b81f-e1a2cd84c7b1");
        int result = MathApp.MathOperations.Add(4, 3);
        System.IO.File.AppendAllText("AdditionTests.AdditionTest2.coverage", "b8bbb953-fe17-4478-b81f-e1a2cd84c7b1");
        Assert.Equal(7, result);
    }

    [Fact]
    public void AdditionTest3()
    {
        int result = MathApp.MathOperations.Add(7, 3);
        Assert.Equal(10, result);
    }
}