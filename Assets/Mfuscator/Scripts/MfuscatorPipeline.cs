using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEditor.Android;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace Mfuscator {

	internal sealed class MfuscatorPipeline : IPreprocessBuildWithReport, IPostGenerateGradleAndroidProject, IPostprocessBuildWithReport {

		private const int _ENC_KEY_SUBSTR_MIN = 4, _ENC_KEY_SUBSTR_MAX = 36;
		private static string _encKey;
		private static bool _doNotContinue;

		private static bool IsGoodReport(BuildReport report) {
			return report.summary.result != BuildResult.Failed && report.summary.result != BuildResult.Cancelled;
		}
		private static bool IsIL2CPP(BuildReport report) {
			return PlayerSettings.GetScriptingBackend(report.summary.platformGroup) == ScriptingImplementation.IL2CPP;
		}
		private static bool IsSupported(BuildReport report) {
			return
				report.summary.platform == BuildTarget.StandaloneWindows64 ||
				report.summary.platform == BuildTarget.StandaloneLinux64 ||
				report.summary.platform == BuildTarget.Android;
		}
		public static string EditorPath => EditorApplication.applicationPath.Remove(EditorApplication.applicationPath.LastIndexOf($"/Editor/{Path.GetFileName(EditorApplication.applicationPath)}"));
		private static string GetDataPath(BuildReport report) {
			return report.summary.outputPath.Replace(Path.GetExtension(report.summary.outputPath), "_Data");
		}
		private static string GetMetaFilePathStandalone(BuildReport report) {
			return Path.Combine(GetDataPath(report), "il2cpp_data", "Metadata", "global-metadata.dat");
		}
		private static string GetMetaFilePathAndroid(string basePath) {
			return Path.Combine(basePath, "src", "main", "assets", "bin", "Data", "Managed", "Metadata", "global-metadata.dat");
		}
		private static string GetMetaFileBasePathAndroid() {
			return Path.Combine(UnityEngine.Application.dataPath, "../", "Library", "Bee", "Android", "Prj", "IL2CPP", "Gradle", "unityLibrary");
		}

		public MfuscatorPipeline() {
			// for callback order
			MfuscatorSettings.Load();
		}

		// [Unity]
		public int callbackOrder => MfuscatorSettings.callbackOrder;

		// [Unity]
		public void OnPreprocessBuild(BuildReport report) {
			// reset
			MfuscatorSettings.Load();
			// NOTE: we generate a strong key with a random length EVERY time for extra security
			// -> Guid * 2 = ~64 characters (good enough)
			_encKey = (Guid.NewGuid().ToString("N", null) + Guid.NewGuid().ToString("N", null))[UnityEngine.Random.Range(_ENC_KEY_SUBSTR_MIN, _ENC_KEY_SUBSTR_MAX + 1)..];
			_doNotContinue = false;

			// NOTE: clear cache NO MATTER what
			if (report.summary.platformGroup == BuildTargetGroup.Standalone) {
				string buildPath = Directory.GetParent(report.summary.outputPath).FullName;
				if (Directory.Exists(buildPath))
					Directory.Delete(buildPath, true);
			} else if (report.summary.platformGroup == BuildTargetGroup.Android) {
				string metaFileBasePathAndroid = GetMetaFileBasePathAndroid();
				if (Directory.Exists(metaFileBasePathAndroid))
					Directory.Delete(metaFileBasePathAndroid, true);
			}

			// ignore?
			if (
#if UNITY_SERVER
				true ||
#endif
				!MfuscatorSettings.enable || !IsGoodReport(report) || !IsIL2CPP(report) || !IsSupported(report)) {
				_doNotContinue = true;
				Utils.LogInfo("Ignoring this build");
				return;
			}

			string editorPath = EditorPath;

			// access
			bool CheckAccess() {
				// simple but effective
				string folder = Path.Combine(editorPath, "DELETE_ME");
				string file = Path.Combine(folder, Path.GetRandomFileName());
				try { _ = Directory.CreateDirectory(folder); } catch { return false; }
				try { File.WriteAllBytes(file, new byte[] { 1 }); } catch { return false; }
				try { _ = File.ReadAllBytes(file); } catch { return false; }
				try { File.Move(file, file += 'a'); } catch { return false; }
				try { File.Delete(file); } catch { return false; }
				try { Directory.Delete(folder); } catch { return false; }
				return true;
			}
			bool GrantAccess() {
				EditorUtility.DisplayProgressBar(nameof(Mfuscator), "Granting access...", 0f);
				using Process console = new() {
					StartInfo = new() {
						Verb = "runas",
						Arguments = $"/C icacls \"{editorPath}\" /grant %username%:(OI)(CI)F",
						CreateNoWindow = true,
						UseShellExecute = true,
						FileName = "cmd.exe",
						WindowStyle = ProcessWindowStyle.Hidden
					}
				};
				try {
					console.Start();
				} catch (Win32Exception) {
					Utils.LogError("Canceled by user");

					EditorUtility.ClearProgressBar(); // 2
					return false;
				}
				console.WaitForExit();

				EditorUtility.ClearProgressBar(); // 1
				return true;
			}
			if (!CheckAccess())
				if (UnityEngine.Application.platform != UnityEngine.RuntimePlatform.WindowsEditor || UnityEngine.Application.isBatchMode) {
					_doNotContinue = true;
					Utils.LogError($"The current system user does not have full access to \"{editorPath}\" and its subfolders and files");
					return;
				} else if (!GrantAccess()) {
					_doNotContinue = true;
					return;
				}

			// NOTE: not supported on Linux/Android (< C++ 14)
			bool experimental = MfuscatorSettings.experimental && (report.summary.platform == BuildTarget.StandaloneWindows || report.summary.platform == BuildTarget.StandaloneWindows64);
			MfuscatorBridge.Prepare(editorPath, _encKey, experimental);
		}

		// [Unity]
		public void OnPostGenerateGradleAndroidProject(string path) {
			// NOTE: this code is expected to be executed only when building Android builds

			// ignore?
			if (_doNotContinue) return;

			_doNotContinue = true;

			// NOTE: it is expected that "OnPostprocessBuild" will be called after this method

			path = GetMetaFilePathAndroid(path);
			if (!File.Exists(path)) {
				Utils.LogError("Failed");
				return;
			}

			string editorPath = EditorPath;
			MfuscatorBridge.Modify(path, editorPath, _encKey);
			_ = MfuscatorBridge.Restore(editorPath);
		}

		// [Unity]
		public void OnPostprocessBuild(BuildReport report) {
			// ignore?
			if (_doNotContinue || !IsGoodReport(report)) return;

			string editorPath = EditorPath;
			MfuscatorBridge.Modify(GetMetaFilePathStandalone(report), editorPath, _encKey);
			_ = MfuscatorBridge.Restore(editorPath);
		}
	}
}
