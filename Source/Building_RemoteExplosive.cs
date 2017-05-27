using System.Collections.Generic;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace RemoteExplosives {
	/* 
	 * The base class for all wireless remote explosives.
	 * Requires a CompCustomExplosive to work correctly. Can be armed and assigned to a channel.
	 * Will blink with an overlay texture when armed.
	 */
	[StaticConstructorOnStartup]
	public class Building_RemoteExplosive : Building, ISwitchable {
		
		private const string flareGraphicPath = "mine_flare";
		private const string flareGraphicStrongPath = "mine_flare_strong";

		private static readonly Texture2D UITex_Arm = ContentFinder<Texture2D>.Get("UIArm");
		private static readonly Graphic flareOverlayNormal = GraphicDatabase.Get<Graphic_Single>(flareGraphicPath, ShaderDatabase.TransparentPostLight);
		private static readonly Graphic flareOverlayStrong = GraphicDatabase.Get<Graphic_Single>(flareGraphicStrongPath, ShaderDatabase.TransparentPostLight);
		
		private static readonly string ArmButtonLabel = "RemoteExplosive_arm_label".Translate();
		private static readonly string ArmButtonDesc = "RemoteExplosive_arm_desc".Translate();
		
		protected bool beepWhenLit = true;

		private CompCustomExplosive explosiveComp;
		private CompAutoReplaceable replaceComp;

		private bool desiredArmState;
		private bool isArmed;
		private int ticksSinceFlare;
		private RemoteExplosivesUtility.RemoteChannel currentChannel;
		private RemoteExplosivesUtility.RemoteChannel desiredChannel;

		private bool justCreated;

		private BuildingProperties_RemoteExplosive _customProps;
		private BuildingProperties_RemoteExplosive CustomProps {
			get {
				if (_customProps == null) {
					_customProps = (def.building as BuildingProperties_RemoteExplosive) ?? new BuildingProperties_RemoteExplosive();
				}
				return _customProps;
			}
		}

		public bool IsArmed {
			get { return isArmed; }
		}

		public bool FuseLit {
			get { return explosiveComp.WickStarted; }
		}

		public RemoteExplosivesUtility.RemoteChannel CurrentChannel {
			get { return currentChannel; }
		}

		public virtual void LightFuse() {
			if(FuseLit) return;
			explosiveComp.StartWick(true);
		}

		public override void PostMake() {
			base.PostMake();
			justCreated = true;
		}

		public override void SpawnSetup(Map map, bool respawningAfterLoad) {
			base.SpawnSetup(map, respawningAfterLoad);
			flareOverlayStrong.drawSize = flareOverlayNormal.drawSize = def.graphicData.drawSize;

			RemoteExplosivesUtility.UpdateSwitchDesignation(this);
			explosiveComp = GetComp<CompCustomExplosive>();
			replaceComp = GetComp<CompAutoReplaceable>();
			if (replaceComp != null) replaceComp.DisableGizmoAutoDisplay();
			
			if (justCreated) {
				if (CustomProps.startsArmed) {
					Arm();
				}
				justCreated = false;
			}
		}

		public override void ExposeData() {
			base.ExposeData();
			Scribe_Values.Look(ref isArmed, "isArmed", false);
			Scribe_Values.Look(ref ticksSinceFlare, "ticksSinceFlare", 0);
			Scribe_Values.Look(ref desiredArmState, "desiredArmState", false);
			Scribe_Values.Look(ref currentChannel, "currentChannel", RemoteExplosivesUtility.RemoteChannel.White);
			Scribe_Values.Look(ref desiredChannel, "desiredChannel", RemoteExplosivesUtility.RemoteChannel.White);
		}

		public bool WantsSwitch() {
			return isArmed != desiredArmState || currentChannel!=desiredChannel;
		}

		public void DoSwitch() {
			if (isArmed != desiredArmState) {
				if (!isArmed) {
					Arm();
				} else {
					Disarm();
				}
			}
			if(desiredChannel!=currentChannel) {
				currentChannel = desiredChannel;
				RemoteExplosivesDefOf.RemoteChannelChange.PlayOneShot(this);
			}
			RemoteExplosivesUtility.UpdateSwitchDesignation(this);
		}

		public void Arm() {
			if(IsArmed) return;
			DrawFlareOverlay(true);
			RemoteExplosivesDefOf.RemoteExplosiveArmed.PlayOneShot(this);
			desiredArmState = true;
			isArmed = true;
		}

		public void SetChannel(RemoteExplosivesUtility.RemoteChannel channel) {
			currentChannel = desiredChannel = channel;
		}

		public void Disarm() {
			if (!IsArmed) return;
			desiredArmState = false;
			isArmed = false;
			explosiveComp.StopWick();
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
				var channelGizmo = RemoteExplosivesUtility.MakeChannelGizmo(desiredChannel, currentChannel, ChannelGizmoAction);
				yield return channelGizmo;
			}

			if (replaceComp != null) yield return replaceComp.MakeGizmo();

			if (DebugSettings.godMode) {
				yield return new Command_Action {
					action = () => {
						if (isArmed) {
							Disarm();
						} else {
							Arm();
						}
					},
					icon = UITex_Arm,
					defaultLabel = "DEV: Toggle armed"
				};
				yield return new Command_Action {
					action = () => { 
						Arm();
						LightFuse();
					},
					icon = Building_DetonatorTable.UITex_Detonate,
					defaultLabel = "DEV: Detonate now"
				};
			}

			foreach (var g in base.GetGizmos()) {
				yield return g;
			}
		}

		private void ChannelGizmoAction() {
			desiredChannel = RemoteExplosivesUtility.GetNextChannel(desiredChannel);
			RemoteExplosivesUtility.UpdateSwitchDesignation(this);
		}

		private void ArmGizmoAction() {
			desiredArmState = !desiredArmState;
			RemoteExplosivesUtility.UpdateSwitchDesignation(this);
		}

		public override void Tick() {
			base.Tick();
			ticksSinceFlare++;
			// beep in sync with the flash
			if (beepWhenLit && FuseLit && ticksSinceFlare == 1) {
				// raise pitch with each beep
				const float maxAdditionalPitch = .15f;
				var pitchRamp = (1 - (explosiveComp.WickTicksLeft / (float)explosiveComp.WickTotalTicks)) * maxAdditionalPitch;
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
			if (!isArmed) return;
			if (FuseLit) {
				if (ticksSinceFlare >= CustomProps.blinkerIntervalLit) {
					DrawFlareOverlay(true);
				}
			} else {
				if (ticksSinceFlare >= CustomProps.blinkerIntervalArmed) {
					DrawFlareOverlay(false);
				}
			}
		}

		private void DrawFlareOverlay(bool useStrong) {
			ticksSinceFlare = 0;

			var overlay = useStrong ? flareOverlayStrong : flareOverlayNormal;
			var s = Vector3.one;
			var matrix = Matrix4x4.TRS(DrawPos + Altitudes.AltIncVect + CustomProps.blinkerOffset, Rotation.AsQuat, s);
			Graphics.DrawMesh(MeshPool.plane10, matrix, overlay.MatAt(Rotation), 0);
		}

		private void EmitBeep(float pitch) {
			var beepInfo = SoundInfo.InMap(this);
			beepInfo.pitchFactor = pitch;
			RemoteExplosivesDefOf.RemoteExplosiveBeep.PlayOneShot(beepInfo);
		}

		public override string GetInspectString() {
			var stringBuilder = new StringBuilder();
			stringBuilder.Append(base.GetInspectString());
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
