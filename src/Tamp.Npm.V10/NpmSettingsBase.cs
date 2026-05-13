namespace Tamp.Npm.V10;

/// <summary>
/// Common knobs shared by every <c>npm</c> verb's settings class. Mirrors
/// <c>Tamp.Yarn.V4.YarnSettingsBase</c> in shape so adopters who already use
/// Yarn can read the Npm wrapper at a glance.
/// </summary>
/// <remarks>
/// <para>
/// Auth design: npm reads <c>NPM_TOKEN</c> / <c>NPM_CONFIG_TOKEN</c> from the
/// environment when present and overlays <c>.npmrc</c> from cwd / home. Tamp passes
/// the token via <see cref="NpmAuthToken"/>; the spawned process picks it up;
/// the runner's redaction table covers it.
/// </para>
/// <para>
/// Windows shim note: npm ships as <c>npm.cmd</c> on Windows. The Tamp <c>Tool</c>
/// resolution via <c>[FromPath("npm")]</c> finds the <c>.cmd</c> shim correctly.
/// If you hit npm's self-resolution bug ("Cannot find module 'npm-cli.js'" pointing
/// at <c>CWD/node_modules/npm/bin/</c>), it's because the shim was invoked via raw
/// <c>ProcessStartInfo</c> with <c>UseShellExecute=false</c> — outside Tamp's normal
/// path. The wrapper here uses Tamp's standard <c>CommandPlan</c> which works.
/// </para>
/// </remarks>
public abstract class NpmSettingsBase
{
    /// <summary>Working directory of the spawned <c>npm</c> process. Typically the workspace root.</summary>
    public string? WorkingDirectory { get; set; }

    /// <summary>Per-invocation environment variables on top of the inherited environment.</summary>
    public Dictionary<string, string> EnvironmentVariables { get; } = new();

    /// <summary>
    /// npm-registry auth token. Set as <c>NPM_TOKEN</c> on the spawned process (the
    /// var the npm-CLI auth code reads) and registered with the runner's redaction
    /// table. For private-registry consumers, this is the canonical injection point.
    /// </summary>
    public Secret? NpmAuthToken { get; set; }

    /// <summary>Subclasses build the per-verb argument list. The verb token(s) come first.</summary>
    protected abstract IEnumerable<string> BuildArguments();

    /// <summary>
    /// Subclasses extend the secret list. Default yields just <see cref="NpmAuthToken"/>
    /// when set; override to add more (e.g. <c>npm publish</c> with an OTP).
    /// </summary>
    protected virtual IEnumerable<Secret> CollectSecrets()
    {
        if (NpmAuthToken is not null) yield return NpmAuthToken;
    }

    internal CommandPlan ToCommandPlan(Tool tool)
    {
        var env = new Dictionary<string, string>(EnvironmentVariables);
        if (NpmAuthToken is { } t) env["NPM_TOKEN"] = t.Reveal();

        return new CommandPlan
        {
            Executable = tool.Executable.Value,
            Arguments = BuildArguments().ToList(),
            Environment = env,
            WorkingDirectory = WorkingDirectory ?? tool.WorkingDirectory,
            Secrets = CollectSecrets().ToList(),
        };
    }
}

/// <summary>Fluent setters for the common knobs.</summary>
public static class NpmSettingsBaseExtensions
{
    public static T SetWorkingDirectory<T>(this T s, string? cwd) where T : NpmSettingsBase { s.WorkingDirectory = cwd; return s; }
    public static T SetEnvironmentVariable<T>(this T s, string name, string value) where T : NpmSettingsBase { s.EnvironmentVariables[name] = value; return s; }
    public static T SetNpmAuthToken<T>(this T s, Secret token) where T : NpmSettingsBase { s.NpmAuthToken = token; return s; }
}
