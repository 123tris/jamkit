using NUnit.Framework;
using UnityEngine;

namespace Metz.JamKit.Tests
{
    public class BouncerMathTests
    {
        [Test]
        public void ClampAwayFromAxes_LeavesDiagonalAlone()
        {
            var dir = new Vector2(1f, 1f).normalized;
            var result = Bouncer2D.ClampAwayFromAxes(dir, 10f);
            Assert.AreEqual(dir.x, result.x, 0.0001f);
            Assert.AreEqual(dir.y, result.y, 0.0001f);
        }

        [Test]
        public void ClampAwayFromAxes_FixesPureHorizontal()
        {
            var result = Bouncer2D.ClampAwayFromAxes(Vector2.right, 10f);
            // No flat rallies: at least 10° away from the x-axis, direction preserved on x.
            Assert.Greater(Mathf.Abs(result.y), Mathf.Sin(10f * Mathf.Deg2Rad) - 0.001f);
            Assert.Greater(result.x, 0f);
            Assert.AreEqual(1f, result.magnitude, 0.0001f);
        }

        [Test]
        public void ClampAwayFromAxes_FixesPureVertical_PreservingSign()
        {
            var result = Bouncer2D.ClampAwayFromAxes(Vector2.down, 15f);
            Assert.Greater(Mathf.Abs(result.x), Mathf.Sin(15f * Mathf.Deg2Rad) - 0.001f);
            Assert.Less(result.y, 0f);
            Assert.AreEqual(1f, result.magnitude, 0.0001f);
        }

        [Test]
        public void ClampAwayFromAxes_ZeroMinAngle_IsIdentity()
        {
            var dir = new Vector2(0.999f, 0.04f).normalized;
            var result = Bouncer2D.ClampAwayFromAxes(dir, 0f);
            Assert.AreEqual(dir, result);
        }
    }
}
