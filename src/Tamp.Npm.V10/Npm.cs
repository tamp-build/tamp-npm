namespace Tamp.Npm.V10;

/// <summary>Top-level facade for <c>npm</c> verbs.</summary>
/// <remarks>
/// <para>Resolve the tool via <c>[FromPath("npm")]</c> — npm is invoked on PATH:</para>
/// <code>
/// [FromPath("npm")] readonly Tool Npm = null!;
/// </code>
/// </remarks>
public static class Npm
{
    /// <summary><c>npm install [package...]</c> — install / sync project dependencies.</summary>
    public static CommandPlan Install(Tool tool, Action<NpmInstallSettings>? configure = null)
        => Build<NpmInstallSettings>(tool, configure);

    /// <summary><c>npm ci</c> — clean install from package-lock.json (the CI-recommended invocation).</summary>
    public static CommandPlan Ci(Tool tool, Action<NpmCiSettings>? configure = null)
        => Build<NpmCiSettings>(tool, configure);

    /// <summary><c>npm run &lt;script&gt;</c> — run an npm script from package.json.</summary>
    public static CommandPlan Run(Tool tool, Action<NpmRunSettings> configure)
        => Build<NpmRunSettings>(tool, configure);

    /// <summary><c>npm publish</c> — publish a tarball or current dir to the registry.</summary>
    public static CommandPlan Publish(Tool tool, Action<NpmPublishSettings>? configure = null)
        => Build<NpmPublishSettings>(tool, configure);

    /// <summary><c>npm audit</c> — security-audit dependencies.</summary>
    public static CommandPlan Audit(Tool tool, Action<NpmAuditSettings>? configure = null)
        => Build<NpmAuditSettings>(tool, configure);

    /// <summary>
    /// Idempotent <c>npm version &lt;ver&gt; --no-git-tag-version --allow-same-version</c>.
    /// Use from a <c>StampVersion</c> target. Safe to invoke repeatedly with the same
    /// version — no-op succeeds (raw <c>npm version</c> exits 1 on no-op which breaks
    /// retry loops).
    /// </summary>
    public static CommandPlan SetVersion(Tool tool, string version)
    {
        if (tool is null) throw new ArgumentNullException(nameof(tool));
        if (string.IsNullOrWhiteSpace(version))
            throw new ArgumentException("Version must be non-empty.", nameof(version));
        var s = new NpmSetVersionSettings { Version = version };
        return s.ToCommandPlan(tool);
    }

    /// <summary>Object-init overload for <see cref="SetVersion(Tool, string)"/>.</summary>
    public static CommandPlan SetVersion(Tool tool, NpmSetVersionSettings settings) => Plan(tool, settings);

    /// <summary>Raw escape hatch for verbs we haven't typed yet.</summary>
    public static CommandPlan Raw(Tool tool, params string[] arguments)
    {
        if (tool is null) throw new ArgumentNullException(nameof(tool));
        if (arguments is null || arguments.Length == 0)
            throw new ArgumentException("Raw requires at least one argument.", nameof(arguments));
        var s = new NpmRawSettings();
        s.AddArgs(arguments);
        return s.ToCommandPlan(tool);
    }

    private static CommandPlan Build<T>(Tool tool, Action<T>? configure) where T : NpmSettingsBase, new()
    {
        if (tool is null) throw new ArgumentNullException(nameof(tool));
        var s = new T();
        configure?.Invoke(s);
        return s.ToCommandPlan(tool);
    }

    // Object-init overloads
    public static CommandPlan Install(Tool tool, NpmInstallSettings settings) => Plan(tool, settings);
    public static CommandPlan Ci(Tool tool, NpmCiSettings settings) => Plan(tool, settings);
    public static CommandPlan Run(Tool tool, NpmRunSettings settings) => Plan(tool, settings);
    public static CommandPlan Publish(Tool tool, NpmPublishSettings settings) => Plan(tool, settings);
    public static CommandPlan Audit(Tool tool, NpmAuditSettings settings) => Plan(tool, settings);

    private static CommandPlan Plan<T>(Tool tool, T settings) where T : NpmSettingsBase
    {
        if (tool is null) throw new ArgumentNullException(nameof(tool));
        if (settings is null) throw new ArgumentNullException(nameof(settings));
        return settings.ToCommandPlan(tool);
    }
}
