using UnityEngine;
using Verse;

namespace RimGuide;

/// <summary>
/// Uniquely identifies an interface control within a package and surface.
/// </summary>
/// <param name="PackageId">Package identifier of the owning mod.</param>
/// <param name="SurfaceId">Surface identifier (e.g. window name, inspector tab, dialog ID).</param>
/// <param name="ControlId">Specific control identifier within the surface.</param>
public readonly record struct GuideIdentity(string PackageId, string SurfaceId, string ControlId);

/// <summary>
/// Encapsulates context data handed to an article when opened from an in-game interface or control.
/// </summary>
/// <remarks>
/// <para><c>target</c> is the subject of the help (usually a <see cref="Def"/>): <c>&lt;Value name="label" /&gt;</c> reads from it.</para>
/// <para><c>source</c> is the raw object the hosting mod was drawing (for example a settings row); <c>&lt;Value name="source:Member" /&gt;</c> reads from it.</para>
/// <para>Additional arbitrary objects (pawns, buildings, recipes) can be attached and retrieved using <see cref="TryGetObject{T}"/>.</para>
/// </remarks>
public sealed class GuideContext
{
    private readonly System.WeakReference<object>? target;
    private readonly System.WeakReference<object>? source;
    private readonly Dictionary<string, System.WeakReference<object>> customObjects;

    /// <summary>
    /// Explicit string values accessible by name via <c>&lt;Value name="key" /&gt;</c>.
    /// </summary>
    public IReadOnlyDictionary<string, string> Values { get; }

    /// <summary>
    /// Initializes a new instance of <see cref="GuideContext"/>.
    /// </summary>
    /// <param name="values">Optional key-value string dictionary.</param>
    /// <param name="target">Optional primary target object (e.g. a <see cref="Def"/>).</param>
    /// <param name="source">Optional source object being drawn (e.g. a settings row or controller).</param>
    /// <param name="customObjects">Optional mapping of named custom context objects.</param>
    public GuideContext(
        IReadOnlyDictionary<string, string>? values = null,
        object? target = null,
        object? source = null,
        IReadOnlyDictionary<string, object>? customObjects = null)
    {
        Values = values == null ? new Dictionary<string, string>() : new Dictionary<string, string>(values);
        if (target != null) this.target = new(target);
        if (source != null) this.source = new(source);

        this.customObjects = new(StringComparer.OrdinalIgnoreCase);
        if (customObjects != null)
        {
            foreach (var kvp in customObjects)
            {
                if (kvp.Value != null) this.customObjects[kvp.Key] = new(kvp.Value);
            }
        }
    }

    /// <summary>Attempts to retrieve the primary target object if it is still alive in memory.</summary>
    /// <param name="value">The target object if available.</param>
    /// <returns><see langword="true"/> if alive; otherwise <see langword="false"/>.</returns>
    public bool TryGetTarget(out object? value)
    {
        value = null;
        return target?.TryGetTarget(out value) == true;
    }

    /// <summary>Attempts to retrieve the primary target object cast to type <typeparamref name="T"/>.</summary>
    /// <typeparam name="T">The expected type of the target.</typeparam>
    /// <param name="value">The cast target object if available.</param>
    /// <returns><see langword="true"/> if alive and castable; otherwise <see langword="false"/>.</returns>
    public bool TryGetTarget<T>(out T? value) where T : class
    {
        value = null;
        if (target != null && target.TryGetTarget(out var obj) && obj is T typed)
        {
            value = typed;
            return true;
        }
        return false;
    }

    /// <summary>Attempts to retrieve the source object if it is still alive in memory.</summary>
    /// <param name="value">The source object if available.</param>
    /// <returns><see langword="true"/> if alive; otherwise <see langword="false"/>.</returns>
    public bool TryGetSource(out object? value)
    {
        value = null;
        return source?.TryGetTarget(out value) == true;
    }

    /// <summary>Attempts to retrieve the source object cast to type <typeparamref name="T"/>.</summary>
    /// <typeparam name="T">The expected type of the source.</typeparam>
    /// <param name="value">The cast source object if available.</param>
    /// <returns><see langword="true"/> if alive and castable; otherwise <see langword="false"/>.</returns>
    public bool TryGetSource<T>(out T? value) where T : class
    {
        value = null;
        if (source != null && source.TryGetTarget(out var obj) && obj is T typed)
        {
            value = typed;
            return true;
        }
        return false;
    }

