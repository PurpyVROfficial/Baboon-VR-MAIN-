using System;
using System.Runtime.InteropServices;
using UnityEditor;

namespace Mfuscator {

	[InitializeOnLoad]
	internal static class MfuscatorBridge {

		[DllImport(nameof(Mfuscator), SetLastError = true)]
		public static extern void Prepare(string editorPath, string encKey, bool experimental);

		[DllImport(nameof(Mfuscator), SetLastError = true)]
		public static extern void Modify(string gMFilePath, string editorPath, string encKey);

		[DllImport(nameof(Mfuscator), SetLastError = true)]
		public static extern bool Restore(string editorPath);

		/// <summary>NOTE: don't forget to call the "FreeVersionStr" method after</summary>
		[DllImport(nameof(Mfuscator), SetLastError = true)]
		public static extern IntPtr GetVersion();

		[DllImport(nameof(Mfuscator), SetLastError = true)]
		public static extern void FreeVersionStr(IntPtr pointer);

		// debug
		public delegate void DebugCallback(IntPtr messageP, byte type, int size);
		[DllImport(nameof(Mfuscator), CallingConvention = CallingConvention.Cdecl, SetLastError = true)]
		public static extern void SetDebugCallback(DebugCallback value);
		/// <summary>NOTE: Mfuscator may crash Unity by trying to call a delegate on an outdated pointer</summary>
		[DllImport(nameof(Mfuscator), SetLastError = true)]
		public static extern void ResetDebugCallback();
		[AOT.MonoPInvokeCallback(typeof(DebugCallback))]
		public static void OnDebugCallback(IntPtr messageP, byte type, int size) {
			string message = Marshal.PtrToStringAnsi(messageP, size);
			message = string.Concat("<color=#999999>[C++]</color> ", message);
			switch (type) {
				case 0:
					Utils.LogInfo(message);
					break;
				case 1:
					Utils.LogWarning(message);
					break;
				case 2:
					Utils.LogError(message);
					break;
			}
		}

		static MfuscatorBridge() {
			SetDebugCallback(OnDebugCallback);
			// NOTE: comment for distribution
			//Utils.LogInfo("A debug callback has been registered");
		}
	}
}
