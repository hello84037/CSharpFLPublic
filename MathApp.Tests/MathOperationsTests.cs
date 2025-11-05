using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MathApp.Tests
{
    public class MathOperationsTests
    {
#region Addition Tests
        [Fact]
        public void AdditionTest()
        {
            System.IO.File.AppendAllText("MathApp.Tests.MathOperationsTests.AdditionTest.coverage", "1b50d3a9-3314-406e-b01a-8a14aa5f6d89" + System.Environment.NewLine);
            int result = MathApp.MathOperations.Add(5, 3);
            System.IO.File.AppendAllText("MathApp.Tests.MathOperationsTests.AdditionTest.coverage", "45ea295d-f46a-4ad5-845d-c6c43a27503b" + System.Environment.NewLine);
            Assert.Equal(8, result);
        }

        [Fact]
        public void AdditionTest2()
        {
            System.IO.File.AppendAllText("MathApp.Tests.MathOperationsTests.AdditionTest2.coverage", "9787798d-c826-44e7-998b-d9096742f202" + System.Environment.NewLine);
            int result = MathApp.MathOperations.Add(4, 3);
            System.IO.File.AppendAllText("MathApp.Tests.MathOperationsTests.AdditionTest2.coverage", "264f493c-200c-48cd-96e9-f3de444d1308" + System.Environment.NewLine);
            Assert.Equal(7, result);
        }

        [Fact]
        public void AdditionTest3()
        {
            System.IO.File.AppendAllText("MathApp.Tests.MathOperationsTests.AdditionTest3.coverage", "4cb00e39-4a65-4fff-81e6-fe90d6e426ad" + System.Environment.NewLine);
            int result = MathApp.MathOperations.Add(7, 3);
            System.IO.File.AppendAllText("MathApp.Tests.MathOperationsTests.AdditionTest3.coverage", "c5fbf08f-6359-4002-9aae-b2d14b1df784" + System.Environment.NewLine);
            Assert.Equal(10, result);
        }

#endregion
#region Division Tests
        [Fact]
        public void DivisionTest()
        {
            System.IO.File.AppendAllText("MathApp.Tests.MathOperationsTests.DivisionTest.coverage", "1a3656bd-86c4-415a-bf23-ab17d07a3ab5" + System.Environment.NewLine);
            double result = MathApp.MathOperations.Divide(10, 2);
            System.IO.File.AppendAllText("MathApp.Tests.MathOperationsTests.DivisionTest.coverage", "0d8d31f3-5b87-40b2-a610-0d4b87e798e5" + System.Environment.NewLine);
            Assert.Equal(5.0, result);
        }

        [Fact]
        public void DivisionTest2()
        {
            System.IO.File.AppendAllText("MathApp.Tests.MathOperationsTests.DivisionTest2.coverage", "526e47f2-a20b-46a4-b4af-5495533f58f7" + System.Environment.NewLine);
            double result = MathApp.MathOperations.Divide(15, 2);
            System.IO.File.AppendAllText("MathApp.Tests.MathOperationsTests.DivisionTest2.coverage", "a5406d8d-d34d-4dca-896a-794bb95544a2" + System.Environment.NewLine);
            Assert.Equal(7.5, result);
        }

        [Fact]
        public void DivisionTest3()
        {
            System.IO.File.AppendAllText("MathApp.Tests.MathOperationsTests.DivisionTest3.coverage", "8ed6a5cb-2de5-462c-90d1-231811e77392" + System.Environment.NewLine);
            double result = MathApp.MathOperations.Divide(150, 2);
            System.IO.File.AppendAllText("MathApp.Tests.MathOperationsTests.DivisionTest3.coverage", "2225fced-0a35-48c8-8942-72ca743df70a" + System.Environment.NewLine);
            Assert.Equal(75.0, result);
        }

#endregion
#region Multiplication Tests
        [Fact]
        public void MultiplicationTest()
        {
            System.IO.File.AppendAllText("MathApp.Tests.MathOperationsTests.MultiplicationTest.coverage", "7a81b680-d462-4f3c-b4c3-cd5d06993997" + System.Environment.NewLine);
            double result = MathApp.MathOperations.Multiply(10, 2);
            System.IO.File.AppendAllText("MathApp.Tests.MathOperationsTests.MultiplicationTest.coverage", "2001cf62-a25a-4b41-9a32-0814cd244310" + System.Environment.NewLine);
            Assert.Equal(20.0, result);
        }

        [Fact]
        public void MultiplicationTest2()
        {
            System.IO.File.AppendAllText("MathApp.Tests.MathOperationsTests.MultiplicationTest2.coverage", "d92caacc-22b9-4eb2-bdd3-dd8994a8840a" + System.Environment.NewLine);
            double result = MathApp.MathOperations.Multiply(15, 2);
            System.IO.File.AppendAllText("MathApp.Tests.MathOperationsTests.MultiplicationTest2.coverage", "cc931910-18ff-4502-8bf6-c2298c00f7eb" + System.Environment.NewLine);
            Assert.Equal(30.0, result);
        }

        [Fact]
        public void MultiplicationTest3()
        {
            System.IO.File.AppendAllText("MathApp.Tests.MathOperationsTests.MultiplicationTest3.coverage", "9f78f4a7-5e8e-48e5-b613-91a121da4e40" + System.Environment.NewLine);
            double result = MathApp.MathOperations.Multiply(150, 2);
            System.IO.File.AppendAllText("MathApp.Tests.MathOperationsTests.MultiplicationTest3.coverage", "69841383-8d9e-4c5a-a9ff-f318fc4184c3" + System.Environment.NewLine);
            Assert.Equal(300.0, result);
        }

#endregion
#region Subtraction Tests
        [Fact]
        public void SubtractionTest()
        {
            System.IO.File.AppendAllText("MathApp.Tests.MathOperationsTests.SubtractionTest.coverage", "9a0b1f63-eb6e-4740-a1ff-33ef01670fd0" + System.Environment.NewLine);
            int result = MathApp.MathOperations.Subtract(5, 3);
            System.IO.File.AppendAllText("MathApp.Tests.MathOperationsTests.SubtractionTest.coverage", "52b7872d-6aa1-4954-9d17-7442f1087989" + System.Environment.NewLine);
            Assert.Equal(2, result);
        }

        [Fact]
        public void SubtractionTest2()
        {
            System.IO.File.AppendAllText("MathApp.Tests.MathOperationsTests.SubtractionTest2.coverage", "92a18f34-ec85-43c4-9809-dd5d9224e280" + System.Environment.NewLine);
            //int result = MathApp.MathOperations.Subtract(6, 4);
            int result = MathApp.MathOperations.Subtract(4, 6);
            System.IO.File.AppendAllText("MathApp.Tests.MathOperationsTests.SubtractionTest2.coverage", "c5b2b083-b25f-4c99-aaee-09f047e1bdf4" + System.Environment.NewLine);
            Assert.Equal(2, result);
        }

        [Fact]
        public void SubtractionTest3()
        {
            System.IO.File.AppendAllText("MathApp.Tests.MathOperationsTests.SubtractionTest3.coverage", "d5a00a92-7af0-4806-a060-8133798d3615" + System.Environment.NewLine);
            int result = MathApp.MathOperations.Subtract(9, 3);
            System.IO.File.AppendAllText("MathApp.Tests.MathOperationsTests.SubtractionTest3.coverage", "a5c8269f-c450-4c9b-a0dc-ef3b8077fe58" + System.Environment.NewLine);
            Assert.Equal(6, result);
        }
        #endregion

        #region Modulus Tests
        [Theory]
        [InlineData(10, 3, 1)]
        [InlineData(-10, 3, -1)]
        [InlineData(10, -3, 1)]
        [InlineData(-10, -3, -1)]
        [InlineData(5, 5, 0)]
        public void ComputesRemainderForValidDivisors(double dividend, double divisor, double expected)
        {
            double result = MathApp.MathOperations.Modulus(dividend, divisor);
            Assert.Equal(expected, result);
        }

        [Fact]
        public void ThrowsWhenDivisorIsZero()
        {
            Assert.Throws<ArgumentException>(() => MathApp.MathOperations.Modulus(10, 0));
        }
        #endregion
    }
}