using Needleforge.Data;
using QueenCrest.Components;
using QueenCrest.Data;
using System.Collections;
using System.Linq;
using UnityEngine;
using static QueenCrest.QueenCrestPlugin;
using ConfigGroup = HeroController.ConfigGroup;

namespace QueenCrest.Patches;

internal static class Moveset {

	private static GameObject? animLibObj = null;

	internal static void Setup() {
		// By default, Needleforge v0.8 copies most of hunter's moveset and config for
		// crests without custom attacks. So as a hotfix I'm just going to set up
		// witch charged slash and leave the other defaults in place.

		AddHeroOverrideAnims();
		EditHeroConfig();
		CreateMissingAttacks();
	}

	/// <summary>
	/// Copies charged-slash related configuration from Witch to Queen Crest.
	/// The rest of Hunter's config is provided by default by Needleforge.
	/// </summary>
	private static void EditHeroConfig() {
		HeroConfigNeedleforge yenConfig = YenCrest.Moveset.HeroConfig!;
		HeroControllerConfig witchConfig = ToolItemManager.GetCrestByName("Witch").HeroConfig;

		yenConfig.SetChargedSlashFields(
			chain: witchConfig.chargeSlashChain,
			lungeSpeed: witchConfig.chargeSlashLungeSpeed,
			lungeDeceleration: witchConfig.chargeSlashLungeDeceleration,
			recoils: witchConfig.chargeSlashRecoils
		);
		yenConfig.heroAnimOverrideLib = CustomAnimations.library;
	}

	/// <summary>
	/// Creates the alt slash and charged slash for Queen Crest, from Hunter and Witch.
	/// The rest of the Hunter attack objects are provided by default by Needleforge.
	/// </summary>
	private static void CreateMissingAttacks() {
		HeroController hc = HeroController.instance;
		HeroConfigNeedleforge yenConfig = YenCrest.Moveset.HeroConfig!;
		ConfigGroup
			yenCg = YenCrest.Moveset.ConfigGroup!,
			witchCg = hc.configs.First(x => x.Config.name == "Whip"),
			hunterCg = hc.configs.First(x => x.Config.name == "Default");

		GameObject queenStrike = Object.Instantiate(witchCg.ChargeSlash, yenCg.ActiveRoot.transform);
		foreach (var animator in queenStrike.GetComponents<tk2dSpriteAnimator>())
			animator.Library = CustomAnimations.library;

		yenCg.ChargeSlash = queenStrike;
		yenCg.AlternateSlashObject = Object.Instantiate(hunterCg.AlternateSlashObject, yenCg.ActiveRoot.transform);
		
		var tinter = yenCg.ActiveRoot.AddComponent<TintRendererGroupConditionally>();
		tinter.Condition = () =>
			HeroController.instance.NailImbuement.CurrentElement == NailElements.None;
		tinter.Color = AttackColor;
	}

	private static bool addedHeroAnims = false;

	/// <summary>
	/// Adds Witch crest's charged slash animations - both the Hornet overrides and the
	/// effect swooshes - to the animation library used by all attacks, etc.
	/// </summary>
	private static void AddHeroOverrideAnims() {
		if (addedHeroAnims)
			return;

		HeroController hc = HeroController.instance;
		tk2dSpriteAnimation
			witch = hc.configs.First(c => c.Config.name == "Whip").Config.heroAnimOverrideLib;

		CustomAnimations.library.clips = [
			.. CustomAnimations.library.clips,
			.. witch.clips.Where(x => x.name.Contains("Charged"))
		];
		CustomAnimations.library.isValid = false;
		CustomAnimations.library.ValidateLookup();
		addedHeroAnims = true;
	}

}
