using UnityEngine;
using Verse;

namespace RemoteExplosives {
	/**
	 * A graphic that can change appearance between multiple graphics, current variant is specified by IGraphicVariantProvider
	 */
	public class Graphic_Variants : Graphic_Collection {
		public override Material MatSingle {
			get { return GetDefaultMat(); }
		}

		public override Material MatAt(Rot4 rot, Thing thing = null) {
			return MatSingleFor(thing);
		}

		public override Material MatSingleFor(Thing thing) {
			var provider = thing as IGraphicVariantProvider;
			if (provider == null) {
				return GetDefaultMat();
			}
			var variantIndex = provider.GraphicVariant;
			if (variantIndex < 0 || variantIndex > subGraphics.Length) {
				RemoteExplosivesController.Instance.Logger.Error(string.Format("No material with index {0} available, as requested by {1}", variantIndex, thing.GetType()));
				return GetDefaultMat();
			}
			return subGraphics[variantIndex].MatSingleFor(thing);
		}

		private Material GetDefaultMat() {
			if (subGraphics.Length > 0) return subGraphics[0].MatSingle;
			return base.MatSingle;
		}
	}
}