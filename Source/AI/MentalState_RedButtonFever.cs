using RimWorld;
using Verse.AI;

namespace RemoteTech {
	public class MentalState_RedButtonFever : MentalState {
		public override RandomSocialMode SocialModeMax() {
			return RandomSocialMode.Off;
		}
	}
}