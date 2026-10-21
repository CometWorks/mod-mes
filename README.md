# MES Cluster

A Magnetar server plugin that makes [Modular Encounters Systems](https://steamcommunity.com/sharedfiles/filedetails/?id=1521905890)
(MES, Workshop 1521905890) behave on a Magnetar cluster. It patches the MES mod at runtime and
contains no MES code: MES does not allow republishing modified copies of the mod. The world keeps
loading MES from the Workshop as before.

## Why

MES spreads NPC grids evenly along the players' flight paths. On a cluster those grids chain into
huge partitions that can't be split, balanced or taken offline separately. Each node also runs its
own MES, which only sees that node's players and grids, so its caps grow with the node count.

## What it does

- **Spawn pattern.** Space is cut into Voronoi cells around jittered lattice sites. Only the core of
  a cell is spawnable: the cell shrunk by half the gap from every face. Cores are at least the gap
  apart, so the NPCs of one core form partitions of their own. The gap defaults to 4 x the world's
  sync distance, the cluster's automatic partition split distance (12 km at a 3 km sync distance).
  Cores are convex, so a cargo ship whose path starts and ends in one core stays in it.
- **Density limits.** No spawn where `MaxNpcGridsInRadius` NPC grids are already within
  `DensityRadius`, and none once the cluster has `MaxClusterNpcGrids`. On a cluster that publishes
  its NPC census, the counts are cluster-wide and include the grids of offline partitions, so a
  returning player meets the old pocket instead of a new one. Otherwise they cover this server.
- **How.** A Harmony postfix on MES's `PathPlacements.GetSpawnPlacement` checks the placement MES
  computed. If it doesn't fit, MES computes up to `PlacementAttempts` more placements, with all of
  its own obstruction checks, and the first that fits is used. If none fits, MES skips the spawn.
  Static encounters are left alone, and so are the spawning types in `ExemptSpawnTypes` (creatures).
- **Guard.** Before patching, the plugin checks the MES version (`AcceptedMesVersions`) and a hash of
  the instructions of the MES code it relies on (`AcceptedMethodHashes`). Every start logs both. On
  a mismatch it logs a Critical line and MES runs unpatched. After reviewing an MES update, add the
  new version and hash to the config.

Each start logs one line per gated spawn (`spawn placed` / `spawn refused`) and a summary every
10 minutes.

## Cluster interface

Both are BCL delegates in AppDomain data slots, so neither plugin references the other:

| Slot | Type | Direction |
|---|---|---|
| `CometWorks.Cluster.NpcCensus.v1` | `Func<double x, double y, double z, double radius, int>` | cluster to MES Cluster: NPC grids within `radius` m, offline partitions included; `+Infinity` = all; -1 = unknown (the spawn is refused) |
| `CometWorks.ModMes.InGap.v1` | `Func<double x, double y, double z, bool>` | MES Cluster to the cluster's NPC sweep: the point is in a gap band (false while the patch is inactive) |

An NPC grid is a top-level grid whose big owners are all NPC identities.

## Configuration

`MesCluster.cfg` in the server's data folder, or the plugin's page in Quasar. Every node of a
cluster must use the same pattern settings: `CellSize`, `Gap`, `GapSyncDistances`, `Jitter` and `Seed`
are read once at start (a change applies at the next start), the cluster's gap-band slot follows them,
and each node logs the effective values with a pattern fingerprint, which must be equal on all nodes:

    MES spawn placements gated: cell 40000 m, gap 12000 m (4 x sync distance 3000 m), jitter 0.5, seed 1321, pattern fingerprint ...

The other options apply at once.

| Option | Default | |
|---|---|---|
| `Enabled` | true | |
| `CellSize` | 40000 | metres |
| `Gap` | 0 | metres; 0 = `GapSyncDistances` x sync distance |
| `GapSyncDistances` | 4 | |
| `Jitter` | 0.5 | how far sites move off the lattice, fraction of a cell |
| `Seed` | 1321 | any integer; another seed gives other cells |
| `PlacementAttempts` | 16 | |
| `ExemptSpawnTypes` | Creature | MES `SpawningType` names, comma separated |
| `DensityRadius` | 15000 | metres |
| `MaxNpcGridsInRadius` | 12 | 0 = off |
| `MaxClusterNpcGrids` | 300 | 0 = off |
| `AcceptedMesVersions` | 2.75.02 | |
| `AcceptedMethodHashes` | b6de8061483bbd9a | the hash each start logs |

## Tests

- `Tests/CellPatternTests`: property checks of the cell pattern against a brute-force definition, the
  settings-to-pattern mapping, and that a new seed moves the cores and a wider gap shrinks them
  (`dotnet run -c Release --project Tests/CellPatternTests`).
- `Tests/ds`: a standalone Magnetar server in a test slot with MES and encounter mods, and a probe
  plugin that fires MES spawn requests and lists the NPC grids. `run.py PLAN [cfg]` runs it, and
  `check.py guard|gate` judges the result.
- `Tests/check_placements.py LOG...`: checks the decisions in any DS logs, e.g. of every node of a
  cluster: every placed spawn start in a cell core, refusals by reason, Critical fallbacks.
