//  Copyright (c) 2021 Demerzel Solutions Limited
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

using System.Collections.Concurrent;
using Nethermind.Utils;
using Nethermind.Utils.Extensions;

namespace Nethermind.Db
{
    public class SimpleFilePublicKeyDb : IFullDb
    {
        public const string DbFileName = "SimpleFileDb.db";

        private bool _hasPendingChanges;
        private ConcurrentDictionary<byte[], byte[]> _cache;

        public string DbPath { get; }
        public string Name { get; }
        public string Description { get; }

        public ICollection<byte[]> Keys => _cache.Keys.ToArray();
        public ICollection<byte[]> Values => _cache.Values;
        public int Count => _cache.Count;

        public SimpleFilePublicKeyDb(string name, string dbDirectoryPath)
        {
            if (dbDirectoryPath is null) throw new ArgumentNullException(nameof(dbDirectoryPath));
            Name = name ?? throw new ArgumentNullException(nameof(name));
            DbPath = Path.Combine(dbDirectoryPath, DbFileName);
            Description = $"{Name}|{DbPath}";

            if (!Directory.Exists(dbDirectoryPath))
            {
                Directory.CreateDirectory(dbDirectoryPath);
            }

            LoadData();
        }

        public byte[] this[byte[] key]
        {
            get => _cache[key];
            set
            {
                if (value is null)
                {
                    _cache.TryRemove(key, out _);
                }
                else
                {
                    _cache.AddOrUpdate(key, newValue => Add(value), (x, oldValue) => Update(oldValue, value));
                }
            }
        }

        public KeyValuePair<byte[], byte[]>[] this[byte[][] keys] => keys.Select(k => new KeyValuePair<byte[], byte[]>(k, _cache.TryGetValue(k, out var value) ? value : null)).ToArray();

        public void Remove(byte[] key)
        {
            _hasPendingChanges = true;
            _cache.TryRemove(key, out _);
        }

        public bool KeyExists(byte[] key)
        {
            return _cache.ContainsKey(key);
        }

        public IDb Innermost => this;
        public void Flush() { }
        public void Clear()
        {
            File.Delete(DbPath);
        }

        public IEnumerable<KeyValuePair<byte[], byte[]>> GetAll(bool ordered = false) => _cache;

        public IEnumerable<byte[]> GetAllValues(bool ordered = false) => _cache.Values;

        public IBatch StartBatch()
        {
            return this.LikeABatch(CommitBatch);
        }

        private void CommitBatch()
        {
            if (!_hasPendingChanges)
            {
                Console.WriteLine($"Skipping commit ({Name}), no changes");
                return;
            }

            using Backup backup = new Backup(DbPath);
            _hasPendingChanges = false;
            KeyValuePair<byte[], byte[]>[] snapshot = _cache.ToArray();

            Console.WriteLine($"Saving data in {DbPath} | backup stored in {backup.BackupPath}");
            try
            {
                using StreamWriter streamWriter = new(DbPath);
                foreach ((byte[] key, byte[] value) in snapshot)
                {
                    if (value is not null)
                    {
                        key.StreamHex(streamWriter);
                        streamWriter.Write(',');
                        value.StreamHex(streamWriter);
                        streamWriter.WriteLine();
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Failed to store data in {DbPath}", e);
            }
        }

        private class Backup : IDisposable
        {
            private readonly string _dbPath;
            public string BackupPath { get; }

            public Backup(string dbPath)
            {
                _dbPath = dbPath;
                try
                {
                    BackupPath = $"{_dbPath}_{Guid.NewGuid().ToString()}";

                    if (File.Exists(_dbPath))
                    {
                        File.Move(_dbPath, BackupPath);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Error during backup creation for {_dbPath} | backup path {BackupPath}", e);
                }
            }

            public void Dispose()
            {
                try
                {
                    if (BackupPath is not null && File.Exists(BackupPath))
                    {
                        if (File.Exists(_dbPath))
                        {
                            File.Delete(BackupPath);
                        }
                        else
                        {
                            File.Move(BackupPath, _dbPath);
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Error during backup removal of {_dbPath} | backup path {BackupPath}", e);
                }
            }
        }

        private void LoadData()
        {
            _cache = new ConcurrentDictionary<byte[], byte[]>(Bytes.EqualityComparer);

            if (!File.Exists(DbPath))
            {
                return;
            }

            string[] lines = File.ReadAllLines(DbPath);
            foreach (string line in lines)
            {
                string[] values = line.Split(",");
                if (values.Length != 2)
                {
                    Console.WriteLine($"Error when loading data from {Name} - expected two items separated by a comma and got '{line}')");
                    continue;
                }

                _cache[Bytes.FromHexString(values[0])] = Bytes.FromHexString(values[1]);
            }
        }

        private byte[] Update(byte[] oldValue, byte[] newValue)
        {
            if (!Bytes.AreEqual(oldValue, newValue))
            {
                _hasPendingChanges = true;
            }

            return newValue;
        }

        private byte[] Add(byte[] value)
        {
            _hasPendingChanges = true;
            return value;
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }
    }
}
