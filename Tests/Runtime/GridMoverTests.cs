using NUnit.Framework;
using UnityEngine;

namespace Metz.JamKit.Tests
{
    public class GridMoverTests
    {
        [Test]
        public void SnapToGrid_XY_RoundsBothAxes_KeepsZ()
        {
            var snapped = GridMover.SnapToGrid(new Vector3(1.4f, -0.6f, 3.3f), 1f, GridMover.GridPlane.XY);
            Assert.AreEqual(new Vector3(1f, -1f, 3.3f), snapped);
        }

        [Test]
        public void SnapToGrid_XZ_RoundsXZ_KeepsY()
        {
            var snapped = GridMover.SnapToGrid(new Vector3(2.6f, 0.5f, -1.4f), 1f, GridMover.GridPlane.XZ);
            Assert.AreEqual(new Vector3(3f, 0.5f, -1f), snapped);
        }

        [Test]
        public void SnapToGrid_RespectsCellSize()
        {
            var snapped = GridMover.SnapToGrid(new Vector3(1.4f, 1.4f, 0f), 2f, GridMover.GridPlane.XY);
            Assert.AreEqual(new Vector3(2f, 2f, 0f), snapped);
        }

        [Test]
        public void SnapToGrid_IsIdempotent()
        {
            var once = GridMover.SnapToGrid(new Vector3(7.7f, -3.2f, 0f), 0.5f, GridMover.GridPlane.XY);
            var twice = GridMover.SnapToGrid(once, 0.5f, GridMover.GridPlane.XY);
            Assert.AreEqual(once, twice);
        }
    }
}
