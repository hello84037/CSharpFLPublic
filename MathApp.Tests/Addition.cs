using Xunit;
using MathApp;

public class AdditionTests
{
    [Fact]
    public void AdditionTest()
    {
        System.IO.File.AppendAllText("AdditionTests.AdditionTest.coverage", "17291029-976c-47a8-a351-7c3ecc1f4a6f");
        int result = MathApp.MathOperations.Add(5, 3);
        System.IO.File.AppendAllText("AdditionTests.AdditionTest.coverage", "17291029-976c-47a8-a351-7c3ecc1f4a6f");
        Assert.Equal(8, result);
    }

    [Fact]
    public void AdditionTest2()
    {
        System.IO.File.AppendAllText("AdditionTests.AdditionTest2.coverage", "176977e7-764d-490b-97ca-5726038889a3");
        int result = MathApp.MathOperations.Add(4, 3);
        System.IO.File.AppendAllText("AdditionTests.AdditionTest2.coverage", "176977e7-764d-490b-97ca-5726038889a3");
        Assert.Equal(7, result);
    }

    [Fact]
    public void AdditionTest3()
    {
        int result = MathApp.MathOperations.Add(7, 3);
        Assert.Equal(10, result);
    }
}