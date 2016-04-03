using System.Collections.Generic;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace RemoteExplosives {
	public class Building_RemoteExplosive : Building, IFlickable {
		
		private const string flareGraphicPath = "mine_flare";
		private const string flareGraphicStrongPath = "mine_flare_strong";

		private static readonly Texture2D UITex_Arm = ContentFinder<Texture2D>.Get("UIArm");
		private static Graphic flareOverlayNormal;
		private static Graphic flareOverlayStrong;

		private static readonly SoundDef armSound = SoundDef.Named("RemoteExplosiveArmed");
		private static readonly SoundDef beepSound = SoundDef.Named("RemoteExplosiveBeep");
		private static readonly SoundDef changeChannelSound = SoundDef.Named("RemoteChannelChange");

		private static readonly string ArmButtonLabel = "RemoteExplosive_arm_label".Translate();
		private static readonly string ArmButtonDesc = "RemoteExplosive_arm_desc".Translate();

		protected int ticksBetweenBlinksArmed = 100;
		protected int ticksBetweenBlinksLit = 7;
		protected bool beepWhenLit = true;

		private CompCustomExplosive comp;

		private bool desiredArmState;
		private bool isArmed;
		private int ticksSinceFlare;
		private RemoteExplosivesUtility.RemoteChannel currentChannel;
		private RemoteExplosivesUtility.RemoteChannel desiredChannel;

		public bool IsArmed {
			get { return isArmed; }
		}

		public bool FuseLit {
			get { return comp.WickStarted; }
		}

		public RemoteExplosivesUtility.RemoteChannel CurrentChannel {
			get { return currentChannel; }
		}

		public virtual void LightFuse() {
			if(FuseLit) return;
			comp.StartWick(true);
		}

		public override void SpawnSetup() {
			base.SpawnSetup();
			if (flareOverlayNormal == null) {
				flareOverlayNormal = GraphicDatabase.Get<Graphic_Single>(flareGraphicPath, ShaderDatabase.TransparentPostLight);
				flareOverlayStrong = GraphicDatabase.Get<Graphic_Single>(flareGraphicStrongPath, ShaderDatabase.TransparentPostLight);
				flareOverlayStrong.drawSize = flareOverlayNormal.drawSize = def.graphicData.drawSize;
			}

			comp = GetComp<CompCustomExplosive>();
			RemoteExplosivesUtility.UpdateFlickDesignation(this);
		}

		public override void ExposeData() {
			base.ExposeData();
			Scribe_Values.LookValue(ref isArmed, "isArmed", false);
			Scribe_Values.LookValue(ref ticksSinceFlare, "ticksSinceFlare", 0);
			Scribe_Values.LookValue(ref desiredArmState, "desiredArmState", false);
			Scribe_Values.LookValue(ref currentChannel, "currentChannel", RemoteExplosivesUtility.RemoteChannel.White);
			Scribe_Values.LookValue(ref desiredChannel, "desiredChannel", RemoteExplosivesUtility.RemoteChannel.White);
		}

		public bool WantsFlick() {
			return isArmed != desiredArmState || currentChannel!=desiredChannel;
		}

		public void DoFlick() {
			if (isArmed != desiredArmState) {
				if (!isArmed) {
					Arm();
				} else {
					Disarm();
				}
			}
			if(desiredChannel!=currentChannel) {
				currentChannel = desiredChannel;
				changeChannelSound.PlayOneShot(SoundInfo.InWorld(this));
			}
			RemoteExplosivesUtility.UpdateFlickDesignation(this);
		}

		public void Arm() {
			if(IsArmed) return;
			DrawFlareOverlay(true);
			armSound.PlayOneShot(Position);
			desiredArmState = true;
			isArmed = true;
		}

		public void Disarm() {
			if (!IsArmed) return;
			desiredArmState = false;
			isArmed = false;
			comp.StopWick();
		}

		public override IEnumerable<Gizmo> GetGizmos() {
			var armGizmo = new Command_Toggle {
				toggleAction = ArmGizmoAction,
				isActive = () => desiredArmState,
				icon = UITex_Arm,
				defaultLabel = ArmButtonLabel,
				defaultDesc = ArmButtonDesc,
				hotKey = KeyBindingDef.Named("RemoteExplosiveArm")
			};
			yield return armGizmo;

			if (RemoteExplosivesUtility.ChannelsUnlocked()) {
				var channelGizmo = RemoteExplosivesUtility.MakeChannelGizmo(desiredChannel, ChannelGizmoAction);
				yield return channelGizmo;
			}
			
			foreach (var g in base.GetGizmos()) {
				yield return g;
			}
		}

		private void ChannelGizmoAction() {
			desiredChannel = RemoteExplosivesUtility.GetNextChannel(desiredChannel);
			RemoteExplosivesUtility.UpdateFlickDesignation(this);
		}

		private void ArmGizmoAction() {
			desiredArmState = !desiredArmState;
			RemoteExplosivesUtility.UpdateFlickDesignation(this);
		}

		public override void Tick() {
			base.Tick();
			ticksSinceFlare++;
			// beep in sync with the flash
			if (beepWhenLit && FuseLit && ticksSinceFlare == 1) {
				// raise pitch with each beep
				const float maxAdditionalPitch = .15f;
				var pitchRamp = (1 - (comp.WickTicksLeft / (float)comp.WickTotalTicks)) * maxAdditionalPitch;
				EmitBeep(1f + pitchRamp);
			}
		}

		public override void PostApplyDamage(DamageInfo dinfo, float totalDamageDealt) {
			base.PostApplyDamage(dinfo, totalDamageDealt);
			if(dinfo.Def == DamageDefOf.EMP) {
				Disarm();
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
			if (RemoteExplosivesUtility.ChannelsUnlocked()) {
				stringBuilder.AppendLine();
				stringBuilder.Append(RemoteExplosivesUtility.GetCurrentChannelInspectString(currentChannel));
			}
			return stringBuilder.ToString();
		}

	}
}
