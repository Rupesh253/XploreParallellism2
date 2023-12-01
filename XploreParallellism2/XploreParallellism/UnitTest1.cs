namespace XploreParallellism2 {
    [TestFixture]
    public class UnitTest1 {
        //public UnitTest1(int number1, int number2) : base(number1, number2) {
        //}

        [SetUp]
        public void SetUp() {
            Console.WriteLine("UnitTest1.SetUp");

        }
        [Test]
        public void Test1() {
            Console.WriteLine("\tUnitTest1.Test1\n\t using number1: {number1}, number2: {number2}");
        }
        [Test]
        public void Test2() {
            Console.WriteLine("\tUnitTest1.Test2\n\t using number1: {number1}, number2: {number2}");
        }
        [TearDown]
        public void TearDown() {
            Console.WriteLine("UnitTest1.TearDown");
        }
    }
}