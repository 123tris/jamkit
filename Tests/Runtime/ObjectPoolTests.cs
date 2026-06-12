using NUnit.Framework;

namespace Metz.JamKit.Tests
{
    public class ObjectPoolTests
    {
        class Box { public int Id; }

        [Test]
        public void Get_CreatesNewWhenEmpty()
        {
            int created = 0;
            var pool = new ObjectPool<Box>(() => { created++; return new Box { Id = created }; });
            var a = pool.Get();
            var b = pool.Get();
            Assert.AreEqual(2, created);
            Assert.AreNotSame(a, b);
        }

        [Test]
        public void Return_ReusesInstance()
        {
            int created = 0;
            var pool = new ObjectPool<Box>(() => { created++; return new Box(); });
            var a = pool.Get();
            pool.Return(a);
            var b = pool.Get();
            Assert.AreSame(a, b);
            Assert.AreEqual(1, created);
        }

        [Test]
        public void Prewarm_PopulatesIdle()
        {
            var pool = new ObjectPool<Box>(() => new Box(), prewarm: 5);
            Assert.AreEqual(5, pool.CountIdle);
        }

        [Test]
        public void MaxIdle_DropsOverflow()
        {
            var pool = new ObjectPool<Box>(() => new Box(), maxIdle: 2);
            pool.Return(new Box());
            pool.Return(new Box());
            pool.Return(new Box()); // dropped
            Assert.AreEqual(2, pool.CountIdle);
        }

        [Test]
        public void Hooks_FireOnGetAndReturn()
        {
            int gets = 0, returns = 0;
            var pool = new ObjectPool<Box>(() => new Box(), onGet: _ => gets++, onReturn: _ => returns++);
            var a = pool.Get();
            pool.Return(a);
            Assert.AreEqual(1, gets);
            Assert.AreEqual(1, returns);
        }
    }
}
