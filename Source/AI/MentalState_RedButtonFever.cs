using RimWorld;
using Verse.AI;

namespace RemoteExplosives {
	public class MentalState_RedButtonFever : MentalState {
		public override RandomSocialMode SocialModeMax() {
			return RandomSocialMode.Off;
		}
	}
}