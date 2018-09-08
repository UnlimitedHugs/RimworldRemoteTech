namespace RemoteTech {
	public interface ISensorSettingsProvider {
		bool HasAIUpgrade { get; }
		bool HasWirelessUpgrade { get; }
		SensorSettings Settings { get; }
		void OnSettingsChanged(SensorSettings s);
	}
}