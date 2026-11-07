using UnityEngine;

namespace RimGuide;

/// <summary>
/// Represents a resolvable texture, image, or graphic asset managed by RimGuide or a registered asset provider.
/// </summary>
/// <param name="Id">Unique asset identifier.</param>
/// <param name="Label">Human-readable asset label.</param>
/// <param name="PackageId">Owning mod package identifier.</param>
/// <param name="LogicalPath">Relative logical path to the asset within the mod directory.</param>
/// <param name="Kind">Asset kind (e.g. <c>Texture</c>, <c>Icon</c>, <c>Illustration</c>).</param>
/// <param name="DefType">Associated Def class name if applicable.</param>
/// <param name="DefName">Associated defName if applicable.</param>
/// <param name="SourceId">Originating provider identifier.</param>
/// <param name="AlreadyLoaded">Whether the texture is already loaded into GPU memory.</param>
public sealed record GuideAsset(
    string Id,
    string Label,
    string PackageId,
    string LogicalPath,
    string Kind = "Texture",
    string DefType = "",
    string DefName = "",
    string SourceId = "",
    bool AlreadyLoaded = false);

/// <summary>
/// Interface allowing mods to provide custom graphic assets or dynamic textures to RimGuide.
/// </summary>
public interface IGuideAssetSource
{
    /// <summary>Unique identifier of this asset source.</summary>
    string Id { get; }
    /// <summary>Enumerates all assets made available by this source.</summary>
    IEnumerable<GuideAsset> Enumerate();
    /// <summary>Attempts to load a 2D texture for the specified asset ID.</summary>
    bool TryLoad(string assetId, out Texture2D? texture);
}

public static partial class GuideApi
{
    private static readonly Dictionary<string, IGuideAssetSource> assetSources = new(StringComparer.Ordinal);

    /// <summary>Gets a snapshot of all currently registered asset sources.</summary>
    public static IReadOnlyCollection<IGuideAssetSource> AssetSources => assetSources.Values;

    /// <summary>Registers a custom asset source with RimGuide.</summary>
    public static void RegisterAssetSource(IGuideAssetSource source)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentException.ThrowIfNullOrWhiteSpace(source.Id);
        lock (assetSources)
        {
            if (!assetSources.TryAdd(source.Id, source))
                throw new InvalidOperationException("Asset source already registered: " + source.Id);
        }
    }

    /// <summary>Attempts to retrieve a registered asset source by ID.</summary>
    public static bool TryAssetSource(string id, out IGuideAssetSource source)
    {
        lock (assetSources) return assetSources.TryGetValue(id, out source!);
    }
}
