﻿using DuetAPI.Machine;
using NUnit.Framework;

namespace DuetUnitTest.Machine
{
    [TestFixture]
    public class Spindles
    {
        [Test]
        public void Clone()
        {
            MachineModel original = new MachineModel();

            Spindle spindle = new Spindle
            {
                Active = 123.45F,
                Current = 45.678F
            };
            original.Spindles.Add(spindle);

            DuetAPI.Machine.MachineModel clone = (DuetAPI.Machine.MachineModel)original.Clone();

            Assert.AreEqual(1, original.Spindles.Count);
            Assert.AreEqual(original.Spindles[0].Active, clone.Spindles[0].Active);
            Assert.AreEqual(original.Spindles[0].Current, clone.Spindles[0].Current);

            Assert.AreNotSame(original.Spindles[0].Active, clone.Spindles[0].Active);
            Assert.AreNotSame(original.Spindles[0].Current, clone.Spindles[0].Current);
        }
    }
}
