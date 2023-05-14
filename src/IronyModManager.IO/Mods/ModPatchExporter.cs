﻿// ***********************************************************************
// Assembly         : IronyModManager.IO
// Author           : Mario
// Created          : 03-31-2020
//
// Last Modified By : Mario
// Last Modified On : 05-14-2023
// ***********************************************************************
// <copyright file="ModPatchExporter.cs" company="Mario">
//     Mario
// </copyright>
// <summary></summary>
// ***********************************************************************

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using IronyModManager.DI;
using IronyModManager.IO.Common;
using IronyModManager.IO.Common.MessageBus;
using IronyModManager.IO.Common.Mods;
using IronyModManager.IO.Common.Mods.Models;
using IronyModManager.IO.Common.Readers;
using IronyModManager.Shared;
using IronyModManager.Shared.Cache;
using IronyModManager.Shared.MessageBus;
using IronyModManager.Shared.Models;
using Nito.AsyncEx;
using ValueType = IronyModManager.Shared.Models.ValueType;

namespace IronyModManager.IO.Mods
{
    /// <summary>
    /// Class ModPatchExporter.
    /// Implements the <see cref="IronyModManager.IO.Common.Mods.IModPatchExporter" />
    /// </summary>
    /// <seealso cref="IronyModManager.IO.Common.Mods.IModPatchExporter" />
    [ExcludeFromCoverage("Skipping testing IO logic.")]
    public class ModPatchExporter : IModPatchExporter
    {
        #region Fields

        /// <summary>
        /// The cache external code key
        /// </summary>
        private const string CacheExternalCodeKey = "ExternallyLoadedCode";

        /// <summary>
        /// The cache state key
        /// </summary>
        private const string CacheStateKey = "PatchState";

        /// <summary>
        /// The cache state prefix
        /// </summary>
        private const string CacheStateRegion = "ModPatchExporter";

        /// <summary>
        /// The json state name
        /// </summary>
        private const string JsonStateName = "state" + Shared.Constants.JsonExtension;

        /// <summary>
        /// The mode file name
        /// </summary>
        private const string ModeFileName = "mode.txt";

        /// <summary>
        /// The state backup
        /// </summary>
        private const string StateBackup = StateName + ".bak";

        /// <summary>
        /// The state conflict history extension
        /// </summary>
        private const string StateConflictHistoryExtension = ".txt";

        /// <summary>
        /// The state history
        /// </summary>
        private const string StateHistory = "state_conflict_history";

        /// <summary>
        /// The state name
        /// </summary>
        private const string StateName = "state.irony";

        /// <summary>
        /// The state temporary
        /// </summary>
        private const string StateTemp = StateName + ".tmp";

        /// <summary>
        /// The old format paths
        /// </summary>
        private static readonly List<string> OldFormatPaths = new() { JsonStateName, JsonStateName + ".bak", JsonStateName + ".tmp" };

        /// <summary>
        /// The write lock
        /// </summary>
        private static readonly AsyncLock writeLock = new();

        /// <summary>
        /// The cache
        /// </summary>
        private readonly ICache cache;

        /// <summary>
        /// The definition information providers
        /// </summary>
        private readonly IEnumerable<IDefinitionInfoProvider> definitionInfoProviders;

        /// <summary>
        /// The message bus
        /// </summary>
        private readonly IMessageBus messageBus;

        /// <summary>
        /// The object clone
        /// </summary>
        private readonly IObjectClone objectClone;

        /// <summary>
        /// The reader
        /// </summary>
        private readonly IReader reader;

        /// <summary>
        /// The saving token
        /// </summary>
        private CancellationTokenSource savingToken;

        /// <summary>
        /// The write counter
        /// </summary>
        private int writeCounter = 0;

        #endregion Fields

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ModPatchExporter" /> class.
        /// </summary>
        /// <param name="objectClone">The object clone.</param>
        /// <param name="cache">The cache.</param>
        /// <param name="reader">The reader.</param>
        /// <param name="definitionInfoProviders">The definition information providers.</param>
        /// <param name="messageBus">The message bus.</param>
        public ModPatchExporter(IObjectClone objectClone, ICache cache, IReader reader, IEnumerable<IDefinitionInfoProvider> definitionInfoProviders, IMessageBus messageBus)
        {
            this.cache = cache;
            this.definitionInfoProviders = definitionInfoProviders;
            this.reader = reader;
            this.messageBus = messageBus;
            this.objectClone = objectClone;
        }

        #endregion Constructors

        #region Enums

        /// <summary>
        /// Enum FileNameGeneration
        /// </summary>
        private enum FileNameGeneration
        {
            /// <summary>
            /// The generate file name
            /// </summary>
            GenerateFileName,

            /// <summary>
            /// The use existing file name
            /// </summary>
            UseExistingFileName,

            /// <summary>
            /// The use existing file name and write empty files
            /// </summary>
            UseExistingFileNameAndWriteEmptyFiles
        }

        #endregion Enums

        #region Methods

