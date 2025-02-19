//  Copyright (c) 2022 Demerzel Solutions Limited
//  This file is part of the Nethermind library.
//
//  The Nethermind library is free software: you can redistribute it and/or modify
//  it under the terms of the GNU Lesser General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  The Nethermind library is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
//  GNU Lesser General Public License for more details.
//
//  You should have received a copy of the GNU Lesser General Public License
//  along with the Nethermind. If not, see <http://www.gnu.org/licenses/>.

using System.Text.RegularExpressions;
using Nethermind.Db.Rocks.Config;
using RocksDbSharp;

namespace Nethermind.Db.Rocks.Statistics;

public class DbMetricsUpdater
{
    private readonly string _dbName;
    private readonly DbOptions _dbOptions;
    private readonly RocksDb _db;
    private readonly IDbConfig _dbConfig;
    private Timer? _timer;

    public DbMetricsUpdater(string dbName, DbOptions dbOptions, RocksDb db, IDbConfig dbConfig)
    {
        _dbName = dbName;
        _dbOptions = dbOptions;
        _db = db;
        _dbConfig = dbConfig;
    }

    public void StartUpdating()
    {
        var offsetInSec = _dbConfig.StatsDumpPeriodSec * 1.1;

        _timer = new Timer(UpdateMetrics, null, TimeSpan.FromSeconds(offsetInSec), TimeSpan.FromSeconds(offsetInSec));
    }

    private void UpdateMetrics(object? state)
    {
        try
        {
            // It seems that currently there is no other option with .NET api to extract the compaction statistics than through the dumped string
            var compactionStatsString = _db.GetProperty("rocksdb.stats");
            ProcessCompactionStats(compactionStatsString);

            if (_dbConfig.EnableDbStatistics)
            {
                var dbStatsString = _dbOptions.GetStatisticsString();
                // Currently we don't extract any DB statistics but we can do it here
            }
        }
        catch (Exception exc)
        {
            Console.WriteLine($"Error when updating metrics for {_dbName} database.", exc);
            // Maybe we would like to stop the _timer here to avoid logging the same error all over again?
        }
    }

    public void ProcessCompactionStats(string compactionStatsString)
    {
        if (!string.IsNullOrEmpty(compactionStatsString))
        {
            var stats = ExctractStatsPerLevel(compactionStatsString);
            UpdateMetricsFromList(stats);

            stats = ExctractIntervalCompaction(compactionStatsString);
            UpdateMetricsFromList(stats);
        }
        else
        {
            Console.WriteLine($"No RocksDB compaction stats available for {_dbName} databse.");
        }
    }

    private void UpdateMetricsFromList(List<(string Name, long Value)> levelStats)
    {
        if (levelStats is not null)
        {
            foreach (var stat in levelStats)
            {
                Metrics.DbStats[$"{_dbName}Db{stat.Name}"] = stat.Value;
            }
        }
    }

    /// <summary>
    /// Example line:
    ///   L0      2/0    1.77 MB   0.5      0.0     0.0      0.0       0.4      0.4       0.0   1.0      0.0     44.6      9.83              0.00       386    0.025       0      0
    /// </summary>
    private List<(string Name, long Value)> ExctractStatsPerLevel(string compactionStatsDump)
    {
        var stats = new List<(string Name, long Value)>(5);

        if (!string.IsNullOrEmpty(compactionStatsDump))
        {
            var rgx = new Regex(@"^\s+L(\d+)\s+(\d+)\/(\d+).*$", RegexOptions.Multiline);
            var matches = rgx.Matches(compactionStatsDump);

            foreach (Match m in matches)
            {
                var level = int.Parse(m.Groups[1].Value);
                stats.Add(($"Level{level}Files", int.Parse(m.Groups[2].Value)));
                stats.Add(($"Level{level}FilesCompacted", int.Parse(m.Groups[3].Value)));
            }
        }

        return stats;
    }

    /// <summary>
    /// Example line:
    /// Interval compaction: 0.00 GB write, 0.00 MB/s write, 0.00 GB read, 0.00 MB/s read, 0.0 seconds
    /// </summary>
    private List<(string Name, long Value)> ExctractIntervalCompaction(string compactionStatsDump)
    {
        var stats = new List<(string Name, long Value)>(5);

        if (!string.IsNullOrEmpty(compactionStatsDump))
        {
            var rgx = new Regex(@"^Interval compaction: (\d+)\.\d+.*GB write.*\s+(\d+)\.\d+.*MB\/s write.*\s+(\d+)\.\d+.*GB read.*\s+(\d+)\.\d+.*MB\/s read.*\s+(\d+)\.\d+.*seconds.*$", RegexOptions.Multiline);
            var match = rgx.Match(compactionStatsDump);

            if (match is not null && match.Success)
            {
                stats.Add(("IntervalCompactionGBWrite", long.Parse(match.Groups[1].Value)));
                stats.Add(("IntervalCompactionMBPerSecWrite", long.Parse(match.Groups[2].Value)));
                stats.Add(("IntervalCompactionGBRead", long.Parse(match.Groups[3].Value)));
                stats.Add(("IntervalCompactionMBPerSecRead", long.Parse(match.Groups[4].Value)));
                stats.Add(("IntervalCompactionSeconds", long.Parse(match.Groups[5].Value)));
            }
            else
            {
                Console.WriteLine($"Cannot find 'Interval compaction' stats for {_dbName} database in the compation stats dump:{Environment.NewLine}{compactionStatsDump}");
            }
        }

        return stats;
    }
}
