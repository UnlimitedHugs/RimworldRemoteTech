using Verse;

namespace RemoteExplosives {
	/// <summary>
	/// Implemented by buildings to become potential targets of the Red Button Fever mental break.
	/// </summary>
	/// <see cref="JobGiver_RedButtonFever"/>
	public interface IRedButtonFeverTarget {
		bool RedButtonFeverCanInteract { get; }
		void RedButtonFeverDoInteraction(Pawn p);
	}
}