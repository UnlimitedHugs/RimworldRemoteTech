using System.Collections.Generic;
using System.Text;
using RimWorld;
using Verse;
using Verse.Sound;

namespace RemoteTech {
	public enum RemoteExplosiveType {
		Combat, Mining, Utility
	}

	/* 
	 * The base class for all wireless remote explosives.
	 * Requires a CompCustomExplosive to work correctly. Can be armed and assigned to a channel.
	 * Will blink with an overlay texture when armed.
	 */
	public class Building_RemoteExplosive : Building, ISwitchable, IWirelessDetonationReceiver, IAutoReplaceExposable {

		private static readonly string ArmButtonLabel = "RemoteExplosive_arm_label".Translate();
		private static readonly string ArmButtonDesc = "RemoteExplosive_arm_desc".Translate();
		
		protected bool beepWhenLit = true;

		private CompCustomExplosive explosiveComp;
		private CompAutoReplaceable replaceComp;
		private CompChannelSelector channelsComp;

		private bool desiredArmState;
		private bool isArmed;
		private int ticksSinceFlare;

		public bool CanReceiveWirelessSignal {
			get { return IsArmed && !FuseLit; }
		}

		private BuildingProperties_RemoteExplosive _customProps;
		private BuildingProperties_RemoteExplosive CustomProps {
			get {
				if (_customProps == null) {
					_customProps = (def.building as BuildingProperties_RemoteExplosive) ?? new BuildingProperties_RemoteExplosive();
				}
				return _customProps;
			}
		}

		private GraphicData_Blinker BlinkerData {
			get { return Graphic.data as GraphicData_Blinker; }
		}

		public bool IsArmed {
			get { return isArmed; }
		}

		public bool FuseLit {
			get { return explosiveComp.WickStarted; }
		}

		public int CurrentChannel {
			get { return channelsComp != null ? channelsComp.Channel : RemoteTechUtility.DefaultChannel; }
		}

		public virtual void LightFuse() {
			if(FuseLit) return;
			explosiveComp.StartWick(true);
		}

		public override void SpawnSetup(Map map, bool respawningAfterLoad) {
			base.SpawnSetup(map, respawningAfterLoad);
			this.UpdateSwitchDesignation();
			explosiveComp = GetComp<CompCustomExplosive>();
			replaceComp = GetComp<CompAutoReplaceable>()?.DisableGizmoAutoDisplay();
			channelsComp = GetComp<CompChannelSelector>()?.Configure(true);
			this.RequireComponent(CustomProps);
			this.RequireComponent(BlinkerData);
			if (!respawningAfterLoad && CustomProps != null) {
				if (CustomProps.explosiveType == RemoteExplosiveType.Combat && RemoteTechController.Instance.SettingAutoArmCombat ||
					CustomProps.explosiveType == RemoteExplosiveType.Mining && RemoteTechController.Instance.SettingAutoArmMining ||
					CustomProps.explosiveType == RemoteExplosiveType.Utility && RemoteTechController.Instance.SettingAutoArmUtility) {
					Arm();
				}
			}
		}
		
		public override void ExposeData() {
			base.ExposeData();
			Scribe_Values.Look(ref isArmed, "isArmed");
			Scribe_Values.Look(ref ticksSinceFlare, "ticksSinceFlare");
			Scribe_Values.Look(ref desiredArmState, "desiredArmState");
		}

		public bool WantsSwitch() {
			return isArmed != desiredArmState;
		}

		public void DoSwitch() {
			if (isArmed != desiredArmState) {
				if (!isArmed) {
					Arm();
				} else {
					Disarm();
				}
			}
		}

		protected override void ReceiveCompSignal(string signal) {
			base.ReceiveCompSignal(signal);
			if(signal == CompChannelSelector.ChannelChangedSignal) Resources.Sound.rxChannelChange.PlayOneShot(this);
		}

		public void Arm() {
			if(IsArmed) return;
			DrawFlareOverlay(true);
			Resources.Sound.rxArmed.PlayOneShot(this);
			desiredArmState = true;
			isArmed = true;
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
				icon = Resources.Textures.rxUIArm,
				defaultLabel = ArmButtonLabel,
				defaultDesc = ArmButtonDesc,
				hotKey = Resources.KeyBinging.rxArm
			};
			yield return armGizmo;

			if (channelsComp != null) {
				channelsComp.Configure(true, false, false, RemoteTechUtility.GetChannelsUnlockLevel());
				var gz = channelsComp.GetChannelGizmo();
				if (gz != null) yield return gz;
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
					icon = Resources.Textures.rxUIArm,
					defaultLabel = "DEV: Toggle armed"
				};
				yield return new Command_Action {
					action = () => { 
						Arm();
						LightFuse();
					},
					icon = Resources.Textures.rxUIDetonate,
					defaultLabel = "DEV: Detonate now"
				};
			}

			foreach (var g in base.GetGizmos()) {
				yield return g;
			}
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
				if (ticksSinceFlare >= BlinkerData.blinkerIntervalActive) {
					DrawFlareOverlay(true);
				}
			} else {
				if (ticksSinceFlare >= BlinkerData.blinkerIntervalNormal) {
					DrawFlareOverlay(false);
				}
			}
		}

		public override string GetInspectString() {
			var stringBuilder = new StringBuilder();
			stringBuilder.Append(base.GetInspectString());
			if (IsArmed) {
				stringBuilder.Append("RemoteExplosive_armed".Translate());
			} else {
				stringBuilder.Append("RemoteExplosive_notArmed".Translate());
			}
			if (channelsComp != null && RemoteTechUtility.GetChannelsUnlockLevel() > RemoteTechUtility.ChannelType.None) {
				stringBuilder.AppendLine();
				stringBuilder.Append(RemoteTechUtility.GetCurrentChannelInspectString(channelsComp.Channel));
			}
			return stringBuilder.ToString();
		}

		public void ReceiveWirelessSignal(Thing sender) {
			LightFuse();
		}

		public void ExposeAutoReplaceValues(AutoReplaceWatcher watcher) {
			var armed = IsArmed;
			watcher.ExposeValue(ref armed, "armed");
			if (watcher.ExposeMode == LoadSaveMode.LoadingVars && armed) {
				Arm();
			}
		}

		private void ArmGizmoAction() {
			desiredArmState = !desiredArmState;
			this.UpdateSwitchDesignation();
		}

		private void DrawFlareOverlay(bool useStrong) {
			ticksSinceFlare = 0;
			var overlay = useStrong ? Resources.Graphics.FlareOverlayStrong : Resources.Graphics.FlareOverlayNormal;
			RemoteTechUtility.DrawFlareOverlay(overlay, DrawPos, BlinkerData);
		}

		private void EmitBeep(float pitch) {
			var beepInfo = SoundInfo.InMap(this);
			beepInfo.pitchFactor = pitch;
			Resources.Sound.rxBeep.PlayOneShot(beepInfo);
		}
	}
}
