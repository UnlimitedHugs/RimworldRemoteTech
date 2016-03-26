using System.Collections.Generic;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace RemoteExplosives {
	public class Building_RemoteExplosive : Building, IFlickable {
		
		private const int ticksBetweenBlinksArmed = 100;
		private const int ticksBetweenBlinksLit = 6;
		private const string flareGraphicPath = "mine_flare";
		private const string flareGraphicStrongPath = "mine_flare_strong";

		private static readonly Texture2D UITex_Arm = ContentFinder<Texture2D>.Get("UIArm");
		private static Graphic flareOverlayNormal;
		private static Graphic flareOverlayStrong;

		private static readonly SoundDef armSound = SoundDef.Named("RemoteExplosiveArmed");
		private static readonly SoundDef beepSound = SoundDef.Named("RemoteExplosiveBeep");

		private static readonly string ArmButtonLabel = "RemoteExplosive_arm_label".Translate();
		private static readonly string ArmButtonDesc = "RemoteExplosive_arm_desc".Translate();

		private CompCustomExplosive comp;

		private bool desiredArmState;
		private bool isArmed;
		private int ticksSinceFlare;

		public bool IsArmed {
			get { return isArmed; }
		}

		public bool FuseLit {
			get { return comp.WickStarted; }
		}

		public void LightFuse() {
			if(FuseLit) return;
			comp.StartSilentWick();
		}

		public override void SpawnSetup() {
			base.SpawnSetup();
			comp = GetComp<CompCustomExplosive>();

			if (flareOverlayNormal != null) return;
			flareOverlayNormal = GraphicDatabase.Get<Graphic_Single>(flareGraphicPath, ShaderDatabase.TransparentPostLight);
			flareOverlayStrong = GraphicDatabase.Get<Graphic_Single>(flareGraphicStrongPath, ShaderDatabase.TransparentPostLight);

			flareOverlayStrong.drawSize = flareOverlayNormal.drawSize = def.graphicData.drawSize;
		}

		public override void ExposeData() {
			base.ExposeData();
			Scribe_Values.LookValue(ref isArmed, "isArmed", false);
			Scribe_Values.LookValue(ref ticksSinceFlare, "ticksSinceFlare", 0);
		}

		public bool WantsFlick() {
			return isArmed != desiredArmState;
		}

		public void DoFlick() {
			isArmed = desiredArmState;
			if (isArmed) {
				DrawFlareOverlay(true);
				armSound.PlayOneShot(Position);
			}
		}

		public override IEnumerable<Gizmo> GetGizmos() {
			var armGizmo = new Command_Toggle();
			armGizmo.toggleAction = ArmGizmoAction;
			armGizmo.isActive = () => desiredArmState;
			armGizmo.icon = UITex_Arm;
			armGizmo.defaultLabel = ArmButtonLabel;
			armGizmo.defaultDesc = ArmButtonDesc;
			armGizmo.hotKey = KeyBindingDef.Named("RemoteExplosiveArm");

			yield return armGizmo;
			foreach (var g in base.GetGizmos()) {
				yield return g;
			}
		}

		private void ArmGizmoAction() {
			desiredArmState = !desiredArmState;
			Find.DesignationManager.RemoveAllDesignationsOn(this);
			if (WantsFlick()) {
				Find.DesignationManager.AddDesignation(new Designation(new TargetInfo(this), DesignationDefOf.Flick));
			}
		}

		public override void Tick() {
			base.Tick();
			ticksSinceFlare++;
			// beep in sync with the flash
			if(FuseLit && ticksSinceFlare == 1) {
				var pitchRamp = (1 - (comp.WickTicksRemaining / (float)comp.WickTotalTicks))*.15f;
				EmitBeep(1f + pitchRamp);
			}
		}

		public override void Draw() {
			base.Draw();
			if(isArmed) {
				if(FuseLit) {
					if(ticksSinceFlare>=ticksBetweenBlinksLit) {
						DrawFlareOverlay(true);
					}
				} else if(IsArmed) {
					if (ticksSinceFlare >= ticksBetweenBlinksArmed) {
						DrawFlareOverlay(false);
					}
				}
			}
		}

		private void DrawFlareOverlay(bool useStrong) {
			ticksSinceFlare = 0;

			Graphic overlay = useStrong ? flareOverlayStrong : flareOverlayNormal;
			Matrix4x4 matrix = default(Matrix4x4);
			var s = Vector3.one;
			matrix.SetTRS(DrawPos + Altitudes.AltIncVect, Rotation.AsQuat, s);
			Graphics.DrawMesh(MeshPool.plane10, matrix, overlay.MatAt(Rotation), 0);
		}

		private void EmitBeep(float pitch) {
			var beepInfo = SoundInfo.InWorld(this);
			beepInfo.pitchFactor = pitch;
			beepSound.PlayOneShot(beepInfo);
		}

		public override string GetInspectString() {
			var stringBuilder = new StringBuilder();
			stringBuilder.Append(base.GetInspectString());
			stringBuilder.AppendLine();
			if (IsArmed) {
				stringBuilder.Append("TrapArmed".Translate());
			} else {
				stringBuilder.Append("TrapNotArmed".Translate());
			}
			return stringBuilder.ToString();
		}

	}
}
