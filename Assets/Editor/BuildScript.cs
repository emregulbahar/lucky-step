using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// Command-line build entry points. Called by <c>commands.build</c> in project.yaml
/// through <c>-executeMethod BuildScript.BuildWebGL</c>.
/// </summary>
public static class BuildScript
{
    private const string WebGLOutputPath = "Builds/WebGL";

    /// <summary>
    /// Builds all enabled scenes in the Build Settings for WebGL into <c>Builds/WebGL</c> with the release settings
    /// (<see cref="ApplyWebGLReleaseSettings"/>).
    /// Exits the editor with code 0 on success and 1 on failure or when no scene is enabled.
    /// </summary>
    public static void BuildWebGL()
    {
        ApplyWebGLReleaseSettings();

        string[] scenes = EditorBuildSettings.scenes
            .Where(scene => scene.enabled)
            .Select(scene => scene.path)
            .ToArray();

        if (scenes.Length == 0)
        {
            Debug.LogError("BuildScript: no enabled scenes in Build Settings.");
            EditorApplication.Exit(1);
            return;
        }

        var options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = WebGLOutputPath,
            target = BuildTarget.WebGL,
            options = BuildOptions.None,
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        bool succeeded = report.summary.result == BuildResult.Succeeded;
        Debug.Log($"BuildScript: WebGL build {report.summary.result}, {report.summary.totalSize} bytes -> {WebGLOutputPath}");
        EditorApplication.Exit(succeeded ? 0 : 1);
    }

    /// <summary>
    /// Story 007 release settings: product name shown in the page title, the full-window Lucky Step template,
    /// gzip with decompression fallback (opens on itch.io without server headers), and code optimised for size.
    /// </summary>
    private static void ApplyWebGLReleaseSettings()
    {
        PlayerSettings.productName = "Lucky Step";
        PlayerSettings.WebGL.template = "PROJECT:LuckyStep";
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
        PlayerSettings.WebGL.decompressionFallback = true;
        PlayerSettings.WebGL.showDiagnostics = false;
        UnityEditor.WebGL.UserBuildSettings.codeOptimization = UnityEditor.WebGL.WasmCodeOptimization.DiskSize;
    }
}
