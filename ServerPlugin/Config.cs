using System.Xml.Serialization;
using PluginSdk.Config;

namespace MesCluster;

[Tab("pattern", caption: "Spawn pattern")]
[Tab("density", caption: "Density")]
[Tab("guard", caption: "MES version guard")]
[XmlRoot("Settings")]
public class Config : PluginConfig
{
    [BoolOption("Gate MES spawn placements (off: MES spawns as it would without this plugin)", Parent = "pattern")]
    public bool Enabled
    {
        get;
        set => SetField(ref field, value);
    } = true;

    [DoubleOption(1000, 1e7, "Voronoi cell size in metres: the spacing of the cell sites", Parent = "pattern")]
    public double CellSize
    {
        get;
        set => SetField(ref field, value);
    } = 40000;

    [DoubleOption(0, 1e7, "Width of the empty band between cell cores in metres; 0 = GapSyncDistances x the world's sync distance", Parent = "pattern")]
    public double Gap
    {
        get;
        set => SetField(ref field, value);
    } = 0;

    [DoubleOption(0, 100, "Gap in sync distances, used when Gap is 0. 4 = the cluster's automatic partition split distance", Parent = "pattern")]
    public double GapSyncDistances
    {
        get;
        set => SetField(ref field, value);
    } = 4;

    [DoubleOption(0, 1, "How far cell sites move off the regular lattice, as a fraction of the cell size", Parent = "pattern")]
    public double Jitter
    {
        get;
        set => SetField(ref field, value);
    } = 0.5;

    [LongOption(description: "Seed of the cell pattern; every node of a cluster must use the same one", Parent = "pattern")]
    public long Seed
    {
        get;
        set => SetField(ref field, value);
    } = 1321;

    [IntOption(1, 100, "How many placements MES may compute for one spawn until one fits a cell core", Parent = "pattern")]
    public int PlacementAttempts
    {
        get;
        set => SetField(ref field, value);
    } = 16;

    [StringOption(description: "MES spawning types left alone: SpawningType names, comma separated", Parent = "pattern")]
    public string ExemptSpawnTypes
    {
        get;
        set => SetField(ref field, value);
    } = "Creature";

    [DoubleOption(0, 1e7, "Radius of the density check around a spawn point in metres", Parent = "density")]
    public double DensityRadius
    {
        get;
        set => SetField(ref field, value);
    } = 15000;

    [IntOption(0, 100000, "No spawn where this many NPC grids are already within DensityRadius (0 = no density limit)", Parent = "density")]
    public int MaxNpcGridsInRadius
    {
        get;
        set => SetField(ref field, value);
    } = 12;

    [IntOption(0, 1000000, "No spawn once the cluster has this many NPC grids, offline partitions included (0 = no limit)", Parent = "density")]
    public int MaxClusterNpcGrids
    {
        get;
        set => SetField(ref field, value);
    } = 300;

    [StringOption(description: "MES versions (MES_SessionCore.ModVersion) the patches were checked against, comma separated", Parent = "guard")]
    public string AcceptedMesVersions
    {
        get;
        set => SetField(ref field, value);
    } = "2.75.02";

    [StringOption(description: "Accepted instruction hashes of the MES methods the plugin depends on, comma separated (each startup logs the current one)", Parent = "guard")]
    public string AcceptedMethodHashes
    {
        get;
        set => SetField(ref field, value);
    } = "";
}
