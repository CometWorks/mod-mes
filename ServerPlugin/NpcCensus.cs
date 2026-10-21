using System;
using Sandbox.Engine.Physics;
using Sandbox.Game.Entities;
using Sandbox.Game.World;
using VRageMath;

namespace MesCluster;

/// <summary>
/// How many NPC grids are around a point. On a cluster the cluster plugin publishes a census that also
/// counts the NPC grids of offline partitions (#1354); this plugin only reads it. Without it (a plain
/// server, or a cluster build that predates the census) the count covers this process only.
///
/// The census is a BCL delegate in an AppDomain data slot, so neither plugin references the other:
/// <c>Func&lt;double x, double y, double z, double radius, int count&gt;</c>, -1 when the cluster does not
/// know (no snapshot yet, or a stale one). Called on the game thread; the cluster answers from a local
/// snapshot.
/// </summary>
internal static class NpcCensus
{
    internal const string CensusSlot = "CometWorks.Cluster.NpcCensus.v1";

    public static bool ClusterWide => AppDomain.CurrentDomain.GetData(CensusSlot) is Func<double, double, double, double, int>;

    /// <summary>NPC grids within <paramref name="radius"/> metres of the point; -1 = unknown.</summary>
    public static int Count(Vector3D point, double radius)
    {
        if (AppDomain.CurrentDomain.GetData(CensusSlot) is Func<double, double, double, double, int> census)
            return census(point.X, point.Y, point.Z, radius);

        return CountLocal(point, radius);
    }

    private static int CountLocal(Vector3D point, double radius)
    {
        double radiusSquared = radius * radius;
        int count = 0;
        foreach (var entity in MyEntities.GetEntities())
        {
            if (entity is not MyCubeGrid grid || grid.Closed || grid.IsPreview)
                continue;
            if (MyGridPhysicalHierarchy.Static.GetRoot(grid) != grid || !IsNpc(grid))
                continue;
            if (Vector3D.DistanceSquared(grid.PositionComp.WorldAABB.Center, point) <= radiusSquared)
                count++;
        }

        return count;
    }

    /// <summary>The same definition as the cluster's census: owned, and every big owner is an NPC identity.</summary>
    public static bool IsNpc(MyCubeGrid grid)
    {
        var owners = grid.BigOwners;
        if (owners.Count == 0)
            return false;

        foreach (var owner in owners)
        {
            if (!MySession.Static.Players.IdentityIsNpc(owner))
                return false;
        }

        return true;
    }
}
