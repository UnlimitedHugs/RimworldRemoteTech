using RimWorld;
using Verse;

namespace RemoteTech {
	/// <summary>
	/// A Designator_Build with a replaceable label.
	/// </summary>
	public class Designator_BuildLabeled : Designator_Build {
		public string replacementLabel;

		public Designator_BuildLabeled(BuildableDef entDef) : base(entDef) {
		}

		public override string Label {
			get { return replacementLabel ?? base.Label; }
		}
	}
}