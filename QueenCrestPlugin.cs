using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Needleforge;
using Needleforge.Attacks;
using Needleforge.Data;
using QueenCrest.Data;
using QueenCrest.Patches;
using Silksong.UnityHelper.Util;
using System.Reflection;
using TeamCherry.Localization;
using UnityEngine;

namespace QueenCrest;

/*
TODO:
- custom slash effect anims
- custom hud frame
*/

[BepInAutoPlugin(id: "io.github.kaycodes13.queencrest")]
[BepInDependency("org.silksong-modding.i18n")]
[BepInDependency("io.github.needleforge", "0.8.1")]
public partial class QueenCrestPlugin : BaseUnityPlugin {

	private Harmony Harmony { get; } = new(Id);
	internal static ManualLogSource Log { get; private set; }

	internal static CrestData YenCrest { get; private set; }

	internal const string
		YenId = "Yen";
	internal static readonly LocalisedString
		YenName = new($"Mods.{Id}", "CREST_NAME"),
		YenDesc = new($"Mods.{Id}", "CREST_DESC");

	private static Color AdminGreen { get; }
		= new Color32(r: 89, g: 255, b: 152, a: 255);
	internal static Color AttackColor { get; }
		= Color.Lerp(Color.white, AdminGreen, 0.3f);

	private void Awake() {
		Log = Logger;
		Logger.LogInfo($"Plugin {Name} ({Id}) has loaded!");

		#region Crest Init + Sprites

		Assembly asm = Assembly.GetExecutingAssembly();
		const string spritePath = $"{nameof(QueenCrest)}.Assets.Sprites";
		Vector2 pivot = new(0.5f, 0.44f);
		Sprite
			linework = SpriteUtil.LoadEmbeddedSprite(asm, $"{spritePath}.Crest.png", pixelsPerUnit: 100f, pivot: pivot),
			silhouette = SpriteUtil.LoadEmbeddedSprite(asm, $"{spritePath}.CrestSilhouette.png", pixelsPerUnit: 100f, pivot: pivot),
			glow = SpriteUtil.LoadEmbeddedSprite(asm, $"{spritePath}.CrestGlow.png", pixelsPerUnit: 140f, pivot: pivot);

		float slotOffset = (0.5f - pivot.y) * (linework.rect.height / linework.pixelsPerUnit);

		YenCrest = NeedleforgePlugin.AddCrest($"{YenId}", YenName, YenDesc, linework, silhouette, glow);
		#endregion

		#region Tool Slots

		YenCrest.AddSkillSlot(AttackToolBinding.Up,	new(-0.9f,  1.85f + slotOffset), false);
		YenCrest.AddSkillSlot(AttackToolBinding.Down,  new( 0.93f, 1.93f + slotOffset), false);
		YenCrest.AddRedSlot(AttackToolBinding.Neutral, new( 2.1f,  0.13f + slotOffset), false);
		YenCrest.AddYellowSlot(new(-2.1f,   0.13f + slotOffset), false);
		YenCrest.AddBlueSlot(  new(-1.94f, -1.67f + slotOffset), false);
		YenCrest.AddBlueSlot(  new( 1.94f, -1.67f + slotOffset), false);
		YenCrest.AddBlueSlot(  new( 0.0f,  -2.95f + slotOffset), false);

		YenCrest.ApplyAutoSlotNavigation(slotDimensions: new(1.25f, 0.75f));
		#endregion

		#region Gameplay

		YenCrest.HudFrame.Preset = VanillaCrest.HUNTER_V3;
		YenCrest.HudFrame.Coroutine = Bind.HudCoroutine;

		YenCrest.BindEvent = Bind.EnableMultibinder;
		YenCrest.BindCompleteEvent = Bind.EnableReaperBindEffect;
		Harmony.PatchAll(typeof(Bind));

		YenCrest.Moveset.Slash = new Attack {
			Name = "Slash",
			AnimLibrary = CustomAnimations.library,
			AnimName = "SlashEffect",
			Hitbox = [
				new(-1.41f, 1.14f),
				new(-2.56f, 0.65f),
				new(-3.35f, -0.13f),
				new(-3.34f, -0.83f),
				new(-2.55f, -1.32f),
				new(-1.15f, -1.42f),
				new(0.56f, -1.2f),
				//new(0.25f, -0.62f),
				//new(0.12f, 0.73f),
				new(0.21f, 1.35f),
			],
			Scale = new(0.9f, 1),
		};

		YenCrest.Moveset.AltSlash = new Attack {
			Name = "AltSlash",
			AnimLibrary = CustomAnimations.library,
			AnimName = "SlashEffectAlt",
			Hitbox = [
				new(-3.2f, 0.2f),
				new(-3.41f, -0.22f),
				new(-3.37f, -0.65f),
				new(-2.5f, -1.2f),
				new(-1.2f, -1.35f),
				new(0.36f, -1.25f),
				new(0.4f, 1f),
				new(-0.71f, 0.9f),
				new(-2.05f, 0.68f),
			],
			Scale = new(0.9f, 1),
		};

		YenCrest.Moveset.OnInitialized += Moveset.Setup;

		Harmony.PatchAll(typeof(EvaOptOut));

		#endregion
	}

	private void OnDestroy() => Harmony.UnpatchSelf();
}
