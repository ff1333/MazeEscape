using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class PortfolioVideoBuild
{
    public static void BuildWindows()
    {
        const string output = "Builds/PortfolioCapture/MazeEscapeCapture.exe";
        Directory.CreateDirectory(Path.GetDirectoryName(output));
        PlayerSettings.bundleVersion = FogboundMaze.ReleaseVersion.Current;
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = new[] { "Assets/Scenes/Main.unity" },
            locationPathName = output,
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.Development
        });
        if (report.summary.result != BuildResult.Succeeded)
            throw new InvalidOperationException("Portfolio capture build failed.");
        if (Application.isBatchMode) EditorApplication.Exit(0);
    }
}
