using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Needleforge;
using Needleforge.Data;
using QueenCrest.Patches;
using TeamCherry.Localization;
using UnityEngine;
using static QueenCrest.Utils.ResourceUtils;

namespace QueenCrest;

/*
TODO:
- custom slash effect anims
- custom hud frame
*/

[BepInAutoPlugin(id: "io.github.kaycodes13.queencrest")]
[BepInDependency("org.silksong-modding.i18n")]
[BepInDependency(NeedleforgePlugin.Id, "0.6.1")]
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

		Vector2 pivot = new(0.5f, 0.44f);
		Sprite
			linework = LoadEmbeddedPngAsSprite("Crest.png", pivot),
			silhouette = LoadEmbeddedPngAsSprite("CrestSilhouette.png", pivot),
			glow = LoadEmbeddedPngAsSprite("CrestGlow.png", pivot, 140f);

		float slotOffset = (0.5f - pivot.y) * (linework.rect.height / linework.pixelsPerUnit);

		YenCrest = NeedleforgePlugin.AddCrest($"{YenId}", YenName, YenDesc, linework, silhouette, glow);
		#endregion

		#region Tool Slots

		YenCrest.AddSkillSlot(AttackToolBinding.Up,    new(-0.9f,  1.85f + slotOffset), false);
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

		YenCrest.BindCompleteEvent = Bind.EnableReaperBindEffect;
		Harmony.PatchAll(typeof(Bind));

		Harmony.PatchAll(typeof(Moveset));
		#endregion
	}

	private void OnDestroy() => Harmony.UnpatchSelf();
}
