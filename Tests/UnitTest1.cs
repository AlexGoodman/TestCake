using Api.Controllers;
using NUnit.Framework;

namespace Tests
{
    public class Tests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public async void Test1()
        {
            var controller = new TestController();
            Assert.AreEqual(await controller.Test1(), 1);
        }

        [Test]
        public void Test2()
        {
            Assert.Pass();
        }

        [Test]
        public void Test3()
        {
            Assert.Pass();
        }
    }
}