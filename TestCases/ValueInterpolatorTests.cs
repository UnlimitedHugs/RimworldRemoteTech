using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace RemoteExplosives {
	[TestClass]
	public class ValueInterpolatorTests {
		private int callbackCalls;
		private DisposablePrimitiveWrapper<float> sharedValue;
			
		[TestMethod]
		public void SingleValue() {
			callbackCalls = 0;
			var val = new DisposablePrimitiveWrapper<float>(11f);
			ValueInterpolator.Instance.Initialize(0);
			ValueInterpolator.Instance.InterpolateValue(val, 15f, 4f, ValueInterpolator.InterpolationCurveType.Linear, TestCallback);
			
			for (int i = 1; i <= 5; i++) {
				ValueInterpolator.Instance.Update(i);
				if (i < 5) {
					Assert.AreEqual(11f + i, val.Value);
				} else {
					Assert.AreEqual(15f, val.Value);
				}
				if(i>=4) {
					Assert.AreEqual(1, callbackCalls);	
				}
			}
			Assert.AreEqual(1, ValueInterpolator.Instance.NumInterpolatorsPooled);
			Assert.AreEqual(1, callbackCalls);
		}

		[TestMethod]
		public void MultipleValues() {
			callbackCalls = 0;
			var val1 = new DisposablePrimitiveWrapper<float>(10f);
			var val2 = new DisposablePrimitiveWrapper<float>(100f);
			ValueInterpolator.Instance.Initialize(0);
			ValueInterpolator.Instance.InterpolateValue(val1, 100f, 10f, ValueInterpolator.InterpolationCurveType.Linear, TestCallback);
			ValueInterpolator.Instance.InterpolateValue(val2, 10f, 10f, ValueInterpolator.InterpolationCurveType.Linear, TestCallback);
			for (int i = 1; i <= 10; i++) {
				ValueInterpolator.Instance.Update(i);
				if (i < 10) {
					Assert.IsTrue(val1>10 && val1<100, i.ToString());
					Assert.IsTrue(val1<100 && val1>10, i.ToString());
				}
			}
			Assert.AreEqual(100f, val1);
			Assert.AreEqual(10f, val2);
			Assert.AreEqual(2, ValueInterpolator.Instance.NumInterpolatorsPooled);
			Assert.AreEqual(2, callbackCalls);
		}

		[TestMethod]
		public void DisposalBeforeCompletion() {
			callbackCalls = 0;
			var val = new DisposablePrimitiveWrapper<float>(11f);
			ValueInterpolator.Instance.Initialize(0);
			ValueInterpolator.Instance.InterpolateValue(val, 15f, 4f, ValueInterpolator.InterpolationCurveType.Linear, TestCallback);
			
			ValueInterpolator.Instance.Update(1);
			val.Dispose();
			ValueInterpolator.Instance.Update(2);
			Assert.AreEqual(1, ValueInterpolator.Instance.NumInterpolatorsPooled);
			Assert.AreEqual(0, callbackCalls);
		}

		[TestMethod]
		public void PoolReuse() {
			callbackCalls = 0;
			var val = new DisposablePrimitiveWrapper<float>(11f);
			ValueInterpolator.Instance.Initialize(0);
			for (int i = 0; i < 10; i++) {
				ValueInterpolator.Instance.InterpolateValue(val, 15f, 4f, ValueInterpolator.InterpolationCurveType.Linear, TestCallback);	
			}
			Assert.AreEqual(0, ValueInterpolator.Instance.NumInterpolatorsPooled, "setup1");
			Assert.AreEqual(10, ValueInterpolator.Instance.NumInterpolatorsActive, "setup1");
			ValueInterpolator.Instance.Update(100);
			for (int i = 0; i < 3; i++) {
				ValueInterpolator.Instance.InterpolateValue(val, 15f, 4f, ValueInterpolator.InterpolationCurveType.Linear, TestCallback);
			}
			Assert.AreEqual(7, ValueInterpolator.Instance.NumInterpolatorsPooled, "setup2");
			Assert.AreEqual(3, ValueInterpolator.Instance.NumInterpolatorsActive, "setup2");
			
			ValueInterpolator.Instance.Update(200);

			Assert.AreEqual(13, callbackCalls, "final");
			Assert.AreEqual(10, ValueInterpolator.Instance.NumInterpolatorsPooled, "final");
			Assert.AreEqual(0, ValueInterpolator.Instance.NumInterpolatorsActive, "final");
		}

		[TestMethod]
		public void RestartThroughCallback() {
			callbackCalls = 0;
			sharedValue = new DisposablePrimitiveWrapper<float>(1f);
			ValueInterpolator.Instance.Initialize(0);
			ValueInterpolator.Instance.InterpolateValue(sharedValue, 2, 1, ValueInterpolator.InterpolationCurveType.Linear, RestartCallback);
			ValueInterpolator.Instance.InterpolateValue(sharedValue, 2, 1, ValueInterpolator.InterpolationCurveType.Linear, RestartCallback);
			ValueInterpolator.Instance.Update(100);

			Assert.AreEqual(2, ValueInterpolator.Instance.NumInterpolatorsActive);

			ValueInterpolator.Instance.Update(200);

			Assert.AreEqual(3, sharedValue.Value);
			Assert.AreEqual(2, ValueInterpolator.Instance.NumInterpolatorsPooled);
		}

		private void TestCallback() {
			callbackCalls++;
		}		

		private void RestartCallback() {
			callbackCalls++;
			ValueInterpolator.Instance.InterpolateValue(sharedValue, 3, 1, ValueInterpolator.InterpolationCurveType.Linear);
		}
	}
}