        /// <summary>
        /// Copies the patch mod asynchronous.
        /// </summary>
        /// <param name="parameters">The parameters.</param>
        /// <returns>Task&lt;System.Boolean&gt;.</returns>
        public Task<bool> CopyPatchModAsync(ModPatchExporterParameters parameters)
        {
            var retry = new RetryStrategy();
            return retry.RetryActionAsync(() => CopyPatchModInternalAsync(parameters));
        }

        /// <summary>
        /// export definition as an asynchronous operation.
        /// </summary>
        /// <param name="parameters">The parameters.</param>
        /// <returns>Task&lt;System.Boolean&gt;.</returns>
        /// <exception cref="ArgumentNullException">nameof(parameters), Game.</exception>
        /// <exception cref="ArgumentNullException">nameof(parameters), Definitions.</exception>
        public async Task<bool> ExportDefinitionAsync(ModPatchExporterParameters parameters)
        {
            async Task<bool> export()
            {
                if (string.IsNullOrWhiteSpace(parameters.Game))
                {
                    throw new ArgumentNullException(nameof(parameters), "Game.");
                }
                var definitionsInvalid = (parameters.Definitions == null || !parameters.Definitions.Any()) &&
                    (parameters.OverwrittenConflicts == null || !parameters.OverwrittenConflicts.Any()) &&
                    (parameters.CustomConflicts == null || !parameters.CustomConflicts.Any());
                if (definitionsInvalid)
                {
                    throw new ArgumentNullException(nameof(parameters), "Definitions.");
                }
                var definitionInfoProvider = definitionInfoProviders.FirstOrDefault(p => p.CanProcess(parameters.Game) && p.IsFullyImplemented);
                if (definitionInfoProvider != null)
                {
                    var results = new List<bool>();

                    if (parameters.Definitions?.Count() > 0)
                    {
                        results.Add(await CopyBinariesAsync(parameters.Definitions.Where(p => p.ValueType == ValueType.Binary),
                            GetPatchRootPath(parameters.RootPath, parameters.PatchPath), false));
                        results.Add(await WriteMergedContentAsync(parameters.Definitions.Where(p => p.ValueType != ValueType.Binary),
                            GetPatchRootPath(parameters.RootPath, parameters.PatchPath), parameters.Game, false, FileNameGeneration.GenerateFileName));
                    }

                    if (parameters.OverwrittenConflicts?.Count() > 0)
                    {
                        results.Add(await CopyBinariesAsync(parameters.OverwrittenConflicts.Where(p => p.ValueType == ValueType.Binary),
                            GetPatchRootPath(parameters.RootPath, parameters.PatchPath), false));
                        results.Add(await WriteMergedContentAsync(parameters.OverwrittenConflicts.Where(p => p.ValueType != ValueType.Binary),
                            GetPatchRootPath(parameters.RootPath, parameters.PatchPath), parameters.Game, false, FileNameGeneration.UseExistingFileNameAndWriteEmptyFiles));
                    }

                    if (parameters.CustomConflicts?.Count() > 0)
                    {
                        results.Add(await WriteMergedContentAsync(parameters.CustomConflicts.Where(p => p.ValueType != ValueType.Binary),
                            GetPatchRootPath(parameters.RootPath, parameters.PatchPath), parameters.Game, true, FileNameGeneration.UseExistingFileName));
                    }
                    return results.All(p => p);
                }
                return false;
            }
            var retry = new RetryStrategy();
            return await retry.RetryActionAsync(() => export());
        }

        /// <summary>
        /// Gets the patch files.
        /// </summary>
        /// <param name="parameters">The parameters.</param>
        /// <returns>IEnumerable&lt;System.String&gt;.</returns>
        public IEnumerable<string> GetPatchFiles(ModPatchExporterParameters parameters)
        {
            var path = GetPatchRootPath(parameters.RootPath, parameters.PatchPath);
            var files = new List<string>();
            if (Directory.Exists(path))
            {
                foreach (var item in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories))
                {
                    var relativePath = item.Replace(path, string.Empty).Trim(Path.DirectorySeparatorChar);
                    if (relativePath.Contains(Path.DirectorySeparatorChar) && !relativePath.Contains(StateHistory, StringComparison.OrdinalIgnoreCase))
                    {
                        files.Add(relativePath);
                    }
                }
            }
            return files;
        }

        /// <summary>
        /// get patch state as an asynchronous operation.
        /// </summary>
        /// <param name="parameters">The parameters.</param>
        /// <param name="loadExternalCode">if set to <c>true</c> [load external code].</param>
        /// <returns>Task&lt;IPatchState&gt;.</returns>
        public async Task<IPatchState> GetPatchStateAsync(ModPatchExporterParameters parameters, bool loadExternalCode = true)
        {
            return await GetPatchStateInternalAsync(parameters, loadExternalCode);
        }

