using System;
using Verse;

namespace RemoteExplosives {
	/// <summary>
	/// Represents a value that is cached for a given number of ticks. 
	/// Allows to reduce performance overhead for getting expensive to calculate values every tick or frame.
	/// Can be implicitly cast to its contained value.
	/// </summary>
	public class CachedValue<T> {
		public static implicit operator T(CachedValue<T> val) {
			return val.Value;
		}

		private readonly Func<T> valueGetter;
		private readonly int recacheIntervalTicks;
		private int cachedTick;
		private T cachedValue;

		public CachedValue(Func<T> valueGetter, int recacheIntervalTicks = GenTicks.TicksPerRealSecond) {
			this.valueGetter = valueGetter;
			this.recacheIntervalTicks = recacheIntervalTicks;
			cachedTick = int.MinValue;
			cachedValue = default(T);
		}

		public T Value {
			get {
				if(!IsValid) throw new InvalidOperationException($"{nameof(CachedValue<T>)} cannot get Value: not initialized");
				var currentTick = GenTicks.TicksGame;
				if (cachedTick + recacheIntervalTicks < currentTick) Recache();
				return cachedValue;
			}
		}

		public T ValueRecached {
			get {
				Recache();
				return Value;
			}
		}

		public bool IsValid {
			get { return valueGetter != null; }
		}

		public void Recache() {
			cachedTick = GenTicks.TicksGame;
			cachedValue = valueGetter();
		}
	}
}