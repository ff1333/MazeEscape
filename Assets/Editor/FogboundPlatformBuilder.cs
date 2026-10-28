using System;
using System.IO;
using System.Net;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class FogboundPlatformBuilder
{
    private static readonly string[] Scenes = { "Assets/Scenes/Main.unity" };

    public static void BuildWindowsDevelopment()
    {
        Build(BuildTarget.StandaloneWindows64, "Builds/Windows/Development/MazeEscape.exe",
            BuildOptions.Development | BuildOptions.AllowDebugging);
    }

    public static void BuildWindowsRelease()
    {
        Build(BuildTarget.StandaloneWindows64, "Builds/Windows/v1.4.0/MazeEscape.exe", BuildOptions.None);
    }

    public static void BuildWebGLRelease()
    {
        Build(BuildTarget.WebGL, "Builds/WebGL/v1.4.0", BuildOptions.None);
    }

    public static void BuildAndroidRelease()
    {
        var destination = new Uri("https://dl.google.com");
        var proxy = WebRequest.DefaultWebProxy?.GetProxy(destination);
        if (proxy != null && proxy != destination)
            Environment.SetEnvironmentVariable("JAVA_TOOL_OPTIONS",
                $"-Djava.net.preferIPv4Stack=true -Dhttps.proxyHost={proxy.Host} -Dhttps.proxyPort={proxy.Port} " +
                $"-Dhttp.proxyHost={proxy.Host} -Dhttp.proxyPort={proxy.Port} " +
                "-Dsun.net.client.defaultConnectTimeout=5000 -Dsun.net.client.defaultReadTimeout=5000");
        EditorUserBuildSettings.buildAppBundle = false;
        PlayerSettings.Android.bundleVersionCode = FogboundMaze.ReleaseVersion.AndroidCode;
        PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevel36;
        Build(BuildTarget.Android, "Builds/Android/MazeEscape-v1.4.0.apk", BuildOptions.None);
    }

    private static void Build(BuildTarget target, string location, BuildOptions options)
    {
        PlayerSettings.bundleVersion = FogboundMaze.ReleaseVersion.Current;
        var directory = Path.GetDirectoryName(location);
        if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = Scenes,
            target = target,
            locationPathName = location,
            options = options
        });

        var summary = report.summary;
        Debug.Log($"FOGBOUND_PLATFORM_BUILD target={target} result={summary.result} " +
                  $"errors={summary.totalErrors} warnings={summary.totalWarnings} size={summary.totalSize}");
        if (summary.result != BuildResult.Succeeded)
        {
            throw new InvalidOperationException($"{target} build failed with {summary.totalErrors} errors.");
        }
    }
}