        /// <summary>
        /// Get patch state mode as an asynchronous operation.
        /// </summary>
        /// <param name="parameters">The parameters.</param>
        /// <returns>A Task&lt;IronyModManager.IO.Common.PatchStateMode?&gt; representing the asynchronous operation.</returns>
        public async Task<PatchStateMode?> GetPatchStateModeAsync(ModPatchExporterParameters parameters)
        {
            var rootPath = GetPatchRootPath(parameters.RootPath, parameters.PatchPath);
            var modeFileName = Path.Combine(rootPath, ModeFileName);
            if (File.Exists(modeFileName))
            {
                var content = await File.ReadAllTextAsync(modeFileName);
                if (int.TryParse(content.Trim(), out var value) && Enum.IsDefined(typeof(PatchStateMode), value))
                {
                    return (PatchStateMode)value;
                }
            }
            return null;
        }

        /// <summary>
        /// Loads the definition contents asynchronous.
        /// </summary>
        /// <param name="parameters">The parameters.</param>
        /// <param name="path">The path.</param>
        /// <returns>Task&lt;System.String&gt;.</returns>
        public async Task<string> LoadDefinitionContentsAsync(ModPatchExporterParameters parameters, string path)
        {
            var patchPath = Path.Combine(parameters.RootPath, parameters.PatchPath);
            var state = await GetPatchStateAsync(parameters);
            if (state != null && state.ConflictHistory != null)
            {
                var history = state.ConflictHistory.FirstOrDefault(p => p.FileCI.Equals(path, StringComparison.OrdinalIgnoreCase));
                if (history != null)
                {
                    return history.Code;
                }
            }
            return string.Empty;
        }

        /// <summary>
        /// rename patch mod as an asynchronous operation.
        /// </summary>
        /// <param name="parameters">The parameters.</param>
        /// <returns>Task&lt;System.Boolean&gt;.</returns>
        public async Task<bool> RenamePatchModAsync(ModPatchExporterParameters parameters)
        {
            async Task<bool> rename()
            {
                var result = await CopyPatchModInternalAsync(parameters);
                if (result)
                {
                    var oldPath = Path.Combine(parameters.RootPath, parameters.ModPath);
                    if (Directory.Exists(oldPath))
                    {
                        DiskOperations.DeleteDirectory(oldPath, true);
                    }
                }
                return result;
            };
            var retry = new RetryStrategy();
            return await retry.RetryActionAsync(() => rename());
        }

        /// <summary>
        /// Resets the cache.
        /// </summary>
        public void ResetCache()
        {
            cache.Invalidate(new CacheInvalidateParameters() { Region = CacheStateRegion, Keys = new List<string>() { CacheStateKey } });
        }

        /// <summary>
        /// Saves the state asynchronous.
        /// </summary>
        /// <param name="parameters">The parameters.</param>
        /// <returns>Task&lt;System.Boolean&gt;.</returns>
        public async Task<bool> SaveStateAsync(ModPatchExporterParameters parameters)
        {
            var state = await GetPatchStateInternalAsync(parameters, true);
            state ??= DIResolver.Get<IPatchState>();
            var modifiedHistory = new ConcurrentDictionary<string, IDefinition>();
            var path = Path.Combine(GetPatchRootPath(parameters.RootPath, parameters.PatchPath));
            state.IgnoreConflictPaths = parameters.IgnoreConflictPaths;
            state.ResolvedConflicts = MapDefinitions(parameters.ResolvedConflicts, false);
            state.Conflicts = MapDefinitions(parameters.Conflicts, false);
            state.IgnoredConflicts = MapDefinitions(parameters.IgnoredConflicts, false);
            state.OverwrittenConflicts = MapDefinitions(parameters.OverwrittenConflicts, false);
            state.CustomConflicts = MapDefinitions(parameters.CustomConflicts, false);
            state.Mode = parameters.Mode;
            state.LoadOrder = parameters.LoadOrder;
            state.HasGameDefinitions = parameters.HasGameDefinitions;
            var history = new ConcurrentDictionary<string, IEnumerable<IDefinition>>();
            foreach (var item in state.IndexedConflictHistory)
            {
                history.TryAdd(item.Key, item.Value);
            }
            if (parameters.ResolvedConflicts != null)
            {
                var tasks = parameters.ResolvedConflicts.Where(s => !string.IsNullOrWhiteSpace(s.Code)).Select(item =>
                {
                    return Task.Run(() =>
                    {
                        if (!history.TryGetValue(item.TypeAndId, out var existingHits))
                        {
                            existingHits = new List<IDefinition>();
                        }
                        var existing = existingHits.FirstOrDefault(p => item.Code.Equals(p.Code));
                        if (existing == null)
                        {
                            var definitions = new List<IDefinition>() { item };
                            history.AddOrUpdate(item.TypeAndId, definitions, (k, v) => definitions);
                            modifiedHistory.AddOrUpdate(item.TypeAndId, item, (k, v) => item);
                        }
                        else if (existingHits.Count() > 1)
                        {
                            var definitions = new List<IDefinition>() { existing };
                            history.AddOrUpdate(existing.TypeAndId, definitions, (k, v) => definitions);
                            modifiedHistory.AddOrUpdate(existing.TypeAndId, existing, (k, v) => existing);
                        }
                    });
                });
                await Task.WhenAll(tasks);
            }
            if (parameters.Definitions != null)
            {
                foreach (var item in parameters.Definitions.Where(s => !string.IsNullOrWhiteSpace(s.Code) && !modifiedHistory.Any(p => p.Key.Equals(s.TypeAndId))))
                {
                    var definitions = new List<IDefinition>() { item };
                    history.AddOrUpdate(item.TypeAndId, definitions, (k, v) => definitions);
                    modifiedHistory.AddOrUpdate(item.TypeAndId, item, (k, v) => item);
                }
            }
            state.ConflictHistory = MapDefinitions(history.SelectMany(p => p.Value), true);
            var externallyLoadedCode = cache.Get<HashSet<string>>(new CacheGetParameters() { Key = CacheExternalCodeKey, Region = CacheStateRegion });
            if (externallyLoadedCode == null)
            {
                externallyLoadedCode = new HashSet<string>();
                cache.Set(new CacheAddParameters<HashSet<string>>() { Key = CacheExternalCodeKey, Value = externallyLoadedCode, Region = CacheStateRegion });
            }
            return StoreState(state, modifiedHistory.Select(p => p.Value), externallyLoadedCode, path);
        }

