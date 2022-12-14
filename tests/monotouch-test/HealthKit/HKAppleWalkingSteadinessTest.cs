#if HAS_HEALTHKIT

using System;

using Foundation;
using HealthKit;
using NUnit.Framework;
#if MONOMAC
using AppKit;
#else
using UIKit;
#endif

namespace MonoTouchFixtures.HealthKit {

	[TestFixture]
	[Preserve (AllMembers = true)]
	public class HKAppleWalkingSteadinessTest {

		[SetUp]
		public void SetUp ()
		{
#if MONOMAC
			TestRuntime.AssertXcodeVersion (14, 0);
#else
			TestRuntime.AssertXcodeVersion (13, 0);
#endif
		}

		[Test]
		public void TryGetClassificationTest ()
		{
			var max = HKAppleWalkingSteadiness.GetMaximumQuantity (HKAppleWalkingSteadinessClassification.Ok);
			Assert.True (HKAppleWalkingSteadiness.TryGetClassification (max, out var classification, out var error));
			Assert.Null (error, "error");
			Assert.AreEqual (classification, HKAppleWalkingSteadinessClassification.Ok, "classification");
		}

		[Test]
		public void GetMinimumQuantityTest ()
			=> Assert.NotNull (HKAppleWalkingSteadiness.GetMinimumQuantity (HKAppleWalkingSteadinessClassification.Ok));

		[Test]
		public void GetMaximumQuantityTest ()
			=> Assert.NotNull (HKAppleWalkingSteadiness.GetMaximumQuantity (HKAppleWalkingSteadinessClassification.Ok));
	}
}

#endif // HAS_HEALTHKIT
