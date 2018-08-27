using System;
using System.Text;
using HugsLib.Utils;
using RimWorld;
using UnityEngine;
using Verse;

namespace RemoteExplosives {
	/// <summary>
	/// A cosmetic comp that displays an animated overlay light to simulate the attention of an AI
	/// </summary>
	public class CompAIPilotLight : ThingComp {
		private const float ThingInterestExpirationTime = 4f;
		private const float PawnInterestExpirationTime = 8f;
		private const float AttentionDeficitMutiplier = .05f;
		private const float BlinkMaxInterval = 5f;
		private const float BlinkAnimDuration = .2f;
		private const float OffsetAnimDuration = .5f;
		private const float OffsetDistance = .15f;
		private const float ReportStringCorruptChance = .008f;

		private GraphicData_Blinker blinker;
		private ValueInterpolator offsetXAnim = new ValueInterpolator();
		private ValueInterpolator offsetZAnim = new ValueInterpolator();
		private ValueInterpolator blinkAnim = new ValueInterpolator(1f);
		private ValueInterpolator blinkSquintAnim = new ValueInterpolator(1f);
		private CachedValue<string> inspectString = new CachedValue<string>(MakeCorruptedInspectString, 10);
		private CompPowerTrader powerComp;

		// saved
		private LocalTargetInfo currentTarget = LocalTargetInfo.Invalid;
		private int targetExpirationTick;
		private int nextBlinkTick;

		private bool _enabled = true;
		public bool Enabled {
			get { return _enabled && (powerComp == null || powerComp.PowerOn); }
			set { _enabled = value; }
		}

		private float CurrentTargetInterestTime {
			get { return currentTarget.Thing is Pawn ? PawnInterestExpirationTime : ThingInterestExpirationTime; }
		}

		public override void PostSpawnSetup(bool respawningAfterLoad) {
			base.PostSpawnSetup(respawningAfterLoad);
			blinker = parent.AssertComponent(parent.def.graphicData as GraphicData_Blinker);
			powerComp = parent.GetComp<CompPowerTrader>();
		}

		public override void PostExposeData() {
			base.PostExposeData();
			Scribe_Values.Look(ref currentTarget, "AILightTarget");
			Scribe_Values.Look(ref targetExpirationTick, "AILightExpiration");
			Scribe_Values.Look(ref nextBlinkTick, "AILightBlink");
		}

		public CompAIPilotLight ReportTarget(Thing t) {
			// switch to new target more likely if current target was looked at longer
			var interruptChance = Mathf.Clamp01((targetExpirationTick - GenTicks.TicksGame) / CurrentTargetInterestTime);
			var currentIsPawn = currentTarget.Thing is Pawn;
			var newIsPawn = t is Pawn;
			var newIsHumanlike = currentTarget.Thing is Pawn p && p.RaceProps != null && p.RaceProps.Humanlike;
			if (!currentIsPawn && newIsPawn || newIsHumanlike) interruptChance = 1f;
			if (!currentTarget.IsValid || Rand.Chance(interruptChance * AttentionDeficitMutiplier)) {
				SetLookTarget(new LocalTargetInfo(t));
			}
			return this;
		}

		public CompAIPilotLight ReportTargetLost(Thing t) {
			if (currentTarget.Thing == t) {
				SetLookTarget(new LocalTargetInfo(t.Position), false); // look at last known position
			}
			return this;
		}

		public override string CompInspectStringExtra() {
			return Enabled ? inspectString.Value : null;
		}

		public override void PostDraw() {
			base.PostDraw();
			if (blinker != null && Enabled) {
				var currentTick = GenTicks.TicksGame;
				if (nextBlinkTick <= currentTick) {
					nextBlinkTick = currentTick + Mathf.Round(Rand.Range(BlinkMaxInterval / 2f, BlinkMaxInterval)).SecondsToTicks();
					blinkAnim.StartInterpolation(0f, BlinkAnimDuration / 2f, CurveType.CubicInOut);
					blinkAnim.SetFinishedCallback((interpolator, value, duration, curve) => 
						interpolator.StartInterpolation(1f, duration, curve).SetFinishedCallback(null)
					);
				}
				if (blinkSquintAnim.finished) {
					// squint if target is a dirty humanlike
					var isHumanlike = currentTarget.Thing is Pawn p && p.RaceProps != null && p.RaceProps.Humanlike;
					var targetSquint = isHumanlike ? .5f : 1f;
					if (blinkSquintAnim.value != targetSquint) blinkSquintAnim.StartInterpolation(targetSquint, BlinkAnimDuration, CurveType.CubicInOut);
				}
				if (currentTarget.IsValid && targetExpirationTick <= GenTicks.TicksGame) {
					SetLookTarget(LocalTargetInfo.Invalid, false);
				}
				blinkAnim.UpdateIfUnpaused();
				blinkSquintAnim.UpdateIfUnpaused();
				offsetXAnim.UpdateIfUnpaused();
				offsetZAnim.UpdateIfUnpaused();
				RemoteExplosivesUtility.DrawFlareOverlay(Resources.Graphics.FlareOverlayNormal, 
					parent.DrawPos + new Vector3(offsetXAnim.value, 0, offsetZAnim.value) + Altitudes.AltIncVect, blinker, 1f, blinkAnim.value * blinkSquintAnim.value);
			}
		}

		private void SetLookTarget(LocalTargetInfo info, bool setFreshExpirationTime = true) {
			if (info != currentTarget) {
				currentTarget = info;
				Vector3 lightOffset;
				if (currentTarget.IsValid) {
					lightOffset = (currentTarget.CenterVector3 - parent.TrueCenter()).normalized * OffsetDistance;
				} else {
					lightOffset = Vector3.zero;
				}
				var animDuration = currentTarget.Thing is Pawn ? OffsetAnimDuration / 2f : OffsetAnimDuration;
				offsetXAnim.StartInterpolation(lightOffset.x, animDuration, CurveType.CircularInOut);
				offsetZAnim.StartInterpolation(lightOffset.z, animDuration, CurveType.CircularInOut);
			}
			if (setFreshExpirationTime) {
				targetExpirationTick = GenTicks.TicksGame + Rand.Range(CurrentTargetInterestTime / 2f, CurrentTargetInterestTime).SecondsToTicks();
			}
		}

		private static string MakeCorruptedInspectString() {
			// randomly modify string characters
			var baseString = "proxSensor_AIStatusValue".Translate();
			var sb = new StringBuilder();
			for (var i = 0; i < baseString.Length; i++) {
				var c = baseString[i];
				if (Rand.Chance(ReportStringCorruptChance)) {
					if (Rand.Chance(.15f)) {
						sb.Append(c);
						sb.Append(c);
					} else if (Rand.Chance(.15f)) {
					} else if (Rand.Chance(.20f)) {
						var s = c.ToString();
						var sCap = s.ToUpperInvariant();
						if (sCap == s) {
							s = s.ToLower();
						} else {
							s = sCap;
						}
						sb.Append(s);
					} else {
						sb.Append(Convert.ToChar(Rand.Range(33, 126)));
					}
				} else {
					sb.Append(c);
				}
			}
			return "proxSensor_AIStatusCaption".Translate(sb.ToString());
		}
	}
}