using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEditor;

namespace Mfuscator {

	internal sealed class MfuscatorSettings : EditorWindow {

		// global settings
		public static bool enable;
		public static bool experimental;
		public static int callbackOrder;

		// cache
		private static string _libVersion = "?";

		[MenuItem("Window/" + nameof(Mfuscator) + " Settings", priority = 502)]
		private static void MenuItemShow() {
			_ = GetWindow<MfuscatorSettings>(utility: false, title: nameof(Mfuscator));
		}

		public static void Load() {
			// 'true' by default
			enable = PlayerPrefs.GetInt(Utils.GetPlayerPrefsKey("enable"), 1) == 1;
			// 'true' by default
			experimental = PlayerPrefs.GetInt(Utils.GetPlayerPrefsKey("experimental"), 1) == 1;
			// '5001' by default
			callbackOrder = PlayerPrefs.GetInt(Utils.GetPlayerPrefsKey("order"), 5001);

			IntPtr verP = MfuscatorBridge.GetVersion();
			_libVersion = Marshal.PtrToStringAnsi(verP);
			MfuscatorBridge.FreeVersionStr(verP);
		}

		public static void Save() {
			PlayerPrefs.SetInt(Utils.GetPlayerPrefsKey("enable"), enable ? 1 : 0);
			PlayerPrefs.SetInt(Utils.GetPlayerPrefsKey("experimental"), experimental ? 1 : 0);
			PlayerPrefs.SetInt(Utils.GetPlayerPrefsKey("order"), callbackOrder);
		}

		private void OnEnable() {
			Load();

			minSize = maxSize = new(256f, 336f);
		}

		private void OnFocus() {
			Load();
		}

		#region GUI
		// cache
		private static readonly GUILayoutOption _maxWidth144 = GUILayout.MaxWidth(144f);

		private static void DrawText(string title, string value, bool flexSpace, Color? color, params GUILayoutOption[] options) {
			GUILayout.BeginHorizontal();
			GUILayout.Label(title);
			GUILayout.Space(2f);
			// NOTE
			GUILayout.Label(color.HasValue ? $"<color=#{ColorUtility.ToHtmlStringRGB(color.Value)}>{value}</color>" : value, new GUIStyle(EditorStyles.boldLabel) { richText = true }, options);
			if (flexSpace) GUILayout.FlexibleSpace();
			GUILayout.EndHorizontal();
		}
		private static bool DrawToggle(string title, bool value, bool flexSpace, params GUILayoutOption[] options) {
			GUILayout.BeginHorizontal();
			GUILayout.Label(title);
			GUILayout.Space(2f);
			value = GUILayout.Toggle(value, string.Empty, options);
			if (flexSpace) GUILayout.FlexibleSpace();
			GUILayout.EndHorizontal();
			return value;
		}
		private static int DrawInt32Field(string title, int value, bool flexSpace, params GUILayoutOption[] options) {
			GUILayout.BeginHorizontal();
			GUILayout.Label(title);
			GUILayout.Space(2f);
			string newStrValue = GUILayout.TextField(value.ToString(), options);
			if (string.IsNullOrWhiteSpace(newStrValue)) value = 0;
			else if (int.TryParse(newStrValue, out int newValue)) value = newValue;
			if (flexSpace) GUILayout.FlexibleSpace();
			GUILayout.EndHorizontal();
			return value;
		}
		#endregion

		private int _indicatorFrame;
		private static readonly string[] _indicatorFrames = new string[] {
			"~",
			"~~",
			"~~~"
		};
		private const float _INDICATOR_MIN_DELAY = .2f;
		private float _lastIndicatorUpdate;

		private void OnGUI() {
			// hot reload support
			if (EditorApplication.isCompiling || EditorApplication.isUpdating || BuildPipeline.isBuildingPlayer) {
				Close();
				return;
			}

			GUIStyle richTextLabel = new(GUI.skin.label) { richText = true };
			GUIStyle richTextHelpBox = new(EditorStyles.helpBox) { richText = true };
			GUIStyle richTextButton = new(GUI.skin.button) { richText = true };

			static void DrawField<T>(Func<T> callback, ref T obj) {
				T newValue = callback.Invoke();
				if (!newValue.Equals(obj)) {
					obj = newValue;
					Save();
				}
			}

			// NOTE: not being updated every frame
			if (Time.unscaledTime - _lastIndicatorUpdate >= _INDICATOR_MIN_DELAY) {
				if (++_indicatorFrame >= _indicatorFrames.Length) _indicatorFrame = 0;
				_lastIndicatorUpdate = Time.unscaledTime;
			}

			// draw
			GUILayout.Space(8f);
			GUILayout.Label("<b>Settings (<color=#aaa>~</color>)</b>", richTextLabel);
			GUILayout.Space(4f);
			DrawText("C++ Lib Version:", _libVersion, true, Color.white);
			DrawField(() => DrawToggle("Enable", enable, true), ref enable);
			DrawField(() => DrawToggle("Experimental Layers (Recommended)", experimental, true), ref experimental);
			DrawField(() => DrawInt32Field("Callback Order", callbackOrder, true, _maxWidth144), ref callbackOrder);
			GUILayout.Space(8f);
			GUILayout.Label("NOTE: You do not need to specify an encryption key because <b>\"MfuscatorPipeline\"</b> generates a strong key for each build automatically.", richTextHelpBox);
			GUILayout.Label($"<color=#00ff00>We work hard to regularly update this asset. Please take a moment to leave a review on the Asset Store page; it will help us a lot.\nThank you! ❤️{_indicatorFrames[_indicatorFrame]}</color>", richTextHelpBox);
			GUILayout.Space(8f);
			if (GUILayout.Button("Restore Original Unity Files")) {
				if (MfuscatorBridge.Restore(MfuscatorPipeline.EditorPath))
					Utils.LogInfo("Cleared");
				else
					Utils.LogWarning("The original files could not be restored");
			}
			if (GUILayout.Button("<color=#aaaaff>Additional Information</color> (Official Site)", richTextButton))
				Application.OpenURL("https://security.mew.icu/");
		}
	}
}
