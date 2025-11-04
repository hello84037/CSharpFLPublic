namespace MathApp.Tests
{

    public class SubtractionTests
    {
        [Fact]
        public void SubtractionTest()
        {
            int result = MathApp.MathOperations.Subtract(5, 3);
            Assert.Equal(2, result);
        }

        [Fact]
        public void SubtractionTest2()
        {
            //int result = MathApp.MathOperations.Subtract(6, 4);
            int result = MathApp.MathOperations.Subtract(4, 6);
            Assert.Equal(2, result);
        }

        [Fact]
        public void SubtractionTest3()
        {
            int result = MathApp.MathOperations.Subtract(9, 3);
            Assert.Equal(6, result);
        }
    }
}