using Verse;

namespace RemoteTech {
	/// <summary>
	/// A marker to identify procedurally generated bills for the workbench.
	/// </summary>
	public class RecipeVariantExtension : DefModExtension, ICloneable<RecipeVariantExtension> {
		public RecipeVariant Variant { get; set; }

		public RecipeVariantExtension Clone() {
			return new RecipeVariantExtension {Variant = Variant};
		}
	}
}