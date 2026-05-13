namespace Tamp.Npm.V10;

/// <summary>Settings for <c>npm install [package...]</c>.</summary>
public sealed class NpmInstallSettings : NpmSettingsBase
{
    /// <summary>Specific packages to install. Empty = install everything in <c>package.json</c>.</summary>
    public List<string> Packages { get; } = new();

    /// <summary>Install as <c>devDependencies</c>. Maps to <c>--save-dev</c>.</summary>
    public bool SaveDev { get; set; }

    /// <summary>Install globally (<c>--global</c>). Use sparingly; Tamp.NetCli.V10 style local-tool
    /// installs are usually preferable.</summary>
    public bool Global { get; set; }

    /// <summary>Skip running install scripts (<c>--ignore-scripts</c>). Recommended on CI for supply-chain safety.</summary>
    public bool IgnoreScripts { get; set; }

    /// <summary>Skip the audit step (<c>--no-audit</c>). Useful for restore-heavy CI legs that audit separately.</summary>
    public bool NoAudit { get; set; }

    /// <summary>Skip the funding spam (<c>--no-fund</c>). Default true — funding output is noise in CI logs.</summary>
    public bool NoFund { get; set; } = true;

    /// <summary>Treat the lockfile as authoritative (<c>--prefer-offline</c>). Speeds up CI by avoiding registry hits when possible.</summary>
    public bool PreferOffline { get; set; }

    public NpmInstallSettings AddPackage(string package) { Packages.Add(package); return this; }
    public NpmInstallSettings SetSaveDev(bool v = true) { SaveDev = v; return this; }
    public NpmInstallSettings SetGlobal(bool v = true) { Global = v; return this; }
    public NpmInstallSettings SetIgnoreScripts(bool v = true) { IgnoreScripts = v; return this; }
    public NpmInstallSettings SetNoAudit(bool v = true) { NoAudit = v; return this; }
    public NpmInstallSettings SetNoFund(bool v = true) { NoFund = v; return this; }
    public NpmInstallSettings SetPreferOffline(bool v = true) { PreferOffline = v; return this; }

    protected override IEnumerable<string> BuildArguments()
    {
        yield return "install";
        foreach (var p in Packages) yield return p;
        if (SaveDev) yield return "--save-dev";
        if (Global) yield return "--global";
        if (IgnoreScripts) yield return "--ignore-scripts";
        if (NoAudit) yield return "--no-audit";
        if (NoFund) yield return "--no-fund";
        if (PreferOffline) yield return "--prefer-offline";
    }
}

/// <summary>
/// Settings for <c>npm ci</c> — clean install from <c>package-lock.json</c>. The recommended
/// CI invocation: refuses to write to the lockfile and fails fast when it's out of sync.
/// </summary>
public sealed class NpmCiSettings : NpmSettingsBase
{
    /// <summary>Skip install scripts (<c>--ignore-scripts</c>). Recommended for supply-chain safety on CI.</summary>
    public bool IgnoreScripts { get; set; }

    /// <summary>Suppress audit output (<c>--no-audit</c>).</summary>
    public bool NoAudit { get; set; } = true;

    /// <summary>Suppress funding spam (<c>--no-fund</c>).</summary>
    public bool NoFund { get; set; } = true;

    /// <summary>Prefer the cache over registry hits (<c>--prefer-offline</c>).</summary>
    public bool PreferOffline { get; set; }

    public NpmCiSettings SetIgnoreScripts(bool v = true) { IgnoreScripts = v; return this; }
    public NpmCiSettings SetNoAudit(bool v = true) { NoAudit = v; return this; }
    public NpmCiSettings SetNoFund(bool v = true) { NoFund = v; return this; }
    public NpmCiSettings SetPreferOffline(bool v = true) { PreferOffline = v; return this; }

    protected override IEnumerable<string> BuildArguments()
    {
        yield return "ci";
        if (IgnoreScripts) yield return "--ignore-scripts";
        if (NoAudit) yield return "--no-audit";
        if (NoFund) yield return "--no-fund";
        if (PreferOffline) yield return "--prefer-offline";
    }
}

/// <summary>Settings for <c>npm run &lt;script&gt; [-- ...args]</c>.</summary>
public sealed class NpmRunSettings : NpmSettingsBase
{
    /// <summary>The script name as defined in <c>package.json</c>. Required.</summary>
    public string? Script { get; set; }

    /// <summary>Args forwarded to the script after <c>--</c>.</summary>
    public List<string> ScriptArgs { get; } = new();

