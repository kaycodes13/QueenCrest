using GlobalSettings;
using HarmonyLib;
using HutongGames.PlayMaker;
using System;
using System.Collections;
using UnityEngine;
using static QueenCrest.QueenCrestPlugin;
using ProbabilityInt = Probability.ProbabilityInt;

namespace QueenCrest.Patches;

internal static class Bind {
	/*
	For the sake of robustness & customizability, I'm choosing to recreate Reaper bind
	functionality from scratch.
	*/

	/// <summary>
	/// Determines how many silk orbs are spawned upon hitting an enemy during the
	/// Reaper bind effect.
	/// </summary>
	/// <remarks>
	/// Because the hunter moveset attacks faster than reaper's, and witch needle art
	/// multihits, I'm nerfing the orb drop rates for balance.
	/// </remarks>
	private static readonly ProbabilityInt[] silkOrbDrops = [
		new ProbabilityInt {
			Value = 1,
			Probability = 0.50f // original: 0.35f
		},
		new ProbabilityInt {
			Value = 2,
			Probability = 0.35f // original: 0.5f
		},
		new ProbabilityInt {
			Value = 3,
			Probability = 0.15f // original: 0.15f
		}
	];

	private static int GetRandomOrbDrops()
		=> Probability.GetRandomItemByProbability<ProbabilityInt, int>(silkOrbDrops);

	/// <summary>
	/// Enables the visual effect of the reaper bind on the HUD while the bind effect is active.
	/// </summary>
	internal static IEnumerator HudCoroutine(BindOrbHudFrame hudInstance) {
		var hc = HeroController.instance;
		bool wasInReaperMode = false;
		float effectTime = 0f;

		while (true) {
			if (hc.IsPaused()) {
				yield return null;
				continue;
			}

			var effect = hudInstance.reaperModeEffect;
			if (effect) {
				if (!wasInReaperMode && hc.reaperState.IsInReaperMode) {
					effect.gameObject.SetActive(false);
					effect.gameObject.SetActive(true);
					effect.AlphaSelf = 1f;
					effectTime = 0f;
				}
				else if (wasInReaperMode && !hc.reaperState.IsInReaperMode) {
					effectTime = effect.FadeTo(0f, hudInstance.reaperModeEffectFadeOutTime);
				}

				if (effectTime > 0f) {
					effectTime -= Time.deltaTime;
					if (effectTime <= 0)
						effect.gameObject.SetActive(false);
				}
			}

			wasInReaperMode = hc.reaperState.IsInReaperMode;
			yield return null;
		}
	}

	internal static void EnableMultibinder(FsmInt value, FsmInt amount, FsmFloat time, PlayMakerFSM _) {
		if (Gameplay.MultibindTool.IsEquipped) {
			value.Value = 2;
			amount.Value = 2;
			time.Value = 0.8f;
		}
	}

	/// <summary>
	/// Initializes the Reaper bind state, which is referenced in many places to enable
	/// its functionality and visuals. Also enables the visual effect around the player.
	/// </summary>
	internal static void EnableReaperBindEffect() {
		if (!YenCrest.IsEquipped)
			return;

		var hc = HeroController.instance;

		hc.reaperState.IsInReaperMode = true;
		hc.reaperState.ReaperModeDurationLeft = Gameplay.ReaperModeDuration;
		if (hc.reaperModeEffect) {
			hc.reaperModeEffect.gameObject.SetActive(false);
			hc.reaperModeEffect.gameObject.SetActive(true);
		}
	}

	/// <summary>
	/// Spawns silk bundles when the player hits an enemy during this crest's imitation
	/// Reaper bind effect.
	/// </summary>
	[HarmonyPatch(typeof(HealthManager), nameof(HealthManager.TakeDamage), [typeof(HitInstance)])]
	[HarmonyPostfix]
	private static void SpawnSilkOrbs(HealthManager __instance, ref HitInstance hitInstance) {
		if (!YenCrest.IsEquipped)
			return;

		HealthManager healthMgr = __instance;
		HeroController heroCtrl = HeroController.instance;

		// Determine if orbs should spawn
		// Inverted conditions to avoid an indent level
		if (!(
			(
				hitInstance.AttackType == AttackTypes.Nail
				|| hitInstance.AttackType == AttackTypes.Heavy && hitInstance.IsNailTag
			)
			&& healthMgr.enemyType != HealthManager.EnemyTypes.Shade
			&& healthMgr.enemyType != HealthManager.EnemyTypes.Armoured
			&& !healthMgr.DoNotGiveSilk

			&& hitInstance.SilkGeneration != HitSilkGeneration.None
			&& heroCtrl.ReaperState.IsInReaperMode
		)) {
			return;
		}

		// Determine number of orbs
		int amount = healthMgr.reaperBundles switch {
			HealthManager.ReaperBundleTiers.Normal => GetRandomOrbDrops(),
			HealthManager.ReaperBundleTiers.Reduced => 1,
			HealthManager.ReaperBundleTiers.None => 0,
			_ => throw new Exception("Something has gone terribly wrong."),
		};
		if (amount == 0)
			return;

		// Determine location, angle, and speed of orbs' spawn and fling

		int cardinalDirection = DirectionUtils.GetCardinalDirection(hitInstance.GetActualDirection(healthMgr.transform, HitInstance.TargetType.Regular));
		float degrees, angleMin = 0f, angleMax = 360f;

		if (healthMgr.flingSilkOrbsAimObject != null) {
			Vector3 posOrbAim = healthMgr.flingSilkOrbsAimObject.transform.position,
				posMgr = healthMgr.transform.position;
			float x = posOrbAim.x - posMgr.x,
				y = posOrbAim.y - posMgr.y;
			degrees = Mathf.Atan2(y, x) * 57.2957764f;
			angleMin = degrees - 45f;
			angleMax = degrees + 45f;
		}
		else if (healthMgr.flingSilkOrbsDown) {
			angleMin = 225f;
			angleMax = 315f;
			degrees = 270f;
		}
		else {
			(angleMin, angleMax, degrees) = cardinalDirection switch {
				DirectionUtils.Right => (315f, 415f, 0f),
				DirectionUtils.Up => (45f, 135f, 90f),
				DirectionUtils.Left => (125f, 225f, 180f),
				DirectionUtils.Down => (225f, 315f, 270f),
				_ => (angleMin, angleMax, 0f)
			};
		}

		// Actually spawn and fling orbs

		FlingUtils.SpawnAndFling(new FlingUtils.Config {
			Prefab = Gameplay.ReaperBundlePrefab,
			AmountMin = amount,
			AmountMax = amount,
			SpeedMin = 25f,
			SpeedMax = 50f,
			AngleMin = angleMin,
			AngleMax = angleMax
		}, healthMgr.transform, healthMgr.effectOrigin);

		GameObject reapHitEffectPrefab = Effects.ReapHitEffectPrefab;
		if (reapHitEffectPrefab) {
			(Vector2 original, float rotation) = DirectionUtils.GetCardinalDirection(degrees) switch {
				DirectionUtils.Up => (Vector2.one, 90f),
				DirectionUtils.Left => (new(-1f, 1f), 0f),
				DirectionUtils.Down => (new(1f, -1f), 90f),
				_ => (Vector2.one, 0f)
			};
			GameObject reapHitEffect =
				reapHitEffectPrefab.Spawn(
					healthMgr.transform.TransformPoint(healthMgr.effectOrigin)
				);
			reapHitEffect.transform.SetRotation2D(rotation);
			reapHitEffect.transform.localScale =
				original.ToVector3(reapHitEffect.transform.localScale.z);
		}
	}

}
