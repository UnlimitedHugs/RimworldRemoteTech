using System;
using System.Collections.Generic;
using UnityEngine;

namespace RemoteExplosives {
	/**
	 * A basic tweening dispatcher that interpolates multiple boxed floats over time. For use with animations.
	 */
	public class ValueInterpolator {
		public enum InterpolationCurveType {
			Linear,
			QuinticEaseOut,
			CubicEaseInOut,
		}
		private delegate float InterpolationCurve(float t, float start, float change, float duration);
		
		private static ValueInterpolator instance;
		public static ValueInterpolator Instance {
			get {
				return instance ?? (instance = new ValueInterpolator());
			}
		}

		private readonly LinkedList<SingleInterpolator> activePool = new LinkedList<SingleInterpolator>();
		private readonly List<SingleInterpolator> swapPool = new List<SingleInterpolator>(); 
		private readonly Queue<SingleInterpolator> inactivePool = new Queue<SingleInterpolator>();
		private float lastUpdateTime;

		private ValueInterpolator() {
		}

		// finishedCallback will not be called if the DisposablePrimitiveWrapper is disposed before the interpolation completes. 
		public void InterpolateValue(DisposablePrimitiveWrapper<float> value, float targetValue, float duration, InterpolationCurveType curveType, Action finishedCallback = null) {
			if(value == null) throw new NullReferenceException("Undefined value");
			if (!curves.ContainsKey(curveType)) throw new NullReferenceException("Undefined curve");
			SingleInterpolator inst;
			if(inactivePool.Count>0) {
				inst = inactivePool.Dequeue();
			} else {
				inst = new SingleInterpolator();
			}
			inst.Initialize(value, targetValue, lastUpdateTime, duration, curves[curveType], finishedCallback);
			activePool.AddLast(inst);
		}

		public void Initialize(float currentTime) {
			lastUpdateTime = currentTime;
			activePool.Clear();
			inactivePool.Clear();
		}

		public void Update(float currentTime) {
			lastUpdateTime = currentTime;
			foreach (var interpolator in activePool) {
				interpolator.Update(currentTime);
				if (interpolator.HasFinished) {
					swapPool.Add(interpolator);
				}
			}
			if(swapPool.Count>0) {
				foreach (var interpolator in swapPool) {
					activePool.Remove(interpolator);
					inactivePool.Enqueue(interpolator);
					if(interpolator.Callback!=null && !interpolator.WasAborted) {
						interpolator.Callback();
					}
				}
				swapPool.Clear();
			}
		}

		public int NumInterpolatorsActive { get { return activePool.Count; } }
		public int NumInterpolatorsPooled { get { return inactivePool.Count; } }

		private class SingleInterpolator {

			public bool HasFinished { get; private set; }
			public bool WasAborted { get; private set; }
			private DisposablePrimitiveWrapper<float> interpolatedValue;
			private float initialValue;
			private float targetValue;
			private float startTime;
			private float duration;
			private InterpolationCurve interpolationCurve;
			private Action finishedCallback;

			public Action Callback {
				get { return finishedCallback; }
			}

			public void Initialize(DisposablePrimitiveWrapper<float> value, float targetVal, float currentTime, float durationTime, InterpolationCurve curve, Action callback) {
				HasFinished = false;
				WasAborted = false;
				interpolatedValue = value;
				initialValue = value;
				targetValue = targetVal;
				startTime = currentTime;
				duration = durationTime;
				finishedCallback = callback;
				interpolationCurve = curve;
			}

			public void Update(float currentTime) {
				if (interpolatedValue.Disposed) {
					HasFinished = true;
					WasAborted = true;
					interpolatedValue = null;
					return;
				}
				var t = Mathf.Max(0, currentTime - startTime);
				if(currentTime >= startTime + duration) {
					t = startTime + duration;
					HasFinished = true;
				}
				interpolatedValue.Value = interpolationCurve(t, initialValue, targetValue - initialValue, duration);
				if (HasFinished) {
					interpolatedValue.Value = targetValue;
					interpolatedValue = null;
				}
			}

		}

		private readonly Dictionary<InterpolationCurveType, InterpolationCurve> curves = new Dictionary<InterpolationCurveType, InterpolationCurve> {
			{InterpolationCurveType.Linear, (t, s, c, d) => {
				t /= d; return c * t + s;
			}},
			{InterpolationCurveType.QuinticEaseOut, (t, s, c, d) => {
				t /= d;
				t--;
				return c * (t * t * t * t * t + 1) + s;
			}},
			{InterpolationCurveType.CubicEaseInOut, (t, s, c, d) => {
				if ((t/=d/2) < 1) return c/2*t*t*t + s;
				return c/2*((t-=2)*t*t + 2) + s;
			}},
		};
	}
}
