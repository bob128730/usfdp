using Avalonia.OpenGL;
using DTOs;
using F23.StringSimilarity;
using Octodiff.Core;
using System.Net.Security;
using Ussedp;
using Wabbajack.Common;
using Wabbajack.Hashing.xxHash64;
using Wabbajack.Paths;
using Wabbajack.Paths.IO;
using Wabbajack.VFS;

namespace BuildAllPatches;

public class Generator
{
    public static async IAsyncEnumerable<Instruction> GeneratePlan(Version fromVersion, Version toVersion,
        Dictionary<RelativePath, AbsolutePath> fromFiles, Dictionary<RelativePath, AbsolutePath> toFiles,
        FileHashCache fileHashCache)
    {
        var scomp = new Levenshtein();

        var filesToDelete = fromFiles.Where(f => !toFiles.ContainsKey(f.Key))
            .Select(f => f.Key)
            .ToHashSet();

        foreach (var file in filesToDelete)
        {
            yield return new Instruction
            {
                Path = file.ToString(),
                Method = ResultType.Deleted
            };
        }

        foreach (var toFile in toFiles)
        {
            if (fromFiles.TryGetValue(toFile.Key, out var found))
            {
                if (found.Size() == toFile.Value.Size())
                {
                    var toHash = fileHashCache.FileHashCachedAsync(toFile.Value, CancellationToken.None);
                    var fromHash = fileHashCache.FileHashCachedAsync(found, CancellationToken.None);

                    if (await toHash == await fromHash)
                    {
                        continue;
                    }

                    yield return new Instruction
                    {
                        FromFile = toFile.Key.ToString(),
                        Path = toFile.Key.ToString(),
                        SrcHash = (long) await fromHash,
                        DestHash = (long) await toHash,
                        PatchFile = $"{(await fromHash).ToHex()}_{(await toHash).ToHex()}",
                        Method = ResultType.Patched
                    };
                    continue;
                }
            }
            
            var fromFile = fromFiles.Where(f => !filesToDelete.Contains(f.Key))
                .OrderBy(f => scomp.Distance(f.Key.ToString(), toFile.Key.ToString()))
                .First();

            var fromHash2 = fileHashCache.FileHashCachedAsync(fromFile.Value, CancellationToken.None);
            var toHash2 = fileHashCache.FileHashCachedAsync(toFile.Value, CancellationToken.None);

            yield return new Instruction
            {
                FromFile = fromFile.Key.ToString(),
                SrcHash = (long) await fromHash2,
                Path = toFile.Key.ToString(),
                DestHash = (long) await toHash2,
                PatchFile = $"{(await fromHash2).ToHex()}_{(await toHash2).ToHex()}",
                Method = ResultType.Patched
            };
        }
    }

    public static bool HavePatch(AbsolutePath baseDir, long srcHash, long destHash)
    {
        var hashName = Hash.FromLong(srcHash).ToHex() + "_" + Hash.FromLong(destHash).ToHex();
        return baseDir.Combine("patches", hashName).FileExists();
    }

    public static async Task GeneratePatch(AbsolutePath workingFolder, Build build, Instruction inst)
    {
        var fromPath = inst.FromFile.ToRelativePath().RelativeTo(build.FromPath);
        var toPath = inst.Path.ToRelativePath().RelativeTo(build.ToPath);
        
        await using var oldData = fromPath.Open(FileMode.Open, FileAccess.Read, FileShare.Read);
        await using var newData = toPath.Open(FileMode.Open, FileAccess.Read, FileShare.Read);

        var guid = ( await fromPath.Hash()).ToHex() + "_" + (await toPath.Hash()).ToHex();
        await using var os = guid.ToRelativePath().RelativeTo(workingFolder.Combine("patches")).Open(FileMode.Create, FileAccess.ReadWrite);
        
        Console.WriteLine($"Diffing: {inst.FromFile} -> {inst.Path}");

        using var sig = new MemoryStream();
        var signatureBuilder = new SignatureBuilder();
        signatureBuilder.Build(oldData, new SignatureWriter(sig));

        sig.Position = 0;
        oldData.Position = 0;

        OctoDiff.Create(oldData, newData, sig, os);
    }
}