    /// <summary>Attempts to retrieve a named custom context object.</summary>
    /// <param name="key">The identifier (e.g. <c>pawn</c>, <c>building</c>, <c>recipe</c>).</param>
    /// <param name="value">The object instance if alive.</param>
    /// <returns><see langword="true"/> if found and alive; otherwise <see langword="false"/>.</returns>
    public bool TryGetObject(string key, out object? value)
    {
        value = null;
        return customObjects.TryGetValue(key, out var weak) && weak.TryGetTarget(out value);
    }

    /// <summary>Attempts to retrieve a named custom context object cast to type <typeparamref name="T"/>.</summary>
    /// <typeparam name="T">The expected object type.</typeparam>
    /// <param name="key">The identifier.</param>
    /// <param name="value">The cast instance if alive.</param>
    /// <returns><see langword="true"/> if found, alive, and castable; otherwise <see langword="false"/>.</returns>
    public bool TryGetObject<T>(string key, out T? value) where T : class
    {
        value = null;
        if (customObjects.TryGetValue(key, out var weak) && weak.TryGetTarget(out var obj) && obj is T typed)
        {
            value = typed;
            return true;
        }
        return false;
    }

    /// <summary>Returns a new <see cref="GuideContext"/> with the specified target attached.</summary>
    public GuideContext WithTarget(object newTarget)
    {
        var dict = new Dictionary<string, object>();
        foreach (var kvp in customObjects) if (kvp.Value.TryGetTarget(out var obj)) dict[kvp.Key] = obj;
        TryGetSource(out var src);
        return new GuideContext(Values, newTarget, src, dict);
    }

    /// <summary>Returns a new <see cref="GuideContext"/> with the specified source attached.</summary>
    public GuideContext WithSource(object newSource)
    {
        var dict = new Dictionary<string, object>();
        foreach (var kvp in customObjects) if (kvp.Value.TryGetTarget(out var obj)) dict[kvp.Key] = obj;
        TryGetTarget(out var tgt);
        return new GuideContext(Values, tgt, newSource, dict);
    }

    /// <summary>Returns a new <see cref="GuideContext"/> with an additional named custom object attached.</summary>
    public GuideContext WithObject(string key, object customObject)
    {
        var dict = new Dictionary<string, object>();
        foreach (var kvp in customObjects) if (kvp.Value.TryGetTarget(out var obj)) dict[kvp.Key] = obj;
        dict[key] = customObject;
        TryGetTarget(out var tgt);
        TryGetSource(out var src);
        return new GuideContext(Values, tgt, src, dict);
    }

    /// <summary>Returns a new <see cref="GuideContext"/> with an additional string value attached.</summary>
    public GuideContext WithValue(string key, string value)
    {
        var dict = new Dictionary<string, string>(Values) { [key] = value };
        var objDict = new Dictionary<string, object>();
        foreach (var kvp in customObjects) if (kvp.Value.TryGetTarget(out var obj)) objDict[kvp.Key] = obj;
        TryGetTarget(out var tgt);
        TryGetSource(out var src);
        return new GuideContext(dict, tgt, src, objDict);
    }
}

/// <summary>
/// Low-level host service provided by the active RimGuide runtime.
/// </summary>
public interface IGuideHost
{
    /// <summary>Handles user interaction (such as Ctrl+Click) over a specified bounding box.</summary>
    bool Handle(Rect rect, GuideIdentity identity, GuideContext? context);
    /// <summary>Draws a visual hint indicator over a bound control when modifier keys are pressed.</summary>
    void DrawHint(Rect rect, GuideIdentity identity);
    /// <summary>Opens an article and scrolls to an anchor.</summary>
    bool Open(string article, string anchor, GuideContext? context);
    /// <summary>Invoked at the beginning of an input event frame.</summary>
    void BeginEvent();
}