        /// <summary>
        /// Standardizes the definition paths.
        /// </summary>
        /// <param name="definitions">The definitions.</param>
        protected virtual void StandardizeDefinitionPaths(IEnumerable<IDefinition> definitions)
        {
            static IList<string> standardizeArray(IList<string> paths)
            {
                if (paths != null && paths.Any())
                {
                    var newPaths = new List<string>();
                    foreach (var item in paths)
                    {
                        newPaths.Add(item.StandardizeDirectorySeparator());
                    }
                    return newPaths;
                }
                return paths;
            }

            if (definitions != null && definitions.Any())
            {
                foreach (var item in definitions)
                {
                    item.AdditionalFileNames = standardizeArray(item.AdditionalFileNames);
                    item.File = item.File.StandardizeDirectorySeparator();
                    item.GeneratedFileNames = standardizeArray(item.GeneratedFileNames);
                    item.ModPath = item.ModPath.StandardizeDirectorySeparator();
                    item.OverwrittenFileNames = standardizeArray(item.OverwrittenFileNames);
                    item.Type = item.Type.StandardizeDirectorySeparator();
                    item.DiskFile = item.DiskFile.StandardizeDirectorySeparator();
                }
            }
        }

        /// <summary>
        /// copy patch mod internal as an asynchronous operation.
        /// </summary>
        /// <param name="parameters">The parameters.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        private static async Task<bool> CopyPatchModInternalAsync(ModPatchExporterParameters parameters)
        {
            var oldPath = Path.Combine(parameters.RootPath, parameters.ModPath);
            var newPath = Path.Combine(parameters.RootPath, parameters.PatchPath);
            if (Directory.Exists(oldPath))
            {
                var files = Directory.EnumerateFiles(oldPath, "*", SearchOption.AllDirectories);
                foreach (var item in files)
                {
                    var info = new System.IO.FileInfo(item);
                    var destinationPath = Path.Combine(newPath, info.FullName.Replace(oldPath, string.Empty, StringComparison.OrdinalIgnoreCase).TrimStart(Path.DirectorySeparatorChar));
                    if (!Directory.Exists(Path.GetDirectoryName(destinationPath)))
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(destinationPath));
                    }
                    info.CopyTo(destinationPath, true);
                }
                var text = await ReadPatchContentAsync(newPath);
                foreach (var renamePair in parameters.RenamePairs)
                {
                    text = text.Replace($"\"{renamePair.Key}\"", $"\"{renamePair.Value}\"");
                }
                await SavePatchContentAsync(Path.Combine(newPath, StateName), text);
                OldFormatPaths.ForEach(path =>
                {
                    var fullPath = Path.Combine(newPath, path);
                    if (File.Exists(fullPath))
                    {
                        DiskOperations.DeleteFile(fullPath);
                    }
                });
                return true;
            }
            return false;
        }

        /// <summary>
        /// Gets the patch root path.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="patchPath">The patch path.</param>
        /// <returns>string.</returns>
        private static string GetPatchRootPath(string path, string patchPath)
        {
            return Path.Combine(path, patchPath);
        }

        /// <summary>
        /// Read patch content as an asynchronous operation.
        /// </summary>
        /// <param name="homePath">The home path.</param>
        /// <returns>A Task&lt;string&gt; representing the asynchronous operation.</returns>
        private static async Task<string> ReadPatchContentAsync(string homePath)
        {
            var jsonPath = Path.Combine(homePath, JsonStateName);
            var path = Path.Combine(homePath, StateName);
            if (File.Exists(jsonPath))
            {
                return await File.ReadAllTextAsync(jsonPath);
            }
            else if (File.Exists(path))
            {
                var bytes = await File.ReadAllBytesAsync(path);
                if (bytes.Any())
                {
                    using var source = new MemoryStream(bytes);
                    using var destination = new MemoryStream();
                    using var compress = new GZipStream(source, CompressionMode.Decompress);
                    await compress.CopyToAsync(destination);
                    var text = Encoding.UTF8.GetString(destination.ToArray());
                    return text;
                }
            }
            return string.Empty;
        }

        /// <summary>
        /// Save patch content as an asynchronous operation.
        /// </summary>
        /// <param name="fullPath">The full path.</param>
        /// <param name="content">The content.</param>
        /// <returns>A Task&lt;bool&gt; representing the asynchronous operation.</returns>
        private static async Task<bool> SavePatchContentAsync(string fullPath, string content)
        {
            var bytes = Encoding.UTF8.GetBytes(content);
            using var source = new MemoryStream(bytes);
            using var destination = new MemoryStream();
            using var compress = new GZipStream(destination, CompressionLevel.Fastest, true);
            await source.CopyToAsync(compress);
            await compress.FlushAsync();
            await File.WriteAllBytesAsync(fullPath, destination.ToArray());
            return true;
        }

        /// <summary>
        /// Copies the binaries asynchronous.
        /// </summary>
        /// <param name="definitions">The definitions.</param>
        /// <param name="patchRootPath">The patch root path.</param>
        /// <param name="checkIfExists">The check if exists.</param>
        /// <returns>System.Threading.Tasks.Task&lt;System.Boolean&gt;.</returns>
        private async Task<bool> CopyBinariesAsync(IEnumerable<IDefinition> definitions, string patchRootPath, bool checkIfExists)
        {
            var tasks = new List<Task>();
            var streams = new List<Stream>();

            var retry = new RetryStrategy();

            static async Task<bool> copyStream(Stream s, FileStream fs)
            {
                await s.CopyToAsync(fs);
                return true;
            }

            foreach (var def in definitions)
            {
                var outPath = Path.Combine(patchRootPath, def.File);
                if (checkIfExists && File.Exists(outPath))
                {
                    continue;
                }
                var stream = reader.GetStream(def.ModPath, def.File);
                // If image and no stream try switching extension
                if (FileSignatureUtility.IsImageFile(def.File) && stream == null)
                {
                    var segments = def.File.Split(".", StringSplitOptions.RemoveEmptyEntries);
                    var file = string.Join(".", segments.Take(segments.Length - 1));
                    foreach (var item in Shared.Constants.ImageExtensions)
                    {
                        stream = reader.GetStream(def.ModPath, file + item);
                        if (stream != null)
                        {
                            break;
                        }
                    }
                }
                if (!Directory.Exists(Path.GetDirectoryName(outPath)))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(outPath));
                }
                var fs = new FileStream(outPath, FileMode.Create, FileAccess.Write, FileShare.Read);
                if (stream.CanSeek)
                {
                    stream.Seek(0, SeekOrigin.Begin);
                }
                tasks.Add(retry.RetryActionAsync(() =>
                {
                    return copyStream(stream, fs);
                }));
                streams.Add(stream);
                streams.Add(fs);
            }
            if (tasks.Count > 0)
            {
                await Task.WhenAll(tasks);
                foreach (var fs in streams)
                {
                    fs.Close();
                    await fs.DisposeAsync();
                }
            }
            return true;
        }

        /// <summary>
        /// Gets the state of the patch.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns>IPatchState.</returns>
        private IPatchState GetPatchState(string path)
        {
            var cachedItem = cache.Get<CachedState>(new CacheGetParameters() { Key = CacheStateKey, Region = CacheStateRegion });
            if (cachedItem != null)
            {
                var lastPath = cachedItem.LastCachedPath ?? string.Empty;
                if (!lastPath.Equals(path, StringComparison.OrdinalIgnoreCase))
                {
                    ResetCache();
                    return null;
                }
                return cachedItem.PatchState;
            }
            return null;
        }

        /// <summary>
        /// Gets the patch state internal asynchronous.
        /// </summary>
        /// <param name="parameters">The parameters.</param>
        /// <param name="loadExternalCode">The load external code.</param>
        /// <returns>System.Threading.Tasks.Task&lt;IronyModManager.IO.Common.Mods.Models.IPatchState&gt;.</returns>
        private async Task<IPatchState> GetPatchStateInternalAsync(ModPatchExporterParameters parameters, bool loadExternalCode)
        {
            var rootPath = GetPatchRootPath(parameters.RootPath, parameters.PatchPath);
            var statePath = Path.Combine(rootPath, StateName);
            var jsonStatePath = Path.Combine(rootPath, JsonStateName);
            var cached = GetPatchState(statePath);
            if ((File.Exists(statePath) || File.Exists(jsonStatePath)) && cached == null)
            {
                using var mutex = await writeLock.LockAsync();
                var text = await ReadPatchContentAsync(rootPath);
                if (!string.IsNullOrWhiteSpace(text))
                {
                    cached = JsonDISerializer.Deserialize<IPatchState>(text);
                    if (string.IsNullOrEmpty(cached.IgnoreConflictPaths))
                    {
                        cached.IgnoreConflictPaths = string.Empty;
                    }
                    if (cached.ConflictHistory == null)
                    {
                        cached.ConflictHistory = new List<IDefinition>();
                    }
                    else
                    {
                        StandardizeDefinitionPaths(cached.ConflictHistory);
                    }
                    if (cached.Conflicts == null)
                    {
                        cached.Conflicts = new List<IDefinition>();
                    }
                    else
                    {
                        StandardizeDefinitionPaths(cached.Conflicts);
                    }
                    if (cached.IgnoredConflicts == null)
                    {
                        cached.IgnoredConflicts = new List<IDefinition>();
                    }
                    else
                    {
                        StandardizeDefinitionPaths(cached.IgnoredConflicts);
                    }
                    if (cached.ResolvedConflicts == null)
                    {
                        cached.ResolvedConflicts = new List<IDefinition>();
                    }
                    else
                    {
                        StandardizeDefinitionPaths(cached.ResolvedConflicts);
                    }
                    if (cached.OverwrittenConflicts == null)
                    {
                        cached.OverwrittenConflicts = new List<IDefinition>();
                    }
                    else
                    {
                        StandardizeDefinitionPaths(cached.OverwrittenConflicts);
                    }
                    if (cached.CustomConflicts == null)
                    {
                        cached.CustomConflicts = new List<IDefinition>();
                    }
                    else
                    {
                        StandardizeDefinitionPaths(cached.CustomConflicts);
                    }
                    cached.LoadOrder ??= new List<string>();
                    // If not allowing full load don't cache anything
                    if (loadExternalCode)
                    {
                        var externallyLoadedCode = new ConcurrentBag<string>();
                        async Task loadCode(IDefinition definition)
                        {
                            var historyPath = Path.Combine(GetPatchRootPath(parameters.RootPath, parameters.PatchPath), StateHistory, definition.Type, definition.Id.GenerateValidFileName() + StateConflictHistoryExtension);
                            if (File.Exists(historyPath))
                            {
                                var code = await File.ReadAllTextAsync(historyPath);
                                definition.Code = string.Join(Environment.NewLine, code.SplitOnNewLine());
                                externallyLoadedCode.Add(definition.TypeAndId);
                            }
                        }
                        var tasks = new List<Task>();
                        foreach (var item in cached.ConflictHistory)
                        {
                            tasks.Add(loadCode(item));
                        }
                        var cachedItem = new CachedState()
                        {
                            LastCachedPath = statePath,
                            PatchState = cached
                        };
                        await Task.WhenAll(tasks);
                        cache.Set(new CacheAddParameters<CachedState>() { Region = CacheStateRegion, Key = CacheStateKey, Value = cachedItem });
                        cache.Set(new CacheAddParameters<HashSet<string>>() { Region = CacheStateRegion, Key = CacheExternalCodeKey, Value = externallyLoadedCode.Distinct().ToHashSet() });
                    }
                }
                mutex.Dispose();
            }
            if (cached != null)
            {
                var result = DIResolver.Get<IPatchState>();
                MapPatchState(cached, result, true);
                return result;
            }
            return null;
        }

        /// <summary>
        /// Maps the definition.
        /// </summary>
        /// <param name="original">The original.</param>
        /// <param name="includeCode">if set to <c>true</c> [include code].</param>
        /// <returns>IDefinition.</returns>
        private IDefinition MapDefinition(IDefinition original, bool includeCode)
        {
            return objectClone.CloneDefinition(original, includeCode);
        }

        /// <summary>
        /// Maps the definitions.
        /// </summary>
        /// <param name="originals">The originals.</param>
        /// <param name="includeCode">if set to <c>true</c> [include code].</param>
        /// <returns>IEnumerable&lt;IDefinition&gt;.</returns>
        private IEnumerable<IDefinition> MapDefinitions(IEnumerable<IDefinition> originals, bool includeCode)
        {
            var col = new List<IDefinition>();
            if (originals != null)
            {
                foreach (var original in originals)
                {
                    col.Add(MapDefinition(original, includeCode));
                }
            }
            return col;
        }

        /// <summary>
        /// Maps the state of the patch.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="destination">The destination.</param>
        /// <param name="includeCode">if set to <c>true</c> [include code].</param>
        private void MapPatchState(IPatchState source, IPatchState destination, bool includeCode)
        {
            destination.ConflictHistory = MapDefinitions(source.ConflictHistory, includeCode);
            destination.Conflicts = MapDefinitions(source.Conflicts, includeCode);
            destination.IgnoreConflictPaths = source.IgnoreConflictPaths;
            destination.IgnoredConflicts = MapDefinitions(source.IgnoredConflicts, includeCode);
            destination.ResolvedConflicts = MapDefinitions(source.ResolvedConflicts, includeCode);
            destination.OverwrittenConflicts = MapDefinitions(source.OverwrittenConflicts, includeCode);
            destination.CustomConflicts = MapDefinitions(source.CustomConflicts, includeCode);
            destination.Mode = source.Mode;
            destination.LoadOrder = source.LoadOrder;
            destination.HasGameDefinitions = source.HasGameDefinitions;
        }

        /// <summary>
        /// Stores the state.
        /// </summary>
        /// <param name="model">The model.</param>
        /// <param name="modifiedHistory">The modified history.</param>
        /// <param name="externalCode">The external code.</param>
        /// <param name="path">The path.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        private bool StoreState(IPatchState model, IEnumerable<IDefinition> modifiedHistory, HashSet<string> externalCode, string path)
        {
            var statePath = Path.Combine(path, StateName);

            var cachedItem = cache.Get<CachedState>(new CacheGetParameters() { Key = CacheStateKey, Region = CacheStateRegion });
            cachedItem ??= new CachedState();
            cachedItem.LastCachedPath = statePath;
            cachedItem.PatchState = model;
            cache.Set(new CacheAddParameters<CachedState>() { Key = CacheStateKey, Value = cachedItem, Region = CacheStateRegion });

            savingToken?.Cancel();
            savingToken = new CancellationTokenSource();
            WriteStateInBackground(model, modifiedHistory, externalCode, path, savingToken.Token).ConfigureAwait(false);
            return true;
        }

        /// <summary>
        /// Writes the merged content asynchronous.
        /// </summary>
        /// <param name="definitions">The definitions.</param>
        /// <param name="patchRootPath">The patch root path.</param>
        /// <param name="game">The game.</param>
        /// <param name="checkIfFileExists">The check if file exists.</param>
        /// <param name="mode">The mode.</param>
        /// <returns>System.Threading.Tasks.Task&lt;System.Boolean&gt;.</returns>
        private async Task<bool> WriteMergedContentAsync(IEnumerable<IDefinition> definitions, string patchRootPath, string game, bool checkIfFileExists, FileNameGeneration mode)
        {
            var tasks = new List<Task>();
            List<bool> results = new List<bool>();
            var validDefinitions = definitions.Where(p => p.ValueType != ValueType.Namespace && p.ValueType != ValueType.Variable);
            var retry = new RetryStrategy();
            async Task evalZeroByteFiles(IDefinition definition, IDefinitionInfoProvider infoProvider, string fileName, string diskFile)
            {
                if (mode == FileNameGeneration.UseExistingFileNameAndWriteEmptyFiles)
                {
                    var emptyFileNames = definition.OverwrittenFileNames.Where(p => p != fileName && p != diskFile);
                    foreach (var emptyFile in emptyFileNames)
                    {
                        var emptyPath = Path.Combine(patchRootPath, emptyFile);
                        await retry.RetryActionAsync(async () =>
                        {
                            await File.WriteAllTextAsync(emptyPath, string.Empty, infoProvider.GetEncoding(definition));
                            return true;
                        });
                    }
                }
            }

            foreach (var item in validDefinitions)
            {
                var infoProvider = definitionInfoProviders.FirstOrDefault(p => p.CanProcess(game) && p.IsFullyImplemented);
                if (infoProvider != null)
                {
                    string diskFile = string.Empty;
                    string fileName = string.Empty;
                    fileName = mode switch
                    {
                        FileNameGeneration.GenerateFileName => infoProvider.GetFileName(item),
                        _ => item.File
                    };
                    diskFile = mode switch
                    {
                        FileNameGeneration.GenerateFileName => infoProvider.GetDiskFileName(item),
                        _ => !string.IsNullOrWhiteSpace(item.DiskFile) ? item.DiskFile : item.File
                    };
                    // For backwards compatibility when filename was used
                    var altFileName = Path.Combine(patchRootPath, fileName);
                    if (diskFile != fileName && File.Exists(altFileName))
                    {
                        DiskOperations.DeleteFile(altFileName);
                    }
                    var outPath = Path.Combine(patchRootPath, diskFile);
                    if (checkIfFileExists && File.Exists(outPath))
                    {
                        // Zero byte files could still not be present
                        await evalZeroByteFiles(item, infoProvider, fileName, diskFile);
                        continue;
                    }
                    if (!Directory.Exists(Path.GetDirectoryName(outPath)))
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(outPath));
                    }
                    // Update filename
                    item.DiskFile = diskFile;
                    item.File = fileName;
                    tasks.Add(retry.RetryActionAsync(async () =>
                    {
                        var code = item.Code;
                        if (!code.EndsWith(Environment.NewLine))
                        {
                            code += Environment.NewLine;
                        }
                        await File.WriteAllTextAsync(outPath, code, infoProvider.GetEncoding(item));
                        return true;
                    }));
                    await evalZeroByteFiles(item, infoProvider, fileName, diskFile);
                    results.Add(true);
                }
                else
                {
                    results.Add(false);
                }
            }
            if (tasks.Count > 0)
            {
                await Task.WhenAll(tasks);
            }

            return results.All(p => p);
        }

        /// <summary>
        /// Writes the state in background.
        /// </summary>
        /// <param name="model">The model.</param>
        /// <param name="modifiedHistory">The modified history.</param>
        /// <param name="externalCode">The external code.</param>
        /// <param name="path">The path.</param>
        /// <param name="cancellationToken">The cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
        /// <returns>System.Threading.Tasks.Task.</returns>
        private async Task WriteStateInBackground(IPatchState model, IEnumerable<IDefinition> modifiedHistory, HashSet<string> externalCode, string path, CancellationToken cancellationToken)
        {
            writeCounter++;
            using var ctr = cancellationToken.Register(() =>
            {
                writeCounter--;
                messageBus.PublishAsync(new WritingStateOperationEvent(writeCounter <= 0)).ConfigureAwait(false);
            });
            using var mutex = await writeLock.LockAsync(cancellationToken);
            await messageBus.PublishAsync(new WritingStateOperationEvent(writeCounter <= 0));
            var statePath = Path.Combine(path, StateName);
            var backupPath = Path.Combine(path, StateBackup);
            var stateTemp = Path.Combine(path, StateTemp);

            await Task.Run(async () =>
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }
                var retry = new RetryStrategy();
                var patchState = DIResolver.Get<IPatchState>();
                MapPatchState(model, patchState, true);
                foreach (var item in patchState.ConflictHistory)
                {
                    if (externalCode != null && externalCode.Contains(item.TypeAndId))
                    {
                        item.Code = null;
                    }
                }

                var loadedCode = new HashSet<string>();
                foreach (var item in modifiedHistory)
                {
                    var historyPath = Path.Combine(path, StateHistory, item.Type, item.Id.GenerateValidFileName() + StateConflictHistoryExtension);
                    var historyDirectory = Path.GetDirectoryName(historyPath);
                    if (!Directory.Exists(historyDirectory))
                    {
                        Directory.CreateDirectory(historyDirectory);
                    }
                    if (externalCode != null && !externalCode.Contains(item.TypeAndId))
                    {
                        loadedCode.Add(item.TypeAndId);
                        if (patchState.IndexedConflictHistory.TryGetValue(item.TypeAndId, out var value))
                        {
                            var existingHistory = value;
                            foreach (var existing in existingHistory)
                            {
                                existing.Code = null;
                            }
                        }
                    }
                    await retry.RetryActionAsync(async () =>
                    {
                        await File.WriteAllTextAsync(historyPath, item.Code);
                        return true;
                    });
                }

                var existingLoadedCode = cache.Get<HashSet<string>>(new CacheGetParameters() { Key = CacheExternalCodeKey, Region = CacheStateRegion });
                if (existingLoadedCode != null)
                {
                    foreach (var item in loadedCode)
                    {
                        existingLoadedCode.Add(item);
                    }
                    cache.Set(new CacheAddParameters<HashSet<string>>() { Key = CacheExternalCodeKey, Value = existingLoadedCode, Region = CacheStateRegion });
                }

                var dirPath = Path.GetDirectoryName(statePath);
                if (!Directory.Exists(dirPath))
                {
                    Directory.CreateDirectory(dirPath);
                }

                if (File.Exists(stateTemp))
                {
                    DiskOperations.DeleteFile(stateTemp);
                }
                var serialized = JsonDISerializer.Serialize(patchState);
                await retry.RetryActionAsync(async () =>
                {
                    return await SavePatchContentAsync(stateTemp, serialized);
                });
                if (File.Exists(backupPath))
                {
                    DiskOperations.DeleteFile(backupPath);
                }
                if (File.Exists(statePath))
                {
                    File.Copy(statePath, backupPath);
                    DiskOperations.DeleteFile(statePath);
                }
                if (File.Exists(stateTemp))
                {
                    File.Copy(stateTemp, statePath);
                }
                var modeFileName = Path.Combine(path, ModeFileName);
                await File.WriteAllTextAsync(modeFileName, ((int)model.Mode).ToString());
                OldFormatPaths.ForEach(oldPath =>
                {
                    var fullPath = Path.Combine(path, oldPath);
                    if (File.Exists(fullPath))
                    {
                        DiskOperations.DeleteFile(fullPath);
                    }
                });
                writeCounter--;
                await messageBus.PublishAsync(new WritingStateOperationEvent(writeCounter <= 0));
                mutex.Dispose();
            }, CancellationToken.None).ConfigureAwait(false);
        }

        #endregion Methods

        #region Classes

        /// <summary>
        /// Class CachedState.
        /// </summary>
        private class CachedState
        {
            #region Properties

            /// <summary>
            /// Gets or sets the last cached path.
            /// </summary>
            /// <value>The last cached path.</value>
            public string LastCachedPath { get; set; }

            /// <summary>
            /// Gets or sets the state of the patch.
            /// </summary>
            /// <value>The state of the patch.</value>
            public IPatchState PatchState { get; set; }

            #endregion Properties
        }

        #endregion Classes
    }
}
