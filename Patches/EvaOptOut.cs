using HarmonyLib;
using HutongGames.PlayMaker.Actions;
using UnityEngine;
using static QueenCrest.QueenCrestPlugin;

namespace QueenCrest.Patches;

internal class EvaOptOut {

	[HarmonyPatch(typeof(CountCrestUnlockPoints), nameof(CountCrestUnlockPoints.OnEnter))]
	[HarmonyPrefix]
	private static void SubtractQueenSlots(CountCrestUnlockPoints __instance) {
		ToolCrestList list = ScriptableObject.CreateInstance<ToolCrestList>();

		foreach (var crest in (ToolCrestList)__instance.CrestList.Value) {
			if (crest.name != YenCrest.name)
				list.Add(crest);
		}

		__instance.CrestList.Value = list;
	}

}
