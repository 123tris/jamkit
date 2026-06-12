using System.Collections.Generic;
using NUnit.Framework;

namespace Metz.JamKit.Tests
{
    public class RandomBagTests
    {
        [Test]
        public void DrawsEverythingBeforeRepeating()
        {
            var bag = new RandomBag<int>(new[] { 1, 2, 3, 4 });
            var seen = new HashSet<int>();
            for (int i = 0; i < 4; i++) seen.Add(bag.Draw());
            Assert.AreEqual(4, seen.Count);
            CollectionAssert.AreEquivalent(new[] { 1, 2, 3, 4 }, seen);
        }

        [Test]
        public void RefillsAfterEmpty()
        {
            var bag = new RandomBag<int>(new[] { 1, 2 });
            bag.Draw(); bag.Draw();
            Assert.AreEqual(0, bag.Remaining);
            bag.Draw(); // triggers refill
            Assert.AreEqual(1, bag.Remaining);
        }

        [Test]
        public void EmptySourceReturnsDefault()
        {
            var bag = new RandomBag<int>(System.Array.Empty<int>());
            Assert.AreEqual(0, bag.Draw());
        }
    }
}