/// <summary>
/// Public RimGuide API version 1.
/// Provides thread-safe hook registrations, contextual window launchers, and UI widget helpers for modders.
/// Zero Harmony patches or transpilers are required to extend RimGuide.
/// </summary>
public static partial class GuideApi
{
    /// <summary>Major API version specification.</summary>
    public const int ApiVersion = 1;

    /// <summary>Default provider identifier used when no <c>provider</c> attribute is specified on XML tags.</summary>
    public const string DefaultProvider = "rimguide";

    /// <summary>Gets or sets the active runtime host instance.</summary>
    public static IGuideHost? Host { get; set; }

    private static (string Package, string Surface) scope;
    private static readonly Dictionary<string, IGuideValueProvider> providers = new(StringComparer.Ordinal);
    private static readonly Dictionary<string, IGuideCardDrawer> cardDrawers = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, IGuideConditionEvaluator> conditionEvaluators = new(StringComparer.Ordinal);
    private static readonly List<IGuideDefResolver> defResolvers = [];
    private static readonly Dictionary<string, Func<IEnumerable<Def>>> defSources = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<(Type Type, string Member), Func<object, object?>> readAccessors = [];
    private static long contentRevision;
    /// <summary>Changes when extensions or their data invalidate rendered content.</summary>
    public static long ContentRevision => Interlocked.Read(ref contentRevision);
    /// <summary>Call after changing the data supplied by an extension.</summary>
    public static void InvalidateContent() => Interlocked.Increment(ref contentRevision);
    /// <summary>Approves a computed, read-only member for use by help content. Never register actions here.</summary>
    public static void RegisterReadAccessor(Type type, string member, Func<object, object?> read)
    {
        ArgumentNullException.ThrowIfNull(type);
        ArgumentException.ThrowIfNullOrWhiteSpace(member);
        ArgumentNullException.ThrowIfNull(read);
        lock (readAccessors) readAccessors[(type, member.ToLowerInvariant())] = read;
        InvalidateContent();
    }
    public static bool TryReadAccessor(object target, string member, out object? value)
    {
        Func<object, object?>? read = null;
        lock (readAccessors)
            for (var type = target.GetType(); type != null; type = type.BaseType)
                if (readAccessors.TryGetValue((type, member.ToLowerInvariant()), out read)) break;
        value = read?.Invoke(target);
        return read != null;
    }
    private static readonly List<IGuideLifecycleHook> lifecycleHooks = [];

    /// <summary>Indicates whether RimGuide is loaded and its host runtime is active.</summary>
    public static bool Available => Host != null;

    /// <summary>Fired immediately after a guide window opens.</summary>
    public static event Action<string, GuideContext?>? GuideOpened;

    /// <summary>Fired when an article is navigated to within the guide window.</summary>
    public static event Action<string, string, GuideContext?>? ArticleNavigated;

    /// <summary>Fired when the active guide context is changed (e.g. via Def switcher dropdown).</summary>
    public static event Action<GuideContext?>? ContextChanged;

    /// <summary>Fired when the guide window is closed.</summary>
    public static event Action? GuideClosed;

    /// <summary>
    /// Begins a scoped package and surface context. Any subsequent calls to <see cref="Handle"/> or widget wrappers
    /// will automatically attribute controls to this package and surface.
    /// </summary>
    /// <param name="packageId">The mod's package id (e.g. <c>myauthor.mymod</c>).</param>
    /// <param name="surfaceId">The surface or window name (e.g. <c>ModSettings</c>).</param>
    /// <returns>An <see cref="IDisposable"/> scope that restores the previous scope when disposed.</returns>
    /// <example>
    /// <code>
    /// using (GuideApi.BeginScope("myauthor.mymod", "SettingsWindow"))
    /// {
    ///     GuideApi.Button(buttonRect, "Save", "SaveButton");
    /// }
    /// </code>
    /// </example>
    public static IDisposable BeginScope(string packageId, string surfaceId)
    {
        var previous = scope;
        scope = (packageId, surfaceId);
        return new Scope(() => scope = previous);
    }

    private sealed class Scope(Action restore) : IDisposable
    {
        private Action? restore = restore;
        public void Dispose() { Interlocked.Exchange(ref restore, null)?.Invoke(); }
    }

