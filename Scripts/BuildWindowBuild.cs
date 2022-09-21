using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.UIElements;
#if USE_ADDRESSABLE
using UnityEditor.AddressableAssets.Settings;
#endif

namespace BuildManager.Scripts
{
	public partial class BuildWindow
	{
		private event Action EndBuild;
		
		private void Build()
		{
			var toBuild = new List<(BuildPreset, BuildTarget)>();

			foreach (BuildPreset preset in _presets)
			{
				if (!preset.isActive)
				{
					continue;
				}

				for (BuildTarget t = (BuildTarget)1;; t = (BuildTarget)((int)t << 1))
				{
					if (!Enum.IsDefined(typeof(BuildTarget), t))
					{
						break;
					}

					if (!preset.buildTargets.HasFlag(t))
					{
						continue;
					}

					if (VerifyBuildModule(t))
					{
						toBuild.Add((preset, t));
					}
				}
			}

			if (toBuild.Count == 0)
			{
				return;
			}

			
			
			BuildAsync(toBuild.ToArray())
				.ContinueWith(r =>
				{
					if(r.Result == BuildResult.Succeeded)
					{
						EndBuild += IncrementVersion;
					}
				})
				.ContinueWith(_ => EndBuild += UpdateGenericData);
			
		}
		
		private void Update()
		{
			if (EndBuild == null)
			{
				return;
			}
			
			EndBuild.Invoke();
			EndBuild = null;
		}

		private void BuildSpecific()
		{
			if (_buildPresets.selectedIndex == -1)
			{
				return;
			}

			BuildTarget target = (BuildTarget)_specificBuildTarget.value;

			BuildPreset preset = (BuildPreset)_buildPresets.selectedItem;

			if (VerifyBuildModule(target))
			{
				BuildAsync((preset, target))
					.ContinueWith(_ => EndBuild += UpdateGenericData);
			}
		}

		private static void IncrementVersion()
		{
			if (!BuildManagerSettings.instance.incrementBuildVersionByBuild)
			{
				return;
			}

			string version = PlayerSettings.bundleVersion;
			string[] split = version.Split('.');

			if (split.Length != BuildManagerSettings.instance.incrementVersion + 1)
			{
				Debug.LogError("Invalid version format");
				return;
			}
			
			split[BuildManagerSettings.instance.incrementVersion] = (int.Parse(split[BuildManagerSettings.instance.incrementVersion]) + 1).ToString();
			
			PlayerSettings.bundleVersion = string.Join('.',split);
		}

		private async Task<BuildResult> BuildAsync(params (BuildPreset, BuildTarget)[] toBuild)
		{
			_progressBar.value = 0;
			
			#if USE_ADDRESSABLE
			if (BuildManagerSettings.instance.buildAddressable)
			{
				_progress.text = "Building Addressable...";
				AddressableAssetSettings.CleanPlayerContent();
				AddressableAssetSettings.BuildPlayerContent();
			}
			#endif

			_progress.text = "Start build...";
			
			int failed = 0;

			int i = 0;
			foreach ((BuildPreset preset, BuildTarget target) in toBuild)
			{
				i++;
				_progress.text = $"Building {i}/{toBuild.Length}... ({preset.presetName} - {target})";
				_progressBar.value = (float)i / toBuild.Length;

				BuildReport report = MakeBuild(preset, target);

				if (report.summary.result == BuildResult.Cancelled)
				{
					_progress.text = "Build cancelled";
					return BuildResult.Cancelled;
				}

				if (report.summary.result == BuildResult.Failed)
				{
					_progress.text = "Build failed";

					if (BuildManagerSettings.instance.stopBuildOnErrors)
					{
						return BuildResult.Failed;
					}

					failed++;
				}

				if (report.summary.result == BuildResult.Succeeded)
				{
					if (BuildManagerSettings.instance.zipBuilds)
					{
						await Task.Delay(100);
						
						_progress.text = $"Zipping {i}/{toBuild.Length}... ({preset.presetName} - {target})";
						_progressBar.value = (float)i / toBuild.Length;

						await ZipBuild(preset, target);
					}
				}

				await Task.Delay(100);
			}

			_progressBar.value = 1;

			if (failed == 0)
			{
				_progress.text = "Done! :)";
			}
			else
			{
				_progress.text = $"Build succeeded with {failed} failed";
			}

			return BuildResult.Succeeded;
		}

		private static Task ZipBuild(BuildPreset preset, BuildTarget target)
		{
			string outputPath = CompilePath(preset, target);
			string zipPath = CompilePath(preset, target, true);
			
			outputPath = Application.dataPath.Replace("Assets", outputPath);
			outputPath = outputPath[..outputPath.LastIndexOf('/')];
			zipPath = Application.dataPath.Replace("Assets", zipPath);

			if (File.Exists(zipPath))
			{
				File.Delete(zipPath);
			}
			// Zip file & folder but not include _DoNotShip folder and zip in folder if BuildManagerSettings.instance.zipContainsFolder is true
			using(ZipArchive archive = ZipFile.Open(zipPath, ZipArchiveMode.Create))
			{
				foreach (string file in Directory.GetFiles(outputPath,"*",SearchOption.AllDirectories))
				{
					if (file.Contains("_DoNotShip"))
					{
						continue;
					}
					
					string relativePath = file.Replace(outputPath, "");
					if (BuildManagerSettings.instance.zipContainsFolder)
					{
						relativePath = relativePath[1..];
					}
					archive.CreateEntryFromFile(file, relativePath);
				}
			}

			return Task.CompletedTask;

			//await Task.Run(() => ZipFile.CreateFromDirectory(outputPath, zipPath));
		}

