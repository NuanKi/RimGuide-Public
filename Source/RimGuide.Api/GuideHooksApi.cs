using UnityEngine;
using Verse;

namespace RimGuide;

/// <summary>
/// Defines an extension interface for third-party modders to provide dynamic values and conditions
/// to RimGuide articles without requiring Harmony patches.
/// </summary>
/// <remarks>
/// Register an implementation using <see cref="GuideApi.RegisterProvider(IGuideValueProvider)"/>.
/// Articles can reference the provider using the <c>provider="..."</c> attribute on <c>&lt;Value&gt;</c>,
/// <c>&lt;If&gt;</c>, or <c>&lt;Unless&gt;</c> tags.
/// </remarks>
/// <example>
/// <code>
/// public sealed class MyModValueProvider : IGuideValueProvider
/// {
///     public string Id => "mymod";
///
///     public bool TryGet(string name, GuideContext? context, out string value)
///     {
///         if (name == "researchProgress") { value = "85%"; return true; }
///         value = ""; return false;
///     }
///
///     public bool TryGetCondition(string name, GuideContext? context, out bool value)
///     {
///         if (name == "hasResearched") { value = true; return true; }
///         value = false; return false;
///     }
/// }
/// </code>
/// </example>
public interface IGuideValueProvider
{
    /// <summary>
    /// Unique identifier of this value provider, matched against the <c>provider</c> attribute in XML tags.
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Attempts to resolve a text value by name for the given context.
    /// </summary>
    /// <param name="name">The variable or property name (e.g. <c>cost</c>, <c>energy</c>).</param>
    /// <param name="context">The active guide context (target Def, source object, or custom objects).</param>
    /// <param name="value">The resolved string representation, or empty string on failure.</param>
    /// <returns><see langword="true"/> if resolved; otherwise <see langword="false"/>.</returns>
    bool TryGet(string name, GuideContext? context, out string value);

    /// <summary>
    /// Attempts to evaluate a boolean condition by name for conditional blocks (<c>&lt;If&gt;</c> and <c>&lt;Unless&gt;</c>).
    /// </summary>
    /// <param name="name">The condition name (e.g. <c>is-active</c>, <c>has-power</c>).</param>
    /// <param name="context">The active guide context.</param>
    /// <param name="value">The resulting boolean evaluation.</param>
    /// <returns><see langword="true"/> if the condition is recognized and evaluated; otherwise <see langword="false"/>.</returns>
    bool TryGetCondition(string name, GuideContext? context, out bool value);

    /// <summary>
    /// Attempts to resolve and format a value using a specified format specifier and format provider.
    /// </summary>
    /// <param name="name">The variable name.</param>
    /// <param name="format">Format specifier (e.g. <c>percent</c>, <c>hours</c>, <c>F1</c>, <c>title</c>).</param>
    /// <param name="formatProvider">Optional culture or format provider.</param>
    /// <param name="context">The active guide context.</param>
    /// <param name="value">The formatted string representation.</param>
    /// <returns><see langword="true"/> if resolved; otherwise <see langword="false"/>.</returns>
    bool TryGetFormatted(string name, string? format, IFormatProvider? formatProvider, GuideContext? context, out string value)
    {
        return TryGet(name, context, out value);
    }
}

/// <summary>
/// Allows third-party modders to draw custom, interactive UI cards or widgets directly inside guide articles
/// using custom XML tags (e.g. <c>&lt;MyCustomCard item="Thing" /&gt;</c>) without Harmony patching.
/// </summary>
/// <remarks>
/// Register an implementation using <see cref="GuideApi.RegisterCardDrawer(IGuideCardDrawer)"/>.
/// Whenever RimGuide encounters an XML block tag matching <see cref="TagName"/>, this drawer is called to measure and paint.
/// </remarks>
/// <example>
/// <code>
/// public sealed class SolarCardDrawer : IGuideCardDrawer
/// {
///     public string TagName => "SolarOutputCard";
///
///     public float Measure(float width, IReadOnlyDictionary&lt;string, string&gt; attributes, GuideContext? context) => 60f;
///
///     public void Draw(Rect rect, IReadOnlyDictionary&lt;string, string&gt; attributes, GuideContext? context, Action&lt;string&gt; navigate)
///     {
///         Widgets.DrawBoxSolid(rect, new Color(0.1f, 0.15f, 0.2f));
///         Widgets.Label(rect, "Current Solar Output: 1700 W");
///     }
/// }
/// </code>
/// </example>
public interface IGuideCardDrawer
{
    /// <summary>
    /// The XML tag name that triggers this card drawer (e.g. <c>PowerGridCard</c>, <c>GeneTree</c>).
    /// </summary>
    string TagName { get; }

    /// <summary>
    /// Measures the vertical height required to render the card given the available container width and attributes.
    /// </summary>
    /// <param name="width">The total horizontal width available for rendering.</param>
    /// <param name="attributes">The XML attributes specified on the card tag.</param>
    /// <param name="context">The active guide context.</param>
    /// <returns>The height in GUI pixels.</returns>
    float Measure(float width, IReadOnlyDictionary<string, string> attributes, GuideContext? context);

