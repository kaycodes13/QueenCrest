using HarmonyLib;
using QueenCrest.Components;
using System.Linq;
using UnityEngine;
using static QueenCrest.QueenCrestPlugin;
using ConfigGroup = HeroController.ConfigGroup;
using UObj = UnityEngine.Object;

namespace QueenCrest.Patches;

internal class Moveset {

	private static GameObject? AttackTemplates;

	[HarmonyPatch(typeof(HeroController), nameof(HeroController.Awake))]
	[HarmonyPostfix]
	private static void Setup(HeroController __instance) {
		if (!YenCrest.AttackConfig || !AttackTemplates) {
			HeroControllerConfig config = BuildConfig(__instance);
			config.heroAnimOverrideLib = BuildAnimations(__instance);

			YenCrest.AttackConfig = config;
			YenCrest.ToolCrest.heroConfig = config;

			AttackTemplates = BuildAttacks(__instance, config);
		}

		GameObject root = UObj.Instantiate(AttackTemplates, __instance.transform.Find("Attacks"));

		ConfigGroup yenCG = new() {
			ActiveRoot = root,
			Config = YenCrest.AttackConfig,

			NormalSlashObject = root.transform.Find("Slash").gameObject,
			AlternateSlashObject = root.transform.Find("AltSlash").gameObject,
			UpSlashObject = root.transform.Find("UpSlash").gameObject,
			DownSlashObject = root.transform.Find("DownSlash").gameObject,
			WallSlashObject = root.transform.Find("WallSlash").gameObject,
			DashStab = root.transform.Find("Dash Stab").gameObject,
			ChargeSlash = root.transform.Find("ChargeSlash").gameObject,
		};

		__instance.configs = [.. __instance.configs, yenCG ];

		yenCG.Setup();
	}

	private static HeroControllerConfig BuildConfig(HeroController hc) {
		HeroControllerConfig
			config = UObj.Instantiate(hc.configs.First(c => c.Config.name == "Default").Config),
			witch = hc.configs.First(c => c.Config.name == "Whip").Config;

		config.name = YenId;
		config.heroAnimOverrideLib = animLib;
		config.chargeSlashChain = witch.ChargeSlashChain;
		config.chargeSlashRecoils = witch.ChargeSlashRecoils;
		config.chargeSlashLungeSpeed = witch.ChargeSlashLungeSpeed;
		config.chargeSlashLungeDeceleration = witch.ChargeSlashLungeDeceleration;

		return config;
	}

	private static tk2dSpriteAnimation BuildAnimations(HeroController hc) {
		tk2dSpriteAnimation
			witch = hc.configs.First(c => c.Config.name == "Whip").Config.heroAnimOverrideLib;

		var animObj = new GameObject($"{YenId}_AnimLib");
		var animLib = animObj.AddComponent<tk2dSpriteAnimation>();
		animLib.clips = [..witch.clips.Where(c => c.name.Contains("Charged"))];
		UObj.DontDestroyOnLoad(animObj);

		return animLib;
	}

	private static GameObject BuildAttacks(HeroController hc, HeroControllerConfig config) {
		Transform
			hunter = hc.transform.Find("Attacks/Default"),
			witchStrike = hc.transform.Find("Attacks/Charge Slash Witch");

		GameObject attacks = UObj.Instantiate(hunter.gameObject, hunter.parent);
		attacks.name = $"{YenId}_Attacks";
		attacks.transform.SetParent(null);
		UObj.DontDestroyOnLoad(attacks);

		var strike = UObj.Instantiate(witchStrike.gameObject, witchStrike.transform.parent);
		strike.name = "ChargeSlash";
		strike.transform.SetParent(attacks.transform);
		foreach (var animator in strike.GetComponents<tk2dSpriteAnimator>())
			animator.Library = config.heroAnimOverrideLib;

		var tinter = attacks.AddComponent<TintRendererGroupConditionally>();
		tinter.Condition = () => HeroController.instance.NailImbuement.CurrentElement == NailElements.None;
		tinter.Color = AttackColor;

		return attacks;
	}

}
