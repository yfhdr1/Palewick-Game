using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Palewick.EditorTools
{
    /// <summary>
    /// Pulls every committed project change (code, scenes, images, audio and settings)
    /// from the current GitHub branch, then runs the project-wide repair after Unity
    /// finishes importing/recompiling the downloaded files.
    /// </summary>
    public static class PalewickProjectUpdater
    {
        private const string PendingFixKey = "Palewick.ProjectUpdater.PendingMasterFix";
        private const int GitTimeoutMilliseconds = 120000;
        private static bool pendingCheckScheduled;
        private static string resolvedGitExecutable;

        [InitializeOnLoadMethod]
        private static void ResumeAfterScriptReload()
        {
            SchedulePendingFixCheck();
        }

        [MenuItem("Palewick/Update Everything From GitHub (One Click)", false, -10)]
        public static void UpdateEverything()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorUtility.DisplayDialog(
                    "Palewick Updater",
                    "Exit Play Mode before updating the project.",
                    "OK");
                return;
            }

            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorUtility.DisplayDialog(
                    "Palewick Updater",
                    "Wait for Unity to finish compiling/importing, then press the update button again.",
                    "OK");
                return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            if (!Directory.Exists(Path.Combine(projectRoot, ".git")))
            {
                EditorUtility.DisplayDialog(
                    "Palewick Updater",
                    "This project is not a Git checkout. Clone the GitHub repository once, then this button can update every future code/image/asset change.",
                    "OK");
                return;
            }

            string gitExecutable = ResolveGitExecutable(projectRoot);
            if (gitExecutable == null)
            {
                EditorUtility.DisplayDialog(
                    "Palewick Updater",
                    "Git was not found on this PC. Install Git for Windows or GitHub Desktop, restart Unity, then press the update button again.",
                    "OK");
                Debug.LogError(
                    "[Palewick Updater] Git was not found. Checked the PATH and the usual " +
                    "Git for Windows / GitHub Desktop install folders.");
                return;
            }
            Debug.Log("[Palewick Updater] Using Git executable: " + gitExecutable);

            GitResult status = RunGit(gitExecutable, projectRoot, "status --porcelain --untracked-files=no");
            if (!status.Succeeded)
            {
                ShowGitFailure("Could not inspect the project before updating.", status);
                return;
            }
            if (!string.IsNullOrWhiteSpace(status.output))
            {
                EditorUtility.DisplayDialog(
                    "Palewick Updater",
                    "The project has local tracked changes. Commit, revert, or stash them first so the updater never overwrites your work.\n\n" + status.output,
                    "OK");
                return;
            }

            GitResult branchResult = RunGit(gitExecutable, projectRoot, "rev-parse --abbrev-ref HEAD");
            string branch = branchResult.output.Trim();
            if (!branchResult.Succeeded || string.IsNullOrEmpty(branch) || branch == "HEAD")
            {
                ShowGitFailure("The current Git branch could not be determined.", branchResult);
                return;
            }

            GitResult beforeResult = RunGit(gitExecutable, projectRoot, "rev-parse --short HEAD");
            string before = beforeResult.Succeeded ? beforeResult.output.Trim() : "unknown";

            try
            {
                EditorUtility.DisplayProgressBar(
                    "Palewick Updater",
                    "Downloading all updates from origin/" + branch + "...",
                    0.35f);

                GitResult fetch = RunGit(gitExecutable, projectRoot, "fetch --prune origin");
                if (!fetch.Succeeded)
                {
                    ShowGitFailure("Could not download updates from GitHub.", fetch);
                    return;
                }

                EditorUtility.DisplayProgressBar(
                    "Palewick Updater",
                    "Applying code, images, scenes and project files...",
                    0.7f);

                GitResult pull = RunGit(gitExecutable, projectRoot, "pull --ff-only origin " + QuoteArgument(branch));
                if (!pull.Succeeded)
                {
                    ShowGitFailure(
                        "The update could not be applied safely. The updater only allows fast-forward updates and never overwrites local work.",
                        pull);
                    return;
                }

                GitResult afterResult = RunGit(gitExecutable, projectRoot, "rev-parse --short HEAD");
                string after = afterResult.Succeeded ? afterResult.output.Trim() : "unknown";

                SessionState.SetBool(PendingFixKey, true);
                Debug.Log(
                    "[Palewick Updater] GitHub update completed on " + branch +
                    " (" + before + " -> " + after + ").\n" + pull.CombinedOutput);

                EditorUtility.DisplayProgressBar(
                    "Palewick Updater",
                    "Importing updates into Unity...",
                    0.9f);
                AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
                SchedulePendingFixCheck();
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        private static void SchedulePendingFixCheck()
        {
            if (pendingCheckScheduled)
            {
                return;
            }
            pendingCheckScheduled = true;
            EditorApplication.delayCall += RunPendingFixWhenReady;
        }

        private static void RunPendingFixWhenReady()
        {
            pendingCheckScheduled = false;
            if (!SessionState.GetBool(PendingFixKey, false))
            {
                return;
            }

            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                SchedulePendingFixCheck();
                return;
            }

            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                SchedulePendingFixCheck();
                return;
            }

            SessionState.SetBool(PendingFixKey, false);
            Debug.Log("[Palewick Updater] Unity import finished; running the master fix automatically.");
            PalewickMasterFix.FixEverything();
        }

        private static string ResolveGitExecutable(string workingDirectory)
        {
            if (!string.IsNullOrEmpty(resolvedGitExecutable))
            {
                return resolvedGitExecutable;
            }

            List<string> candidates = new List<string> { "git" };
            if (Application.platform == RuntimePlatform.WindowsEditor)
            {
                string programFiles = Environment.GetEnvironmentVariable("ProgramFiles");
                string programFilesX86 = Environment.GetEnvironmentVariable("ProgramFiles(x86)");
                string localAppData = Environment.GetEnvironmentVariable("LOCALAPPDATA");
                if (!string.IsNullOrEmpty(programFiles))
                {
                    candidates.Add(Path.Combine(programFiles, @"Git\cmd\git.exe"));
                }
                if (!string.IsNullOrEmpty(programFilesX86))
                {
                    candidates.Add(Path.Combine(programFilesX86, @"Git\cmd\git.exe"));
                }
                if (!string.IsNullOrEmpty(localAppData))
                {
                    candidates.Add(Path.Combine(localAppData, @"Programs\Git\cmd\git.exe"));

                    string desktopRoot = Path.Combine(localAppData, "GitHubDesktop");
                    if (Directory.Exists(desktopRoot))
                    {
                        // Newest app folder first so an updated GitHub Desktop wins.
                        List<string> appFolders = new List<string>(Directory.GetDirectories(desktopRoot, "app-*"));
                        appFolders.Sort(StringComparer.OrdinalIgnoreCase);
                        for (int i = appFolders.Count - 1; i >= 0; i--)
                        {
                            string bundledGitRoot = Path.Combine(appFolders[i], @"resources\app\git");
                            candidates.Add(Path.Combine(bundledGitRoot, @"cmd\git.exe"));
                            candidates.Add(Path.Combine(bundledGitRoot, @"mingw64\bin\git.exe"));
                        }
                    }
                }
            }

            foreach (string candidate in candidates)
            {
                if (candidate != "git" && !File.Exists(candidate))
                {
                    continue;
                }
                GitResult probe = RunGit(candidate, workingDirectory, "--version");
                if (probe.Succeeded)
                {
                    resolvedGitExecutable = candidate;
                    return resolvedGitExecutable;
                }
            }

            return null;
        }

        private static GitResult RunGit(string gitExecutable, string workingDirectory, string arguments)
        {
            GitResult result = new GitResult { exitCode = -1 };
            try
            {
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = gitExecutable,
                    Arguments = arguments,
                    WorkingDirectory = workingDirectory,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    StandardOutputEncoding = Encoding.UTF8,
                    StandardErrorEncoding = Encoding.UTF8
                };
                startInfo.EnvironmentVariables["GIT_TERMINAL_PROMPT"] = "0";
                startInfo.EnvironmentVariables["GCM_INTERACTIVE"] = "never";

                using (Process process = new Process { StartInfo = startInfo })
                {
                    StringBuilder stdout = new StringBuilder();
                    StringBuilder stderr = new StringBuilder();
                    process.OutputDataReceived += (_, eventArgs) =>
                    {
                        if (eventArgs.Data != null)
                        {
                            stdout.AppendLine(eventArgs.Data);
                        }
                    };
                    process.ErrorDataReceived += (_, eventArgs) =>
                    {
                        if (eventArgs.Data != null)
                        {
                            stderr.AppendLine(eventArgs.Data);
                        }
                    };

                    if (!process.Start())
                    {
                        result.error = "Git process did not start.";
                        return result;
                    }
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();

                    if (!process.WaitForExit(GitTimeoutMilliseconds))
                    {
                        try
                        {
                            process.Kill();
                        }
                        catch
                        {
                            // Nothing else to do; a timeout is reported below.
                        }
                        result.output = stdout.ToString();
                        result.error = "Git timed out after two minutes.\n" + stderr;
                        return result;
                    }

                    // Flush the asynchronous output callbacks before reading the buffers.
                    process.WaitForExit();
                    result.exitCode = process.ExitCode;
                    result.output = stdout.ToString();
                    result.error = stderr.ToString();
                }
            }
            catch (Exception exception)
            {
                result.exitCode = -1;
                result.error = exception.Message;
            }
            return result;
        }

        private static string QuoteArgument(string value)
        {
            return "\"" + value.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
        }

        private static void ShowGitFailure(string message, GitResult result)
        {
            string details = result.CombinedOutput;
            Debug.LogError("[Palewick Updater] " + message + "\n" + details);
            EditorUtility.DisplayDialog(
                "Palewick Updater",
                message + (string.IsNullOrWhiteSpace(details) ? string.Empty : "\n\n" + details),
                "OK");
        }

        private struct GitResult
        {
            public int exitCode;
            public string output;
            public string error;

            public bool Succeeded
            {
                get { return exitCode == 0; }
            }

            public string CombinedOutput
            {
                get
                {
                    string stdout = output == null ? string.Empty : output.Trim();
                    string stderr = error == null ? string.Empty : error.Trim();
                    if (string.IsNullOrEmpty(stdout))
                    {
                        return stderr;
                    }
                    if (string.IsNullOrEmpty(stderr))
                    {
                        return stdout;
                    }
                    return stdout + "\n" + stderr;
                }
            }
        }
    }
}