    /// <summary>Handles user interaction (such as Ctrl+Click) on a given control rectangle.</summary>
    public static bool Handle(Rect rect, string controlId, GuideContext? context = null) =>
        Host?.Handle(rect, new(scope.Package ?? "", scope.Surface ?? "", controlId), context) == true;

    /// <summary>Draws a visual hint indicator on a control if modifier keys are active.</summary>
    public static void DrawHint(Rect rect, string controlId) =>
        Host?.DrawHint(rect, new(scope.Package ?? "", scope.Surface ?? "", controlId));

    /// <summary>Programmatically opens an article in the guide window.</summary>
    /// <param name="article">The article identifier to open.</param>
    /// <param name="anchor">Optional heading anchor id to scroll to.</param>
    /// <param name="context">Optional context to pass to the article.</param>
    /// <returns><see langword="true"/> if the article exists and was opened; otherwise <see langword="false"/>.</returns>
    public static bool Open(string article, string anchor = "", GuideContext? context = null) =>
        Host?.Open(article, anchor, context) == true;

    /// <summary>Signals the start of an input event frame.</summary>
    public static void BeginEvent() => Host?.BeginEvent();

    #region Provider Registration

    /// <summary>Registers a custom dynamic value provider.</summary>
    public static void RegisterProvider(IGuideValueProvider provider)
    {
        ArgumentNullException.ThrowIfNull(provider);
        ArgumentException.ThrowIfNullOrWhiteSpace(provider.Id);
        lock (providers)
        {
            if (!providers.TryAdd(provider.Id, provider))
                throw new InvalidOperationException("Provider already registered: " + provider.Id);
            InvalidateContent();
        }
    }