    /// <summary>
    /// Paints the card's visual content into the specified rectangle.
    /// </summary>
    /// <param name="rect">The bounding box where the card should draw.</param>
    /// <param name="attributes">The XML attributes specified on the card tag.</param>
    /// <param name="context">The active guide context.</param>
    /// <param name="navigate">Callback to navigate to another article or anchor link (e.g. <c>navigate("guide:OtherArticle")</c>).</param>
    void Draw(Rect rect, IReadOnlyDictionary<string, string> attributes, GuideContext? context, Action<string> navigate);
}

/// <summary>
/// Extension interface allowing modders to customize Def resolution, label formatting, description overrides,
/// counterpart Def matching, and icon extraction for custom Def types or virtual Defs.
/// </summary>
/// <remarks>
/// Register an implementation using <see cref="GuideApi.RegisterDefResolver(IGuideDefResolver)"/>.
/// Resolvers are queried in descending order of <see cref="Priority"/>.
/// </remarks>
public interface IGuideDefResolver
{
    /// <summary>
    /// Resolution priority. Resolvers with higher priority are queried before resolvers with lower priority.
    /// Default is 0.
    /// </summary>
    int Priority => 0;

    /// <summary>
    /// Attempts to resolve a Def instance by type and defName.
    /// </summary>
    /// <param name="typeName">The short or full class name of the Def, or custom semantic type.</param>
    /// <param name="defName">The defName identifier.</param>
    /// <param name="def">The resolved Def if successful.</param>
    /// <returns><see langword="true"/> if handled; otherwise <see langword="false"/> to defer to next resolver.</returns>
    bool TryResolveDef(string typeName, string defName, out Def? def)
    {
        def = null;
        return false;
    }

    /// <summary>
    /// Attempts to enumerate Def instances matching a given type name or custom semantic collection identifier.
    /// Used for dynamic folders (e.g. <c>folder-defs:Example_Buildings</c>) and wildcard DefCard resolution.
    /// </summary>
    /// <param name="typeName">The Def type name or custom category (e.g. <c>ThingDef</c>, <c>Example_Buildings</c>).</param>
    /// <param name="defs">The enumerated Def instances.</param>
    /// <returns><see langword="true"/> if handled; otherwise <see langword="false"/> to defer to subsequent resolvers or default database scanning.</returns>
    bool TryResolveDefs(string typeName, out IEnumerable<Def>? defs)
    {
        defs = null;
        return false;
    }

    /// <summary>
    /// Attempts to find a linked or counterpart Def for a given Def (e.g. PreceptDef for a RitualPatternDef).
    /// </summary>
    /// <param name="def">The subject Def.</param>
    /// <param name="counterpart">The counterpart Def.</param>
    /// <returns><see langword="true"/> if found; otherwise <see langword="false"/>.</returns>
    bool TryFindCounterpart(Def def, out Def? counterpart)
    {
        counterpart = null;
        return false;
    }

    /// <summary>
    /// Attempts to extract or generate a 2D icon texture for a Def.
    /// </summary>
    /// <param name="def">The subject Def.</param>
    /// <param name="icon">The resulting texture.</param>
    /// <returns><see langword="true"/> if resolved; otherwise <see langword="false"/>.</returns>
    bool TryGetIcon(Def def, out Texture2D? icon)
    {
        icon = null;
        return false;
    }

    /// <summary>
    /// Attempts to get a clean display label for a Def.
    /// </summary>
    /// <param name="def">The subject Def.</param>
    /// <param name="label">The human-readable label.</param>
    /// <returns><see langword="true"/> if provided; otherwise <see langword="false"/>.</returns>
    bool TryGetLabel(Def def, out string? label)
    {
        label = null;
        return false;
    }

    /// <summary>
    /// Attempts to get an in-depth description for a Def.
    /// </summary>
    /// <param name="def">The subject Def.</param>
    /// <param name="description">The description string.</param>
    /// <returns><see langword="true"/> if provided; otherwise <see langword="false"/>.</returns>
    bool TryGetDescription(Def def, out string? description)
    {
        description = null;
        return false;
    }
}

/// <summary>
/// Allows modders to register custom named conditions for XML conditional logic (&lt;If&gt;, &lt;Unless&gt;).
/// </summary>
public interface IGuideConditionEvaluator
{
    /// <summary>The unique condition identifier.</summary>
    string Id { get; }
    /// <summary>Evaluates whether the condition is met for the given context.</summary>
    bool Evaluate(GuideContext? context);
}

/// <summary>
/// Interface for receiving lifecycle callbacks when the guide window opens, closes, or navigates.
/// </summary>
/// <remarks>
/// Register an implementation using <see cref="GuideApi.RegisterLifecycleHook(IGuideLifecycleHook)"/>.
/// </remarks>
public interface IGuideLifecycleHook
{
    /// <summary>Invoked immediately after the guide window is opened.</summary>
    /// <param name="articleId">The landing article ID.</param>
    /// <param name="context">The active context.</param>
    void OnGuideOpened(string articleId, GuideContext? context) { }

    /// <summary>Invoked when the user navigates to an article or anchor link.</summary>
    /// <param name="articleId">The destination article ID.</param>
    /// <param name="anchor">The destination section anchor if any.</param>
    /// <param name="context">The active context.</param>
    void OnArticleNavigated(string articleId, string anchor, GuideContext? context) { }

    /// <summary>Invoked when the guide context is modified (e.g. the user changes the inspected item).</summary>
    /// <param name="newContext">The updated context.</param>
    void OnContextChanged(GuideContext? newContext) { }

    /// <summary>Invoked when the guide window is closed.</summary>
    void OnGuideClosed() { }
}
