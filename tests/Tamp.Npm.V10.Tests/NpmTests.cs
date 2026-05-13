using System.Linq;
using Tamp;
using Tamp.Npm.V10;
using Xunit;

namespace Tamp.Npm.V10.Tests;

public sealed class NpmTests
{
    private static Tool FakeTool() => new(AbsolutePath.Create("/fake/npm"));

    private static int IndexOf(IReadOnlyList<string> args, string token)
    {
        for (var i = 0; i < args.Count; i++) if (args[i] == token) return i;
        return -1;
    }

    // ---- Install ----

    [Fact]
    public void Install_Bare()
    {
        var plan = Npm.Install(FakeTool());
        Assert.Equal("install", plan.Arguments[0]);
        Assert.Contains("--no-fund", plan.Arguments);  // default-on
    }

    [Fact]
    public void Install_Packages_Plus_Flags()
    {
        var plan = Npm.Install(FakeTool(), s => s
            .AddPackage("react")
            .AddPackage("react-dom")
            .SetSaveDev()
            .SetIgnoreScripts()
            .SetPreferOffline());
        Assert.Equal(new[] { "install", "react", "react-dom" }, plan.Arguments.Take(3));
        Assert.Contains("--save-dev", plan.Arguments);
        Assert.Contains("--ignore-scripts", plan.Arguments);
        Assert.Contains("--prefer-offline", plan.Arguments);
    }

    [Fact]
    public void Install_Object_Init()
    {
        var plan = Npm.Install(FakeTool(), new NpmInstallSettings
        {
            SaveDev = true,
            IgnoreScripts = true,
            NoFund = false,
        });
        Assert.Contains("--save-dev", plan.Arguments);
        Assert.Contains("--ignore-scripts", plan.Arguments);
        Assert.DoesNotContain("--no-fund", plan.Arguments);
    }

    // ---- Ci ----

    [Fact]
    public void Ci_Has_Sensible_Defaults()
    {
        var plan = Npm.Ci(FakeTool());
        Assert.Equal("ci", plan.Arguments[0]);
        Assert.Contains("--no-audit", plan.Arguments);  // default-on
        Assert.Contains("--no-fund", plan.Arguments);   // default-on
    }

    [Fact]
    public void Ci_Adds_IgnoreScripts_When_Set()
    {
        var plan = Npm.Ci(FakeTool(), s => s.SetIgnoreScripts());
        Assert.Contains("--ignore-scripts", plan.Arguments);
    }

    // ---- Run ----

    [Fact]
    public void Run_Requires_Script()
    {
        Assert.Throws<InvalidOperationException>(() =>
            Npm.Run(FakeTool(), _ => { }).Arguments.ToList());
    }

    [Fact]
    public void Run_Forwards_Args_After_Double_Dash()
    {
        var plan = Npm.Run(FakeTool(), s => s
            .SetScript("test")
            .AddScriptArgs("--coverage", "--reporter=spec"));
        Assert.Equal(new[] { "run", "test" }, plan.Arguments.Take(2));
        var dashIdx = IndexOf(plan.Arguments, "--");
        Assert.True(dashIdx >= 0, "Expected `--` separator before forwarded args.");
        Assert.Equal("--coverage", plan.Arguments[dashIdx + 1]);
        Assert.Equal("--reporter=spec", plan.Arguments[dashIdx + 2]);
    }

    [Fact]
    public void Run_Silent_Flag()
    {
        var plan = Npm.Run(FakeTool(), s => s.SetScript("build").SetSilent());
        Assert.Contains("--silent", plan.Arguments);
    }

    // ---- Publish ----

    [Fact]
    public void Publish_With_Tag_And_Access()
    {
        var plan = Npm.Publish(FakeTool(), s => s
            .SetTag("next")
            .SetAccess("public"));
        Assert.Equal("publish", plan.Arguments[0]);
        Assert.Equal("next", plan.Arguments[IndexOf(plan.Arguments, "--tag") + 1]);
        Assert.Equal("public", plan.Arguments[IndexOf(plan.Arguments, "--access") + 1]);
    }

    [Fact]
    public void Publish_DryRun()
    {
        var plan = Npm.Publish(FakeTool(), s => s.SetDryRun());
        Assert.Contains("--dry-run", plan.Arguments);
    }

    [Fact]
    public void Publish_Otp_Tracked_As_Secret()
    {
        var otp = new Secret("npm-otp", "123456");
        var plan = Npm.Publish(FakeTool(), s => s.SetOtp(otp));
        Assert.Equal("123456", plan.Arguments[IndexOf(plan.Arguments, "--otp") + 1]);
        Assert.Contains(otp, plan.Secrets);
    }

    // ---- Audit ----

    [Fact]
    public void Audit_Fix_With_Level_And_Json()
    {
        var plan = Npm.Audit(FakeTool(), s => s.SetFix().SetAuditLevel("high").SetJson());
        Assert.Equal("audit", plan.Arguments[0]);
        Assert.Contains("fix", plan.Arguments);
        Assert.Equal("high", plan.Arguments[IndexOf(plan.Arguments, "--audit-level") + 1]);
        Assert.Contains("--json", plan.Arguments);
    }

    // ---- Auth + env ----

    [Fact]
    public void NpmAuthToken_Exports_To_NPM_TOKEN_Env_And_Is_Redactable()
    {
        var tok = new Secret("npm-token", "supersecret");
        var plan = Npm.Install(FakeTool(), s => s.SetNpmAuthToken(tok));
        Assert.Equal("supersecret", plan.Environment["NPM_TOKEN"]);
        Assert.Contains(tok, plan.Secrets);
    }

    [Fact]
    public void WorkingDirectory_Propagates_To_Plan()
    {
        var plan = Npm.Install(FakeTool(), s => s.SetWorkingDirectory("/repo/frontend"));
        Assert.Equal("/repo/frontend", plan.WorkingDirectory);
    }

    // ---- Raw ----

    [Fact]
    public void Raw_Allows_Arbitrary_Verb()
    {
        var plan = Npm.Raw(FakeTool(), "config", "set", "registry", "https://example/npm/");
        Assert.Equal(new[] { "config", "set", "registry", "https://example/npm/" }, plan.Arguments);
    }

    [Fact]
    public void Raw_Rejects_Empty_Args()
    {
        Assert.Throws<ArgumentException>(() => Npm.Raw(FakeTool()));
    }

    [Fact]
    public void Executable_Matches_Tool_Path()
    {
        // AbsolutePath normalization differs by OS — assert basename only.
        var plan = Npm.Install(FakeTool());
        Assert.EndsWith("npm", plan.Executable.TrimEnd(System.IO.Path.DirectorySeparatorChar));
    }
}
