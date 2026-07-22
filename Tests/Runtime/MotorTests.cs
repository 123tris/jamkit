using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Metz.JamKit.Tests
{
    /// <summary>
    /// Pins <see cref="Motor.Resolve"/>'s body-type precedence (Rigidbody2D &gt; Rigidbody &gt;
    /// Transform) and the transform fallback's move/teleport behavior. These are the invariants the
    /// gameplay components rely on now that the rb2d/rb/transform branch lives in one place.
    /// </summary>
    public class MotorTests
    {
        readonly List<GameObject> _spawned = new();

        GameObject New(string name)
        {
            var go = new GameObject(name);
            _spawned.Add(go);
            return go;
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var go in _spawned)
                if (go != null) Object.DestroyImmediate(go);
            _spawned.Clear();
        }

        [Test]
        public void ResolvePicksRigidbody2DFirst()
        {
            var go = New("2d");
            go.AddComponent<Rigidbody2D>();
            Assert.IsInstanceOf<Rigidbody2DMotor>(Motor.Resolve(go));
        }

        [Test]
        public void ResolvePicksRigidbodyWhenNo2D()
        {
            var go = New("3d");
            go.AddComponent<Rigidbody>();
            Assert.IsInstanceOf<Rigidbody3DMotor>(Motor.Resolve(go));
        }

        [Test]
        public void ResolveFallsBackToTransform()
        {
            Assert.IsInstanceOf<TransformMotor>(Motor.Resolve(New("plain")));
        }

        [Test]
        public void ResolvePrefers2DWhenBothPresent()
        {
            var go = New("both");
            go.AddComponent<Rigidbody2D>();
            go.AddComponent<Rigidbody>();
            Assert.IsInstanceOf<Rigidbody2DMotor>(Motor.Resolve(go));
        }

        [Test]
        public void TransformMotorMovesAndTeleportsTheTransform()
        {
            var go = New("plain");
            IMotor motor = Motor.Resolve(go);

            motor.MoveTo(new Vector3(1f, 2f, 3f));
            Assert.AreEqual(new Vector3(1f, 2f, 3f), go.transform.position);

            motor.Teleport(new Vector3(-4f, 0f, 5f));
            Assert.AreEqual(new Vector3(-4f, 0f, 5f), go.transform.position);

            Assert.DoesNotThrow(() => motor.Halt()); // no velocity on a transform — a safe no-op
        }

        [Test]
        public void LaunchBodyIgnoresTransformOnlyObjects()
        {
            // No rigidbody to receive velocity — must not throw.
            Assert.DoesNotThrow(() => Motor.LaunchBody(New("plain"), Vector3.one));
        }
    }
}
