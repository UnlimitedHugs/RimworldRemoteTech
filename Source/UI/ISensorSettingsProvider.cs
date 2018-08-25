namespace RemoteExplosives {
	public interface ISensorSettingsProvider {
		bool HasAIUpgrade { get; }
		bool HasWirelessUpgrade { get; }
		SensorSettings Settings { get; }
	}
}