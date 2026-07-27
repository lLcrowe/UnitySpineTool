using System;
using NUnit.Framework;

namespace UnitySpineTool.Tests
{
    public class Spine43CompatibilityTests
    {
        [Test]
        public void PackageAssemblies_AreLoaded()
        {
            Assert.That(typeof(SpineTool.SpineAnimModule).Assembly.GetName().Name, Is.EqualTo("UnitySpineTool"));
            Assert.That(typeof(SpineTool.Editor.SpineAnimationPreviewWindow).Assembly.GetName().Name, Is.EqualTo("UnitySpineTool.Editor"));
        }

        [Test]
        public void Spine43ApiContracts_AreAvailable()
        {
            Assert.That(typeof(Spine.Skeleton).GetMethod(nameof(Spine.Skeleton.SetupPose), Type.EmptyTypes), Is.Not.Null);
            Assert.That(typeof(Spine.Skeleton).GetMethod(nameof(Spine.Skeleton.FindConstraint)), Is.Not.Null);
            Assert.That(typeof(Spine.AnimationState).GetMethod(nameof(Spine.AnimationState.GetTrack), new[] { typeof(int) }), Is.Not.Null);
            Assert.That(typeof(Spine.IkConstraint).GetProperty(nameof(Spine.IkConstraint.Pose)), Is.Not.Null);
            Assert.That(typeof(Spine.DrawOrder).GetProperty(nameof(Spine.DrawOrder.AppliedPose)), Is.Not.Null);
            Assert.That(typeof(Spine.Unity.SkeletonAnimation).GetProperty(nameof(Spine.Unity.SkeletonAnimation.Renderer)), Is.Not.Null);
        }
    }
}
