using Api.Controllers;
using NUnit.Framework;
using System.Threading.Tasks;

namespace Tests
{
    public class Tests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public async Task Test1()
        {
            var controller = new TestController();
            Assert.AreEqual(1, await controller.Test1());
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