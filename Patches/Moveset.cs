using HarmonyLib;
using QueenCrest.Components;
using System;
using System.Linq;
using UnityEngine;
using static QueenCrest.QueenCrestPlugin;
using ConfigGroup = HeroController.ConfigGroup;
using UObj = UnityEngine.Object;

namespace QueenCrest.Patches;

internal class Moveset {

	private static ConfigGroup? YenCG;

	// Awake prefix because this needs to run BEFORE needleforge adds crests
	// but AFTER the bundle for heroconfigs is loaded
	[HarmonyPatch(typeof(ToolItemManager), nameof(ToolItemManager.Awake))]
	[HarmonyPrefix]
	private static void CreateHCConfig() {
		if (YenCrest.AttackConfig != null)
			return;

		const string path = "Assets/Data Assets/HeroController Configs";
		AssetBundle bundle = FindBundleByAssetPath("HeroController Configs");

		// Hunter moveset
		HeroControllerConfig
			config = UObj.Instantiate(bundle.LoadAsset<HeroControllerConfig>($"{path}/Default.asset"));

		// Witch needle art
		HeroControllerConfig
			witch = bundle.LoadAsset<HeroControllerConfig>($"{path}/Whip.asset");

		config.chargeSlashChain = witch.ChargeSlashChain;
		config.chargeSlashRecoils = witch.ChargeSlashRecoils;
		config.chargeSlashLungeSpeed = witch.ChargeSlashLungeSpeed;
		config.chargeSlashLungeDeceleration = witch.ChargeSlashLungeDeceleration;

		// Witch needle art's animations
		GameObject animObj = new($"{YenId}_AnimLib");
		UObj.DontDestroyOnLoad(animObj);
		tk2dSpriteAnimation animLib = animObj.AddComponent<tk2dSpriteAnimation>();
		animLib.clips = [.. witch.heroAnimOverrideLib.clips.Where(c => c.name.Contains("Charged"))];
		witch.heroAnimOverrideLib.GetClipByName("SlashEffect");
		config.heroAnimOverrideLib = animLib;

		config.name = YenId;
		YenCrest.AttackConfig = config;
	}

	[HarmonyPatch(typeof(HeroController), nameof(HeroController.Awake))]
	[HarmonyPostfix]
	private static void CreateCG(HeroController __instance) {
		if (!YenCrest.AttackConfig)
			return;

		Transform
			hunter = __instance.transform.Find("Attacks/Default"),
			witchArt = __instance.transform.Find("Attacks/Charge Slash Witch");

		GameObject root = new($"{YenId}_AttackRoot");
		UObj.DontDestroyOnLoad(root);
		Transform rootT = root.transform;
		rootT.SetParent(hunter.parent);
		rootT.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
		rootT.localScale = Vector3.one;

		YenCG = new ConfigGroup() {
			ActiveRoot = root,
			Config = YenCrest.AttackConfig,

			// Hunter moveset
			NormalSlashObject = UObj.Instantiate(hunter.Find("Slash").gameObject, rootT),
			AlternateSlashObject = UObj.Instantiate(hunter.Find("AltSlash").gameObject, rootT),
			UpSlashObject = UObj.Instantiate(hunter.Find("UpSlash").gameObject, rootT),
			DownSlashObject = UObj.Instantiate(hunter.Find("DownSlash").gameObject, rootT),
			WallSlashObject = UObj.Instantiate(hunter.Find("WallSlash").gameObject, rootT),
			DashStab = UObj.Instantiate(hunter.Find("Dash Stab").gameObject, rootT),

			// Witch needle art
			ChargeSlash = UObj.Instantiate(witchArt.gameObject, rootT),
		};
		YenCG.Setup();

		foreach (var animator in YenCG.ChargeSlash.GetComponents<tk2dSpriteAnimator>())
			animator.Library = YenCrest.AttackConfig.heroAnimOverrideLib;

		// Apply custom attack colour
		var tinter = root.AddComponent<TintRendererGroupConditionally>();
		tinter.Condition = () => HeroController.instance.NailImbuement.CurrentElement == NailElements.None;
		tinter.Color = AttackColor;

		root.SetActive(true);

		// Add the config group to the list of configs
		__instance.configs = [
			.. __instance.configs,
			YenCG
		];
	}

	#region Utils

	/// <summary>
	/// Returns the first loaded asset bundle containing an asset with the provided
	/// partial path.
	/// </summary>
	private static AssetBundle FindBundleByAssetPath(string path) {
		foreach (AssetBundle bundle in AssetBundle.GetAllLoadedAssetBundles())
			if (bundle.GetAllAssetNames().Any(n => n.Contains(path)))
				return bundle;
		throw new ArgumentException("No bundle containing that partial asset path is loaded");
	}

	#endregion

}
