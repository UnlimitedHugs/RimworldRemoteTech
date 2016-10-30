using UnityEngine;
using Verse;

namespace RemoteExplosives {
	/**
	 * Changes a float value over time according to an interpolation curve. Used for animation.
	 */
	public class InterpolatedValue {
		public float value;
		public bool finished = true;
		public bool respectTimeScale = true;
		private float elapsedTime;
		private float initialValue;
		private float targetValue;
		private float duration;
		private InterpolationCurves.InterpolationCurve curve;
		
		public void StartInterpolation(float finalValue, float interpolationDuration, InterpolationCurves.InterpolationCurve interpolationCurve) {
			initialValue = value;
			elapsedTime = 0;
			targetValue = finalValue;
			duration = interpolationDuration;
			curve = interpolationCurve;
			finished = false;
		}

		public void UpdateIfUnpaused() {
			if(Find.TickManager.Paused) return;
			Update();
		}

		public void Update() {
			if (finished) return;
			var deltaTime = Time.deltaTime;
			if (respectTimeScale) deltaTime *= Find.TickManager.TickRateMultiplier;
			elapsedTime += deltaTime;
			if (elapsedTime >= duration) {
				elapsedTime = duration;
				value = targetValue;
				finished = true;
			} else {
				value = curve(elapsedTime, initialValue, targetValue - initialValue, duration);
			}
		}

		public static implicit operator float(InterpolatedValue v) {
			return v.value;
		}
	}
}