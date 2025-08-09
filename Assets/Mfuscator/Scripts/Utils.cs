using System.Runtime.CompilerServices;
using UnityEngine;

namespace Mfuscator {

	internal static class Utils {

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static string GetText(object message) {
			return string.Concat('[', nameof(Mfuscator), "] ", message.ToString());
		}
		public static void LogInfo(object message) {
			Debug.Log(GetText(message));
		}
		public static void LogWarning(object message) {
			Debug.LogWarning(GetText(message));
		}
		public static void LogError(object message) {
			Debug.LogError(GetText(message));
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static string GetPlayerPrefsKey(string subKey) {
			return string.Concat(nameof(Mfuscator), '_', subKey);
		}
	}
}
