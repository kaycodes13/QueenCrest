using HarmonyLib;
using QueenCrest.Components;
using System.Linq;
using UnityEngine;
using static QueenCrest.QueenCrestPlugin;
using ConfigGroup = HeroController.ConfigGroup;
using UObj = UnityEngine.Object;

namespace QueenCrest.Patches;

internal class Moveset {

	[HarmonyPatch(typeof(HeroController), nameof(HeroController.Awake))]
	[HarmonyPostfix]
	private static void Setup(HeroController __instance) {
		if (!YenCrest.AttackConfig) {
			HeroControllerConfig
				config = UObj.Instantiate(__instance.configs.First(c => c.Config.name == "Default").Config),
				witch = __instance.configs.First(c => c.Config.name == "Whip").Config;

			var animObj = new GameObject($"{YenId}_AnimLib");
			var animLib = animObj.AddComponent<tk2dSpriteAnimation>();
			animLib.clips = [..
				witch.heroAnimOverrideLib.clips.Where(c => c.name.Contains("Charged"))
			];
			UObj.DontDestroyOnLoad(animObj);

			config.name = YenId;
			config.heroAnimOverrideLib = animLib;
			config.chargeSlashChain = witch.ChargeSlashChain;
			config.chargeSlashRecoils = witch.ChargeSlashRecoils;
			config.chargeSlashLungeSpeed = witch.ChargeSlashLungeSpeed;
			config.chargeSlashLungeDeceleration = witch.ChargeSlashLungeDeceleration;

			YenCrest.AttackConfig = config;
			YenCrest.ToolCrest.heroConfig = config;
		}

		Transform
			hunter = __instance.transform.Find("Attacks/Default"),
			witchStrike = __instance.transform.Find("Attacks/Charge Slash Witch");

		GameObject root = UObj.Instantiate(hunter.gameObject, hunter.parent);
		root.name = $"{YenId}_Attacks";

		var strike = UObj.Instantiate(witchStrike.gameObject, root.transform);
		foreach (var animator in strike.GetComponents<tk2dSpriteAnimator>())
			animator.Library = YenCrest.AttackConfig.heroAnimOverrideLib;

		var tinter = root.AddComponent<TintRendererGroupConditionally>();
		tinter.Condition = () => HeroController.instance.NailImbuement.CurrentElement == NailElements.None;
		tinter.Color = AttackColor;

		ConfigGroup yenCG = new() {
			ActiveRoot = root,
			Config = YenCrest.AttackConfig,

			NormalSlashObject = root.transform.Find("Slash").gameObject,
			AlternateSlashObject = root.transform.Find("AltSlash").gameObject,
			UpSlashObject = root.transform.Find("UpSlash").gameObject,
			DownSlashObject = root.transform.Find("DownSlash").gameObject,
			WallSlashObject = root.transform.Find("WallSlash").gameObject,
			DashStab = root.transform.Find("Dash Stab").gameObject,
			ChargeSlash = strike,
		};

		__instance.configs = [.. __instance.configs, yenCG ];

		yenCG.Setup();
	}

}
