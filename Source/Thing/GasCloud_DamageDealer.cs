using System.Collections.Generic;
using System.Linq;
using Verse;

namespace RemoteExplosives {
	/// <summary>
	/// Applies damage to thing in the same cell based on concentration.
	/// See MoteProperties_GasEffect for settings.
	/// </summary>
	public class GasCloud_DamageDealer : GasCloud_AffectThing {
		protected override void ApplyGasEffect(Thing thing, float strengthMultiplier) {
			BodyPartRecord bodyPart = null;
			if (thing is Pawn pawn && Props.damageBodyPartTags.Count > 0) {
				var partTag = RandomElementOrDefault(Props.damageBodyPartTags);
				bodyPart = RandomElementOrDefault(pawn.RaceProps?.body.GetPartsWithTag(partTag));
			}
			var amount = Props.damageAmount * strengthMultiplier;
			if (amount < 1f && Props.damageCanGlance) {
				amount = amount * Rand.Value > .5f ? amount : 0f;
			}
			thing.TakeDamage(new DamageInfo(Props.damageDef, amount, Props.damageArmorPenetration, -1F, this, bodyPart));
		}

		private T RandomElementOrDefault<T>(IEnumerable<T> source) {
			var list = source as IList<T> ?? source.ToList();
			return list.Count > 0 ? list[Rand.Range(0, list.Count)] : default(T);
		}
	}
}