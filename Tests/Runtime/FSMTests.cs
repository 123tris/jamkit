using NUnit.Framework;

namespace Metz.JamKit.Tests
{
    public class FSMTests
    {
        enum S { A, B, C }

        [Test]
        public void Change_FiresEnterAndExit()
        {
            int enteredA = 0, exitedA = 0, enteredB = 0;
            var fsm = new FSM<S>()
                .On(S.A, onEnter: () => enteredA++, onExit: () => exitedA++)
                .On(S.B, onEnter: () => enteredB++);

            fsm.Change(S.A);
            Assert.AreEqual(1, enteredA);
            Assert.AreEqual(0, exitedA);

            fsm.Change(S.B);
            Assert.AreEqual(1, exitedA);
            Assert.AreEqual(1, enteredB);
        }

        [Test]
        public void Change_SameStateIsNoOp()
        {
            int enteredA = 0;
            var fsm = new FSM<S>().On(S.A, onEnter: () => enteredA++);
            fsm.Change(S.A);
            fsm.Change(S.A);
            Assert.AreEqual(1, enteredA);
        }

        [Test]
        public void Tick_OnlyRunsCurrentStateUpdate()
        {
            int updA = 0, updB = 0;
            var fsm = new FSM<S>()
                .On(S.A, onUpdate: () => updA++)
                .On(S.B, onUpdate: () => updB++);
            fsm.Change(S.A);
            fsm.Tick();
            fsm.Tick();
            Assert.AreEqual(2, updA);
            Assert.AreEqual(0, updB);
        }

        [Test]
        public void Is_ReportsCurrentState()
        {
            var fsm = new FSM<S>().On(S.A).On(S.B);
            Assert.IsFalse(fsm.Is(S.A));
            fsm.Change(S.A);
            Assert.IsTrue(fsm.Is(S.A));
            Assert.IsFalse(fsm.Is(S.B));
        }
    }
}
