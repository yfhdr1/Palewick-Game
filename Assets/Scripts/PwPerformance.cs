using UnityEngine;
public static class PwPerformance
{
    private const string GraphicsKey = "pw_graphics";
    private const string ResKey = "pw_res";
    private const string FpsKey = "pw_fps";
    private const string TunedKey = "pw_perf_tuned";
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Boot()
    {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = Mathf.Max(60, PlayerPrefs.GetInt(FpsKey, 60));
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
        AutoTune();
        Application.lowMemory += OnLowMemory;
    }
    private static void AutoTune()
    {
        if (PlayerPrefs.GetInt(TunedKey, 0) == 1) return;
        PlayerPrefs.SetInt(TunedKey, 1);
        bool weak = SystemInfo.systemMemorySize > 0 && SystemInfo.systemMemorySize < 3200;
        bool weakGpu = SystemInfo.graphicsMemorySize > 0 && SystemInfo.graphicsMemorySize < 1024;
        bool weakCpu = SystemInfo.processorCount <= 4;
        int score = (weak ? 1 : 0) + (weakGpu ? 1 : 0) + (weakCpu ? 1 : 0);
        if (score >= 2)
        {
            if (!PlayerPrefs.HasKey(GraphicsKey)) PlayerPrefs.SetString(GraphicsKey, "low");
            if (!PlayerPrefs.HasKey(ResKey)) PlayerPrefs.SetInt(ResKey, 720);
        }
        else if (score == 1)
        {
            if (!PlayerPrefs.HasKey(ResKey)) PlayerPrefs.SetInt(ResKey, 1080);
        }
        PlayerPrefs.Save();
    }
    private static void OnLowMemory()
    {
        Resources.UnloadUnusedAssets();
        System.GC.Collect();
    }
}
