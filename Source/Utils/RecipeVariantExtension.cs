using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace RemoteTech {
	/// <summary>
	/// Put on RecipeDef to specify additional variants of the recipe to be created
	/// </summary>
	public class MakeRecipeVariants : DefModExtension, ICloneable {
		public RecipeVariantType Variant;
		public List<RecipeVariantType> CreateVariants = new List<RecipeVariantType>();

		public object Clone() {
			return new MakeRecipeVariants { Variant = Variant, CreateVariants = CreateVariants.ToList() };
		}
	}
}