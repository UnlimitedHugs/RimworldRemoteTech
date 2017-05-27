namespace RemoteExplosives {
	/**
	 * These are functions that describe the change of a value over time. See http://easings.net/ for more info.
	 */
	public static class InterpolationCurves {
		public delegate float InterpolationCurve(float time, float startValue, float changeInValue, float totalDuration);

		public static InterpolationCurve Linear = (t, s, c, d) => {
			t /= d;
			return c*t + s;
		};

		public static InterpolationCurve QuinticEaseOut = (t, s, c, d) => {
			t /= d;
			t--;
			return c*(t*t*t*t*t + 1) + s;
		};

		public static InterpolationCurve CubicEaseInOut = (t, s, c, d) => {
			if ((t /= d/2) < 1) return c/2*t*t*t + s;
			return c/2*((t -= 2)*t*t + 2) + s;
		};
	}
}