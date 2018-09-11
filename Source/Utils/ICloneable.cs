namespace RemoteTech {
	public interface ICloneable<out T> {
		T Clone();
	}
}