    /// <summary>Unregisters a value provider by ID.</summary>
    public static bool UnregisterProvider(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) return false;
        lock (providers) { bool removed = providers.Remove(id); if (removed) InvalidateContent(); return removed; }
    }

    /// <summary>Attempts to retrieve a registered value provider by ID.</summary>
    public static bool TryProvider(string id, out IGuideValueProvider provider)
    {
        lock (providers) return providers.TryGetValue(id, out provider!);
    }

    #endregion

    #region Card Drawer Registration

    /// <summary>
    /// Registers a custom card drawer to handle custom XML tags in guide articles.
    /// </summary>
    /// <param name="drawer">The custom card drawer implementation.</param>
    public static void RegisterCardDrawer(IGuideCardDrawer drawer)
    {
        ArgumentNullException.ThrowIfNull(drawer);
        ArgumentException.ThrowIfNullOrWhiteSpace(drawer.TagName);
        lock (cardDrawers)
        {
            cardDrawers[drawer.TagName] = drawer;
            InvalidateContent();
        }
    }

    /// <summary>Unregisters a custom card drawer by tag name.</summary>
    public static bool UnregisterCardDrawer(string tagName)
    {
        if (string.IsNullOrWhiteSpace(tagName)) return false;
        lock (cardDrawers) { bool removed = cardDrawers.Remove(tagName); if (removed) InvalidateContent(); return removed; }
    }

    /// <summary>Attempts to retrieve a registered card drawer for a tag name.</summary>
    public static bool TryCardDrawer(string tagName, out IGuideCardDrawer drawer)
    {
        lock (cardDrawers) return cardDrawers.TryGetValue(tagName, out drawer!);
    }

    #endregion

    #region Condition Evaluator Registration

    /// <summary>Registers a custom condition evaluator for XML &lt;If&gt; and &lt;Unless&gt; tags.</summary>
    public static void RegisterConditionEvaluator(IGuideConditionEvaluator evaluator)
    {
        ArgumentNullException.ThrowIfNull(evaluator);
        ArgumentException.ThrowIfNullOrWhiteSpace(evaluator.Id);
        lock (conditionEvaluators)
        {
            conditionEvaluators[evaluator.Id] = evaluator;
            InvalidateContent();
        }
    }

    /// <summary>Unregisters a condition evaluator by ID.</summary>
    public static bool UnregisterConditionEvaluator(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) return false;
        lock (conditionEvaluators) { bool removed = conditionEvaluators.Remove(id); if (removed) InvalidateContent(); return removed; }
    }

    /// <summary>Attempts to retrieve a registered condition evaluator by ID.</summary>
    public static bool TryConditionEvaluator(string id, out IGuideConditionEvaluator evaluator)
    {
        lock (conditionEvaluators) return conditionEvaluators.TryGetValue(id, out evaluator!);
    }

    #endregion

    #region Def Resolver Registration

    /// <summary>Registers a Def resolver for custom Def types, icons, labels, and descriptions.</summary>
    public static void RegisterDefResolver(IGuideDefResolver resolver)
    {
        ArgumentNullException.ThrowIfNull(resolver);
        lock (defResolvers)
        {
            if (!defResolvers.Contains(resolver))
            {
                defResolvers.Add(resolver);
                defResolvers.Sort((a, b) => b.Priority.CompareTo(a.Priority));
                InvalidateContent();
            }
        }
    }

    /// <summary>Unregisters a previously registered Def resolver.</summary>
    public static bool UnregisterDefResolver(IGuideDefResolver resolver)
    {
        lock (defResolvers) { bool removed = defResolvers.Remove(resolver); if (removed) InvalidateContent(); return removed; }
    }

    /// <summary>Returns a snapshot of all active Def resolvers ordered by priority.</summary>
    public static IReadOnlyList<IGuideDefResolver> DefResolvers
    {
        get
        {
            lock (defResolvers) return defResolvers.ToArray();
        }
    }

    /// <summary>
    /// Registers a custom Def collection provider for dynamic folders and wildcard DefCards.
    /// </summary>
    /// <param name="typeName">Custom type name or semantic identifier (e.g. <c>Example_Buildings</c>).</param>
    /// <param name="provider">Callback enumerating matching Def instances.</param>
    public static void RegisterDefSource(string typeName, Func<IEnumerable<Def>> provider)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(typeName);
        ArgumentNullException.ThrowIfNull(provider);
        lock (defSources)
        {
            defSources[typeName] = provider;
            InvalidateContent();
        }
    }

    /// <summary>Unregisters a previously registered custom Def collection provider.</summary>
    public static bool UnregisterDefSource(string typeName)
    {
        if (string.IsNullOrWhiteSpace(typeName)) return false;
        lock (defSources) { bool removed = defSources.Remove(typeName); if (removed) InvalidateContent(); return removed; }
    }

    /// <summary>Attempts to resolve Def instances from registered def sources.</summary>
    public static bool TryResolveDefSource(string typeName, out IEnumerable<Def>? defs)
    {
        Func<IEnumerable<Def>>? func;
        lock (defSources) defSources.TryGetValue(typeName, out func);
        if (func != null)
        {
            try
            {
                defs = MaterializeDefs(func());
                return true;
            }
            catch (Exception ex) { Log.WarningOnce($"[RimGuide] Error enumerating def source '{typeName}': {ex.Message}", ("RG.source:" + typeName).GetHashCode()); }
        }
        defs = null;
        return false;
    }

    /// <summary>Enumerates inside the caller's exception boundary; rejects unbounded collections.</summary>
    public static Def[] MaterializeDefs(IEnumerable<Def>? source)
    {
        if (source == null) return [];
        var result = source.Take(100001).ToArray();
        if (result.Length > 100000) throw new InvalidOperationException("A Def source exceeds 100000 entries.");
        return result.Where(x => x != null).Distinct().ToArray();
    }

    /// <summary>Returns all registered custom def source type names.</summary>
    public static IReadOnlyList<string> CustomDefTypeNames
    {
        get
        {
            lock (defSources) return defSources.Keys.ToArray();
        }
    }

    #endregion

    #region Lifecycle Hooks

    /// <summary>Registers a lifecycle hook to receive guide open, close, and navigation callbacks.</summary>
    public static void RegisterLifecycleHook(IGuideLifecycleHook hook)
    {
        ArgumentNullException.ThrowIfNull(hook);
        lock (lifecycleHooks)
        {
            if (!lifecycleHooks.Contains(hook)) lifecycleHooks.Add(hook);
        }
    }

    /// <summary>Unregisters a lifecycle hook.</summary>
    public static bool UnregisterLifecycleHook(IGuideLifecycleHook hook)
    {
        lock (lifecycleHooks) return lifecycleHooks.Remove(hook);
    }

    /// <summary>Fires the <see cref="GuideOpened"/> event and notifies all registered lifecycle hooks.</summary>
    public static void RaiseGuideOpened(string articleId, GuideContext? context)
    {
        GuideOpened?.Invoke(articleId, context);
        IGuideLifecycleHook[] hooks;
        lock (lifecycleHooks) hooks = lifecycleHooks.ToArray();
        foreach (var h in hooks)
        {
            try { h.OnGuideOpened(articleId, context); }
            catch (Exception ex) { Log.Warning($"[RimGuide] Error in lifecycle hook OnGuideOpened: {ex.Message}"); }
        }
    }

    /// <summary>Fires the <see cref="ArticleNavigated"/> event and notifies all registered lifecycle hooks.</summary>
    public static void RaiseArticleNavigated(string articleId, string anchor, GuideContext? context)
    {
        ArticleNavigated?.Invoke(articleId, anchor, context);
        IGuideLifecycleHook[] hooks;
        lock (lifecycleHooks) hooks = lifecycleHooks.ToArray();
        foreach (var h in hooks)
        {
            try { h.OnArticleNavigated(articleId, anchor, context); }
            catch (Exception ex) { Log.Warning($"[RimGuide] Error in lifecycle hook OnArticleNavigated: {ex.Message}"); }
        }
    }

    /// <summary>Fires the <see cref="ContextChanged"/> event and notifies all registered lifecycle hooks.</summary>
    public static void RaiseContextChanged(GuideContext? newContext)
    {
        ContextChanged?.Invoke(newContext);
        IGuideLifecycleHook[] hooks;
        lock (lifecycleHooks) hooks = lifecycleHooks.ToArray();
        foreach (var h in hooks)
        {
            try { h.OnContextChanged(newContext); }
            catch (Exception ex) { Log.Warning($"[RimGuide] Error in lifecycle hook OnContextChanged: {ex.Message}"); }
        }
    }

    /// <summary>Fires the <see cref="GuideClosed"/> event and notifies all registered lifecycle hooks.</summary>
    public static void RaiseGuideClosed()
    {
        GuideClosed?.Invoke();
        IGuideLifecycleHook[] hooks;
        lock (lifecycleHooks) hooks = lifecycleHooks.ToArray();
        foreach (var h in hooks)
        {
            try { h.OnGuideClosed(); }
            catch (Exception ex) { Log.Warning($"[RimGuide] Error in lifecycle hook OnGuideClosed: {ex.Message}"); }
        }
    }

    #endregion

    #region Widget Wrappers

    /// <summary>Draws a standard button wrapped with RimGuide hint and click detection.</summary>
    public static bool Button(Rect rect, string text, string controlId, GuideContext? context = null)
    {
        bool owned = Handle(rect, controlId, context);
        bool clicked = Widgets.ButtonText(rect, text);
        DrawHint(rect, controlId);
        return clicked && !owned;
    }

    /// <summary>Draws a standard label wrapped with RimGuide hint and click detection.</summary>
    public static void Label(Rect rect, string text, string controlId, GuideContext? context = null)
    {
        Handle(rect, controlId, context);
        Widgets.Label(rect, text);
        DrawHint(rect, controlId);
    }

    /// <summary>Draws a text field wrapped with RimGuide hint and click detection.</summary>
    public static string TextField(Rect rect, string value, string controlId, GuideContext? context = null)
    {
        bool owned = Handle(rect, controlId, context);
        string edited = Widgets.TextField(rect, value);
        DrawHint(rect, controlId);
        return owned ? value : edited;
    }

    /// <summary>Draws a checkbox labeled wrapped with RimGuide hint and click detection.</summary>
    public static void Checkbox(Rect rect, string label, ref bool value, string controlId, GuideContext? context = null)
    {
        bool owned = Handle(rect, controlId, context), old = value;
        Widgets.CheckboxLabeled(rect, label, ref value);
        if (owned) value = old;
        DrawHint(rect, controlId);
    }

    #endregion
}
