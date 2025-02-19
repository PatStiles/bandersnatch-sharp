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
//

using System.IO.Abstractions;

namespace Nethermind.Db.FullPruning
{
    /// <summary>
    /// Factory
    /// </summary>
    public class FullPruningInnerDbFactory : IRocksDbFactory
    {
        private readonly IRocksDbFactory _rocksDbFactory;
        private readonly IFileSystem _fileSystem;
        private int _index; // current index of the inner db

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="rocksDbFactory">Inner real db factory.</param>
        /// <param name="fileSystem">File system.</param>
        /// <param name="path">Main DB path.</param>
        public FullPruningInnerDbFactory(IRocksDbFactory rocksDbFactory, IFileSystem fileSystem, string path)
        {
            _rocksDbFactory = rocksDbFactory;
            _fileSystem = fileSystem;
            _index = GetStartingIndex(path); // we need to read the current state of inner DB's
        }

        /// <inheritdoc />
        public IDb CreateDb(DbSettings dbSettings)
        {
            DbSettings settings = GetDbSettings(dbSettings);
            return _rocksDbFactory.CreateDb(settings);
        }

        /// <inheritdoc />
        public IColumnsDb<T> CreateColumnsDb<T>(DbSettings dbSettings) where T : struct, Enum
        {
            DbSettings settings = GetDbSettings(dbSettings);
            return _rocksDbFactory.CreateColumnsDb<T>(settings);
        }

        /// <inheritdoc />
        public string GetFullDbPath(DbSettings dbSettings)
        {
            DbSettings settings = GetDbSettings(dbSettings);
            return _rocksDbFactory.GetFullDbPath(settings);
        }

        // When creating a new DB, we need to change its inner settings
        private DbSettings GetDbSettings(DbSettings dbSettings)
        {
            _index++;

            // if its -1 then this is first db.
            bool firstDb = _index == -1;

            // if first DB, then we will put it into main directory and not use indexed subdirectory
            string dbName = firstDb ? dbSettings.DbName : dbSettings.DbName + _index;
            string dbPath = firstDb ? dbSettings.DbPath : _fileSystem.Path.Combine(dbSettings.DbPath, _index.ToString());
            DbSettings newDbSettings = dbSettings.Clone(dbName, dbPath);
            newDbSettings.CanDeleteFolder = !firstDb; // we cannot delete main db folder, only indexed subfolders
            return newDbSettings;
        }

        /// <summary>
        /// Gets the current start index for indexed DB's
        /// </summary>
        /// <param name="path">Main path to DB directory.</param>
        /// <returns>Current - starting index of DB.</returns>
        private int GetStartingIndex(string path)
        {
            // gets path to non-index DB.
            string fullPath = _rocksDbFactory.GetFullDbPath(new DbSettings(string.Empty, path));
            IDirectoryInfo directory = _fileSystem.DirectoryInfo.FromDirectoryName(fullPath);
            if (directory.Exists)
            {
                if (directory.EnumerateFiles().Any())
                {
                    return -2; // if there are files in the directory, then we have a main DB, marked -2.
                }

                // else we have sub-directories, which should be index based
                // we want to find lowest positive index and return it - 1.
                int minIndex = directory.EnumerateDirectories()
                    .Select(d => int.TryParse(d.Name, out int index) ? index : -1)
                    .Where(i => i >= 0)
                    .OrderBy(i => i)
                    .FirstOrDefault();

                return minIndex - 1;
            }

            return -1; // if directory doesn't exist current index is -1.
        }
    }
}
