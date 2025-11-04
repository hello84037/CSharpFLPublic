namespace MathApp.Tests
{

    public class AdditionTests
    {
        [Fact]
        public void AdditionTest()
        {
            int result = MathApp.MathOperations.Add(5, 3);
            Assert.Equal(8, result);
        }

        [Fact]
        public void AdditionTest2()
        {
            int result = MathApp.MathOperations.Add(4, 3);
            Assert.Equal(7, result);
        }

        [Fact]
        public void AdditionTest3()
        {
            int result = MathApp.MathOperations.Add(7, 3);
            Assert.Equal(10, result);
        }
    }
}