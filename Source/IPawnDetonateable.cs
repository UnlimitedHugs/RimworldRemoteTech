namespace RemoteExplosives {
	public interface IPawnDetonateable {
		bool UseInteractionCell { get; }
		bool WantsDetonation();
		void DoDetonation();
	}
}