using Verse;

namespace RemoteTech {
	/// <summary>
	/// Allows a recipe to specify a material variant that should not be added for that recipe
	/// </summary>
	public class RecipeVariantSkip : DefModExtension, ICloneable<RecipeVariantSkip> {
		public RecipeVariant skipVariant;

		public RecipeVariantSkip Clone() {
			return new RecipeVariantSkip();
		}
	}
}