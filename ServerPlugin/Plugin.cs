using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using HarmonyLib;
using PluginSdk.Config;
using PluginSdk.Logging;
using Sandbox.Game.World;
using VRage.FileSystem;
using VRage.Plugins;
#if !LOCAL_BUILD
using System.Reflection;

[assembly: AssemblyVersion("1.0.0.0")]
[assembly: AssemblyFileVersion("1.0.0.0")]
#endif

namespace MesCluster;

// ReSharper disable once UnusedType.Global
public class Plugin : IPlugin
{
    public const string Name = "MesCluster";

    /// <summary>The cluster's sweep reads it to remove NPC grids in the gap bands faster (#1354).</summary>
    internal const string InGapSlot = "CometWorks.ModMes.InGap.v1";

    private static readonly Logger Log = Logger.Create(Name);
    private static readonly TimeSpan StatsInterval = TimeSpan.FromMinutes(10);

    public static Plugin Instance { get; private set; }

    // Discovered by the Quasar agent
    public Config PluginConfig { get; private set; }

    // Read once at start, so that the nodes of a cluster cannot drift apart: see MesSpawnGate.Configure
    private static readonly HashSet<string> PatternSettings = new()
    {
        nameof(Config.CellSize), nameof(Config.Gap), nameof(Config.GapSyncDistances), nameof(Config.Jitter), nameof(Config.Seed),
    };

    private Harmony harmony;
    private string configPath;
    private bool installed;
    private DateTime nextStats;

    public void Init(object gameInstance)
    {
        Instance = this;
        configPath = Path.Combine(MyFileSystem.UserDataPath, Name + ".cfg");
        PluginConfig = LoadConfig(configPath);
        PluginConfig.PropertyChanged += OnConfigChanged;
        harmony = new Harmony(Name);

        AppDomain.CurrentDomain.SetData(InGapSlot, (Func<double, double, double, bool>)InGap);
        Log.Info("Loaded");
    }

    public void Update()
    {
        if (!installed && MySession.Static is { Ready: true })
        {
            installed = true;
            try
            {
                MesSpawnGate.Install(harmony, PluginConfig);
            }
            catch (Exception e)
            {
                Log.Critical("Patching MES failed; MES runs UNGATED", e);
            }
            nextStats = DateTime.UtcNow + StatsInterval;
        }

        if (MesSpawnGate.Active && DateTime.UtcNow >= nextStats)
        {
            nextStats = DateTime.UtcNow + StatsInterval;
            Log.Info($"spawns since start: placed {MesSpawnGate.Accepted}, moved into a core {MesSpawnGate.Moved}, " +
                     $"refused: pattern {MesSpawnGate.RefusedPattern}, density {MesSpawnGate.RefusedDensity}, " +
                     $"cluster cap {MesSpawnGate.RefusedCluster}, census unknown {MesSpawnGate.RefusedUnknown}; " +
                     $"census {(NpcCensus.ClusterWide ? "cluster-wide" : "this process only")}");
        }
    }

    public void Dispose()
    {
        AppDomain.CurrentDomain.SetData(InGapSlot, null);
        if (PluginConfig != null)
            PluginConfig.PropertyChanged -= OnConfigChanged;
        // Harmony patches stay: unpatching on unload can break other plugins' patches of the same methods
        Instance = null;
    }

    private static bool InGap(double x, double y, double z) =>
        MesSpawnGate.Active && MesSpawnGate.Pattern is { } pattern && pattern.InGap(x, y, z);

    private void OnConfigChanged(object sender, PropertyChangedEventArgs e)
    {
        try
        {
            ConfigStorage.SaveXml(PluginConfig, configPath);
            if (PatternSettings.Contains(e.PropertyName))
                Log.Warning($"{e.PropertyName} changed: the spawn pattern changes at the next start (all nodes must use the same one)");
            else if (MesSpawnGate.Active)
                MesSpawnGate.ConfigureExempt(PluginConfig);
        }
        catch (Exception ex)
        {
            Log.Error("Applying the changed config failed", ex);
        }
    }

    private static Config LoadConfig(string path)
    {
        try
        {
            var config = ConfigStorage.LoadXml<Config>(path);
            ConfigStorage.SaveXml(config, path);
            return config;
        }
        catch (Exception e)
        {
            Log.Error($"Cannot read {path}, using the defaults", e);
            return new Config();
        }
    }
}
