using DynamicTyping.Benchmark;
using Xunit;

namespace DynamicTyping.Tests
{
    public class UnitTest3
    {
        [Fact]
        public void A()
        {
            var b = new ActualBenchmark();
            for (var i = 0; i < 100; i++)
            {
                b.Enumerate();
            }
        }
    }
}