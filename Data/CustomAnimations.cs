using Newtonsoft.Json;
using Silksong.UnityHelper.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using WrapMode = tk2dSpriteAnimationClip.WrapMode;

namespace QueenCrest.Data;

/// <summary>
/// Automatically imports all custom animations defined in the .json files under the Assets/Animations folder.
/// </summary>
internal static class CustomAnimations {
	internal static readonly tk2dSpriteCollectionData spriteCollection;
	internal static readonly tk2dSpriteAnimation library;

	private const string animsPath = $"{nameof(QueenCrest)}.Assets.Animations";

	static CustomAnimations() {
		Assembly asm = Assembly.GetExecutingAssembly();

		using (StreamReader reader = new(asm.GetManifestResourceStream($"{animsPath}.SpriteDefinitions.json"))) {
			SpriteDefinition[] frameDatas = JsonConvert.DeserializeObject<SpriteDefinition[]>(reader.ReadToEnd())!;
			IEnumerable<Texture2D> texes = frameDatas.Select(x => SpriteUtil.LoadEmbeddedTexture(asm, $"{animsPath}.{x.Path}"));

			spriteCollection = Tk2dUtil.CreateTk2dSpriteCollection(
				texes,
				spriteNames: frameDatas.Select(x => x.Path),
				spriteCenters: frameDatas.Select(x => x.Pivot)
			);
		}
		UnityEngine.Object.DontDestroyOnLoad(spriteCollection);
		spriteCollection.hideFlags = HideFlags.HideAndDontSave;
		spriteCollection.gameObject.name = $"{QueenCrestPlugin.YenId} Animations";

#if DEBUG
		string outpath = Path.Combine(Path.GetDirectoryName(asm.Location), "collection_atlas.png");
		Texture2D outtex = SpriteUtil.GetReadableTexture((Texture2D)spriteCollection.textures[0]);
		File.WriteAllBytes(outpath, outtex.EncodeToPNG());
#endif

		AnimDefinition[] animDatas;
		using (StreamReader reader = new(asm.GetManifestResourceStream($"{animsPath}.AnimDefinitions.json"))) {
			animDatas = JsonConvert.DeserializeObject<AnimDefinition[]>(reader.ReadToEnd())!;
		}

		List<tk2dSpriteAnimationClip> clips = [];

		foreach(AnimDefinition animData in animDatas) {
			tk2dSpriteAnimationClip anim = new() {
				name = animData.Name,
				fps = animData.Fps,
				wrapMode = animData.WrapMode,
				frames = spriteCollection.CreateFrames(animData.Frames),
			};
			foreach (int index in animData.Triggers) {
				anim.frames[index].triggerEvent = true;
			}
			clips.Add(anim);
		}

		library = spriteCollection.gameObject.AddComponent<tk2dSpriteAnimation>();
		library.clips = [.. clips];
	}

	/// <summary>
	/// Deserialization struct for an entry in a <see cref="tk2dSpriteCollectionData"/>.
	/// </summary>
	/// <param name="Path">
	///		Embedded resource path (relative to <see cref="animsPath"/> of the sprite.
	///	</param>
	/// <param name="Pivot">Pivot/center point of the sprite, in pixels.</param>
	[Serializable]
	private record struct SpriteDefinition(string Path, Vector2 Pivot);

	/// <summary>
	/// Deserialization struct for a custom animation.
	/// </summary>
	/// <param name="Frames">
	///		List of <see cref="SpriteDefinition.Path"/>s of the frames of this animation.
	///	</param>
	/// <param name="Triggers">
	///		A list of frame indexes which should trigger an event.
	///	</param>
	[Serializable]
	private record struct AnimDefinition(string Name, int Fps, WrapMode WrapMode, string[] Frames, int[] Triggers);
}
