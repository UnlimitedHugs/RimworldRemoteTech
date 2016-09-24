using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace RemoteExplosives {
	[TestClass]
	public class DistributedTickSchedulerTests {
		private DistributedTickScheduler scheduler;
		private List<int> callsPerCallback;
		private int totalCallbackCalls;
		private int currentTick;
		private int callbackCreationIndex;
		private List<Action> registeredCallbacks;
		[TestInitialize]
		public void PrepareRun() {
			scheduler = DistributedTickScheduler.Instance;
			scheduler.Initialize(0);
			totalCallbackCalls = 0;
			callsPerCallback = new List<int>();
			callbackCreationIndex = 0;
			registeredCallbacks = new List<Action>();
		}

		[TestMethod]
		public void LessCallbacksThanInterval() {
			foreach (var callback in PrepareCallbacks(3)) {
				scheduler.RegisterTickability(callback, 10);
			}
			TickScheduler(30);
			AssertTotalCalls(9);
			AssertNumCallsPerCallback(3);
		}

		[TestMethod]
		public void AsManyCallbacksAsInterval() {
			foreach (var callback in PrepareCallbacks(5)) {
				scheduler.RegisterTickability(callback, 5);
			}
			TickScheduler(15);
			AssertNumCallsPerCallback(3);
			AssertTotalCalls(15);
		}

		[TestMethod]
		public void MoreCallbacksThanInterval() {
			foreach (var callback in PrepareCallbacks(7)) {
				scheduler.RegisterTickability(callback, 5);
			}
			TickScheduler(14);
			AssertNumCallsPerCallback(3);
			AssertTotalCalls(21);
		}

		[TestMethod]
		public void OneTickInterval() {
			foreach (var callback in PrepareCallbacks(5)) {
				scheduler.RegisterTickability(callback, 1);
			}
			TickScheduler(5);
			AssertTotalCalls(25);
			AssertNumCallsPerCallback(5);
		}

		[TestMethod]
		public void CallbackInfluxMidCycle() {
			foreach (var callback in PrepareCallbacks(5)) {
				scheduler.RegisterTickability(callback, 5);
			}
			TickScheduler(3);
			foreach (var callback in PrepareCallbacks(2)) {
				scheduler.RegisterTickability(callback, 5);
			}
			TickScheduler(7);
			AssertTotalCalls(3 + 2 + 2 + 7);
			TickScheduler(5);
			AssertTotalCalls(3 + 2 + 2 + 7 + 7);
		}

		[TestMethod]
		public void MultipleIntervals() {
			foreach (var callback in PrepareCallbacks(3)) {
				scheduler.RegisterTickability(callback, 3);
			}
			foreach (var callback in PrepareCallbacks(3)) {
				scheduler.RegisterTickability(callback, 6);
			}
			TickScheduler(3);
			AssertTotalCalls(3 + 3);
			AssertNumCallsPerCallback(1);
			TickScheduler(3);
			AssertTotalCalls(3 + 3 + 3);
		}

		[TestMethod]
		public void Reinitialize() {
			foreach (var callback in PrepareCallbacks(10)) {
				scheduler.RegisterTickability(callback, 5);
			}
			TickScheduler(5);
			AssertTotalCalls(10);
			AssertNumCallsPerCallback(1);
			scheduler.Initialize(11);
			TickScheduler(3);
			AssertTotalCalls(10);
		}

		[TestMethod]
		public void Unregister() {
			foreach (var callback in PrepareCallbacks(3)) {
				scheduler.RegisterTickability(callback, 3);
			}
			foreach (var callback in PrepareCallbacks(3)) {
				scheduler.RegisterTickability(callback, 6);
			}
			TickScheduler(3);
			AssertTotalCalls(3 + 3);
			for (int i = 0; i < 3; i++) {
				scheduler.UnregisterTickability(registeredCallbacks[i], 3);
			}
			TickScheduler(3);
			AssertTotalCalls(3 + 3);
			for (int i = 3; i < 6; i++) {
				scheduler.UnregisterTickability(registeredCallbacks[i], 6);
			}
			TickScheduler(50);
			AssertTotalCalls(3 + 3);
		}

		private IEnumerable<Action> PrepareCallbacks(int amount) {
			while (amount>0) {
				callsPerCallback.Add(0);
				var index = callbackCreationIndex;
				Action action = () => {
					totalCallbackCalls++;
					callsPerCallback[index]++;
				};
				registeredCallbacks.Add(action);
				callbackCreationIndex++;
				amount--;
				yield return action;
			}
		}

		private void AssertTotalCalls(int expectedCalls) {
			Assert.AreEqual(expectedCalls, totalCallbackCalls, "total calls");
		}

		private void AssertNumCallsPerCallback(int expectedNumCalls) {
			var pass = callsPerCallback.All(num => num == expectedNumCalls);
			if (!pass) {
				Assert.Fail("Num calls per callback failed. Expected {0}, min: {1}, max: {2}\nlisting: {3}", expectedNumCalls, callsPerCallback.Min(), callsPerCallback.Max(), string.Join("", callsPerCallback.Select(i => i.ToString()).ToArray()));
			}
		}

		private void TickScheduler(int numTicks) {
			while (numTicks>0) {
				numTicks--;
				currentTick++;
				scheduler.Tick(currentTick);
			}
		}
	}
}