    /// <summary>Silence npm's pre-/post-script noise (<c>--silent</c>).</summary>
    public bool Silent { get; set; }

    public NpmRunSettings SetScript(string script) { Script = script; return this; }
    public NpmRunSettings AddScriptArg(string arg) { ScriptArgs.Add(arg); return this; }
    public NpmRunSettings AddScriptArgs(params string[] args) { ScriptArgs.AddRange(args); return this; }
    public NpmRunSettings SetSilent(bool v = true) { Silent = v; return this; }

    protected override IEnumerable<string> BuildArguments()
    {
        if (string.IsNullOrEmpty(Script))
            throw new InvalidOperationException("Script is required for npm run (set via SetScript).");
        yield return "run";
        yield return Script!;
        if (Silent) yield return "--silent";
        if (ScriptArgs.Count > 0)
        {
            yield return "--";
            foreach (var a in ScriptArgs) yield return a;
        }
    }
}

/// <summary>Settings for <c>npm publish</c>.</summary>
public sealed class NpmPublishSettings : NpmSettingsBase
{
    /// <summary>Tarball path or directory to publish. When null, uses cwd.</summary>
    public string? Spec { get; set; }

    /// <summary>Dist-tag to publish under. Default <c>latest</c>.</summary>
    public string? Tag { get; set; }

    /// <summary>Access level: <c>public</c> or <c>restricted</c>. Required for new scoped packages.</summary>
    public string? Access { get; set; }

    /// <summary>One-time password from the registry's MFA. Tracked as a Secret so it gets redacted.</summary>
    public Secret? Otp { get; set; }

    /// <summary>Dry run — pack but don't upload (<c>--dry-run</c>).</summary>
    public bool DryRun { get; set; }

    public NpmPublishSettings SetSpec(string? spec) { Spec = spec; return this; }
    public NpmPublishSettings SetTag(string? tag) { Tag = tag; return this; }
    public NpmPublishSettings SetAccess(string? access) { Access = access; return this; }
    public NpmPublishSettings SetOtp(Secret? otp) { Otp = otp; return this; }
    public NpmPublishSettings SetDryRun(bool v = true) { DryRun = v; return this; }

    protected override IEnumerable<string> BuildArguments()
    {
        yield return "publish";
        if (!string.IsNullOrEmpty(Spec)) yield return Spec!;
        if (!string.IsNullOrEmpty(Tag)) { yield return "--tag"; yield return Tag!; }
        if (!string.IsNullOrEmpty(Access)) { yield return "--access"; yield return Access!; }
        if (Otp is { } o) { yield return "--otp"; yield return o.Reveal(); }
        if (DryRun) yield return "--dry-run";
    }

    protected override IEnumerable<Secret> CollectSecrets()
    {
        foreach (var s in base.CollectSecrets()) yield return s;
        if (Otp is not null) yield return Otp;
    }
}

/// <summary>Settings for <c>npm audit</c>.</summary>
public sealed class NpmAuditSettings : NpmSettingsBase
{
    /// <summary>Minimum severity to gate on. Maps to <c>--audit-level=&lt;value&gt;</c>. Values: info, low, moderate, high, critical.</summary>
    public string? AuditLevel { get; set; }

    /// <summary>Emit machine-readable JSON (<c>--json</c>).</summary>
    public bool Json { get; set; }

    /// <summary>Run audit fix (<c>fix</c> subcommand). When false, just reports.</summary>
    public bool Fix { get; set; }

    /// <summary>Force the fix even when it would do a major-version bump (<c>--force</c>). Use carefully.</summary>
    public bool Force { get; set; }

    public NpmAuditSettings SetAuditLevel(string? level) { AuditLevel = level; return this; }
    public NpmAuditSettings SetJson(bool v = true) { Json = v; return this; }
    public NpmAuditSettings SetFix(bool v = true) { Fix = v; return this; }
    public NpmAuditSettings SetForce(bool v = true) { Force = v; return this; }

    protected override IEnumerable<string> BuildArguments()
    {
        yield return "audit";
        if (Fix) yield return "fix";
        if (!string.IsNullOrEmpty(AuditLevel)) { yield return "--audit-level"; yield return AuditLevel!; }
        if (Force) yield return "--force";
        if (Json) yield return "--json";
    }
}

/// <summary>Raw escape hatch — for verbs that don't have typed wrappers yet.</summary>
public sealed class NpmRawSettings : NpmSettingsBase
{
    private readonly List<string> _args = new();
    public void AddArgs(IEnumerable<string> args) => _args.AddRange(args);
    protected override IEnumerable<string> BuildArguments() => _args;
}
