namespace RemoteExplosives {
	/**
	 * Allows storing primitive types by reference. Allows explicit disposal to signal that the value is no longer in use. 
	 */
	public class DisposablePrimitiveWrapper<T> where T : struct {
		private T _t;

		public static implicit operator T(DisposablePrimitiveWrapper<T> w) {
			return w.Value;
		}

		public DisposablePrimitiveWrapper(T t) {
			_t = t;
		}

		public bool Disposed { get; private set; }

		public T Value {
			get {
				return _t;
			}

			set {
				_t = value;
			}
		}

		public void Dispose() {
			Disposed = true;
		}

		public override string ToString() {
			return _t.ToString();
		}
	} 

}