		private static BuildReport MakeBuild(BuildPreset preset, BuildTarget target)
		{
			BuildPlayerOptions options = new BuildPlayerOptions
			{
				scenes = preset.Scenes,
				locationPathName = CompilePath(preset, target),
				target = GetBuildTarget(target),
				options = preset.options,
				extraScriptingDefines = preset.extraScriptingDefines
			};

			return BuildPipeline.BuildPlayer(options);
		}

		private static string CompilePath(BuildPreset preset, BuildTarget target, bool zip = false)
		{
			string path;
			if (zip)
			{
				path = BuildManagerSettings.instance.zipPath;
			}
			else
			{
				path = BuildManagerSettings.instance.buildPath;
			}

			path = path.Replace("{platform}", target.ToString());
			path = path.Replace("{preset}", preset.presetName);
			path = path.Replace("{version}", PlayerSettings.bundleVersion);
			path = path.Replace("{product}", PlayerSettings.productName);
			path = path.Replace("{date}", DateTime.Now.ToString("yyyy-MM-dd"));
			path = path.Replace("{date-y}", DateTime.Now.ToString("yyyy"));
			path = path.Replace("{date-m}", DateTime.Now.ToString("MM"));
			path = path.Replace("{date-d}", DateTime.Now.ToString("dd"));
			path = path.Replace("{time}", DateTime.Now.ToString("HH-mm-ss"));
			path = path.Replace("{time-h}", DateTime.Now.ToString("HH"));
			path = path.Replace("{time-m}", DateTime.Now.ToString("mm"));
			path = path.Replace("{time-s}", DateTime.Now.ToString("ss"));

			if (zip)
			{
				if (!path.EndsWith(".zip"))
				{
					path += ".zip";
				}
			}
			else
			{
				path = AddExtension(target);
			}

			return path;

			string AddExtension(BuildTarget t)
			{
				UnityEditor.BuildTarget buildTarget = GetBuildTarget(t);
				switch (buildTarget)
				{
					case UnityEditor.BuildTarget.StandaloneWindows:
					case UnityEditor.BuildTarget.StandaloneWindows64:
						return path + ".exe";
					case UnityEditor.BuildTarget.StandaloneOSX:
						return path + ".app";
					case UnityEditor.BuildTarget.StandaloneLinux64:
						return path + ".x86_64";
					default:
						return path;
				}
			}
		}

		private BuildTarget VerifyBuildModules(BuildTarget globalBuildTargets)
		{
			BuildTarget notSupported = 0;

			for (BuildTarget t = (BuildTarget)1;; t = (BuildTarget)((int)t << 1))
			{
				if (!Enum.IsDefined(typeof(BuildTarget), t))
				{
					break;
				}

				if (!globalBuildTargets.HasFlag(t))
				{
					continue;
				}

				if (!VerifyBuildModule(t))
				{
					notSupported |= t;
				}
			}

			if (notSupported != 0)
			{
				_helpBox.style.display = DisplayStyle.Flex;
				_helpBox.text = $"The following build targets are not supported: {notSupported}";
			}
			else
			{
				_helpBox.style.display = DisplayStyle.None;
			}


			return globalBuildTargets & ~notSupported;
		}

		private static bool VerifyBuildModule(BuildTarget buildTarget)
		{
			return buildTarget switch
			{
				BuildTarget.Windows => BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Standalone,
					UnityEditor.BuildTarget.StandaloneWindows64),
				BuildTarget.Mac => BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Standalone,
					UnityEditor.BuildTarget.StandaloneOSX),
				BuildTarget.Linux => BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Standalone,
					UnityEditor.BuildTarget.StandaloneLinux64),
				BuildTarget.Android => BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android,
					UnityEditor.BuildTarget.Android),
				BuildTarget.IOS => BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.iOS,
					UnityEditor.BuildTarget.iOS),
				BuildTarget.WebGL => BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.WebGL,
					UnityEditor.BuildTarget.WebGL),
				BuildTarget.UWP => BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.WSA,
					UnityEditor.BuildTarget.WSAPlayer),
				BuildTarget.AppleTV => BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.tvOS,
					UnityEditor.BuildTarget.tvOS),
				_ => false
			};
		}

		private static UnityEditor.BuildTarget GetBuildTarget(BuildTarget buildTarget)
		{
			return buildTarget switch
			{
				BuildTarget.Windows => UnityEditor.BuildTarget.StandaloneWindows64,
				BuildTarget.Mac => UnityEditor.BuildTarget.StandaloneOSX,
				BuildTarget.Linux => UnityEditor.BuildTarget.StandaloneLinux64,
				BuildTarget.Android => UnityEditor.BuildTarget.Android,
				BuildTarget.IOS => UnityEditor.BuildTarget.iOS,
				BuildTarget.WebGL => UnityEditor.BuildTarget.WebGL,
				BuildTarget.UWP => UnityEditor.BuildTarget.WSAPlayer,
				BuildTarget.AppleTV => UnityEditor.BuildTarget.tvOS,
				_ => throw new ArgumentOutOfRangeException(nameof(buildTarget), buildTarget, null)
			};
		}
	}
}