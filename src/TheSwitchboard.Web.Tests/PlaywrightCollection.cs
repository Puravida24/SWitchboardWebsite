namespace TheSwitchboard.Web.Tests;

/// <summary>
/// Shared xUnit collection so one `dotnet run` subprocess + one Chromium browser
/// is spun up across every [Collection("Playwright")]-tagged test class, rather
/// than one per class. First test pays the ~10-15s startup; the rest reuse it.
/// </summary>
[CollectionDefinition("Playwright")]
public sealed class PlaywrightCollection : ICollectionFixture<PlaywrightFixture>
{
}
