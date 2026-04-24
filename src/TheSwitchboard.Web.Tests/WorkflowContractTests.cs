using System.Text.RegularExpressions;

namespace TheSwitchboard.Web.Tests;

/// <summary>
/// Contract: GitHub Actions workflow files pin action versions that we know
/// work on the currently-supported GitHub runner. When an action is deprecated
/// upstream (old Node runtime, old artifact protocol), CI starts failing for
/// reasons unrelated to our code. This test locks the minimum-viable versions
/// so drift is caught pre-push instead of surfacing as a red email.
/// </summary>
public class WorkflowContractTests
{
    private static string GetSolutionRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "TheSwitchboardWeb.sln")))
            dir = dir.Parent;
        if (dir == null) throw new InvalidOperationException("solution root not found");
        return dir.FullName;
    }

    private static string Load(string relative)
    {
        var path = Path.Combine(GetSolutionRoot(), relative);
        Assert.True(File.Exists(path), $"workflow file not found: {path}");
        return File.ReadAllText(path);
    }

    // v0.12.0 ships with Node 20 (deprecated) + actions/upload-artifact@v3
    // (decommissioned — produces "artifact name not valid" failures). v0.15.0
    // (published 2025-10-24) moves to Node 24 + upload-artifact@v4 and fixes
    // the /zap/wrk/zap.yaml permission issue.
    private const int ZapMinMinor = 15;

    [Fact]
    public void Zap_Baseline_Action_IsAtOrAboveMinimumVersion()
    {
        var yaml = Load(".github/workflows/zap-baseline.yml");
        var m = Regex.Match(yaml, @"zaproxy/action-baseline@v(\d+)\.(\d+)\.(\d+)");
        Assert.True(m.Success, "expected zaproxy/action-baseline@vX.Y.Z in zap-baseline.yml");
        var major = int.Parse(m.Groups[1].Value);
        var minor = int.Parse(m.Groups[2].Value);
        Assert.True(
            major > 0 || minor >= ZapMinMinor,
            $"zaproxy/action-baseline is pinned to v{major}.{minor}.{m.Groups[3].Value}. " +
            $"Minimum viable is v0.{ZapMinMinor}.0 (Node 24 + upload-artifact@v4). " +
            "Older versions fail on GitHub's deprecated-artifact-protocol enforcement.");
    }
}
