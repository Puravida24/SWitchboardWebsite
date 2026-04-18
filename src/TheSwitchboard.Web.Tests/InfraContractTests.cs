namespace TheSwitchboard.Web.Tests;

/// <summary>
/// Slice H-2 Config Hygiene — file-read contract tests.
///
///   A. appsettings.json must NOT contain the hardcoded dev Postgres password.
///   B. .gitignore must exclude .claude/ (Claude Code runtime state shouldn't leak to repo).
///   C. .dockerignore must exist and exclude build artifacts + test projects + VCS.
///   D. Dockerfile must run as a non-root user.
///   E. Dockerfile must define a HEALTHCHECK.
///   F. Dockerfile ENTRYPOINT must not wrap the app in `sh -c` (no shell overhead).
/// </summary>
public class InfraContractTests
{
    private static string RepoRoot()
    {
        var dir = AppContext.BaseDirectory;
        while (dir is not null && !Directory.Exists(Path.Combine(dir, "src")))
            dir = Path.GetDirectoryName(dir);
        Assert.NotNull(dir);
        return dir!;
    }

    private static string Read(string relativeFromRepoRoot) =>
        File.ReadAllText(Path.Combine(RepoRoot(), relativeFromRepoRoot));

    [Fact]
    public void H2_A_AppSettings_HasNoHardcodedDevPassword()
    {
        var appsettings = Read("src/TheSwitchboard.Web/appsettings.json");
        // The dev password was `switchboard_dev`. Committed to a public repo = leak.
        Assert.DoesNotContain("switchboard_dev", appsettings);
        // Also guard against any other hardcoded Password=value pattern.
        Assert.DoesNotMatch(@"Password\s*=\s*[^;""\s][^;""]*", appsettings);
    }

    [Fact]
    public void H2_B_GitIgnore_Excludes_Claude_Dir()
    {
        var gi = Read(".gitignore");
        Assert.Contains(".claude/", gi);
    }

    [Fact]
    public void H2_C_DockerIgnore_Exists_And_Excludes_BuildArtifacts()
    {
        var path = Path.Combine(RepoRoot(), ".dockerignore");
        Assert.True(File.Exists(path), ".dockerignore must exist at repo root");
        var di = File.ReadAllText(path);
        // Must exclude build output, VCS metadata, tests, IDE metadata, and node_modules.
        Assert.Contains("bin/", di);
        Assert.Contains("obj/", di);
        Assert.Contains(".git", di);
        Assert.Contains(".vs", di);
        Assert.Contains("node_modules", di);
        Assert.Contains("TheSwitchboard.Web.Tests", di);
    }

    [Fact]
    public void H2_D_Dockerfile_UsesNonRootUser()
    {
        var df = Read("Dockerfile");
        // aspnet:9.0 base image ships with a pre-created non-root 'app' user.
        // Dockerfile must switch to it (USER app) before the ENTRYPOINT.
        Assert.Contains("USER app", df);
        Assert.DoesNotContain("USER root", df);
    }

    [Fact]
    public void H2_E_Dockerfile_HasHealthcheck()
    {
        var df = Read("Dockerfile");
        Assert.Contains("HEALTHCHECK", df);
    }

    [Fact]
    public void H2_F_Dockerfile_NoShellWrapper()
    {
        var df = Read("Dockerfile");
        // The old ENTRYPOINT was ["sh", "-c", "dotnet ..."] — adds a shell layer
        // for no reason. Entrypoint should be direct exec form: ["dotnet", "App.dll"].
        Assert.DoesNotContain("\"sh\", \"-c\"", df);
    }
}
