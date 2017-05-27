namespace RemoteExplosives {
	public interface IPawnDetonateable {
		bool UseInteractionCell { get; }
		bool WantsDetonation { get; set; }
		void DoDetonation();
	}
}