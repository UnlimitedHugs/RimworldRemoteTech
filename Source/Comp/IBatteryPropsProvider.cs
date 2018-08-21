using RimWorld;

namespace RemoteExplosives {
	/// <summary>
	/// In combination with CompPowerBattery_Props_Patch allows types extending CompPowerBattery 
	/// to return custom comp props to the base class (can't override key methods)
	/// </summary>
	public interface IBatteryPropsProvider {
		CompProperties_Battery ReplacementProps { get; }
	}
}