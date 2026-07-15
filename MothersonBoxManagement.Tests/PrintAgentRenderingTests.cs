using MothersonBoxManagement.PrintAgent.Core;

namespace MothersonBoxManagement.Tests;

public class PrintAgentRenderingTests
{
    [Fact]
    public void ZplLabel_IsFixedAtEightHundredDots_AndContainsQrPayload()
    {
        var payload = new AgentPrintLabelPayload(1, "BOX-20260713", "BOX-20260713", 800, 800, 203, 1);
        var zpl = ZplLabelBuilder.Build(payload);

        Assert.StartsWith("^XA", zpl);
        Assert.Contains("^PW800", zpl);
        Assert.Contains("^LL800", zpl);
        Assert.Contains("^BQN", zpl);
        Assert.Contains("BOX-20260713", zpl);
        Assert.EndsWith("^XZ", zpl);
    }

    [Fact]
    public void ZplLabel_EscapesControlCharacters()
    {
        var payload = new AgentPrintLabelPayload(1, "BOX^~\\01", "BOX^~\\01", 800, 800, 203, 1);
        var zpl = ZplLabelBuilder.Build(payload);

        Assert.DoesNotContain("BOX^~\\01", zpl);
        Assert.Contains("BOX\\5E\\7E\\5C01", zpl);
    }

    [Theory]
    [InlineData(0, 2)]
    [InlineData(1, 4)]
    [InlineData(2, 8)]
    [InlineData(8, 30)]
    public void Backoff_IsBounded(int failures, int expectedSeconds)
    {
        Assert.Equal(TimeSpan.FromSeconds(expectedSeconds), AgentBackoff.ForFailureCount(failures));
    }

    [Fact]
    public void InvalidLabelDimensions_AreRejected()
    {
        var payload = new AgentPrintLabelPayload(1, "BOX-1", "BOX-1", 801, 800, 203, 1);
        Assert.Throws<InvalidOperationException>(() => ZplLabelBuilder.Build(payload));
    }

    [Fact]
    public void ConfigurationDialog_ReservesEnoughHeightForItsPairingAction()
    {
        Assert.True(ConfigurationDialogLayout.MinimumWindowHeight > 250);
        Assert.True(ConfigurationDialogLayout.MinimumWindowHeight >= ConfigurationDialogLayout.RequiredWindowHeight);
    }

    [Fact]
    public void AgentVersion_IsBoundedBeforeItIsSentToThePairingApi()
    {
        var buildVersion = "1.0.0+3a89de8d8713694f22414c49b0e54f2d81019f13";

        var version = AgentVersionFormatter.ForApi(buildVersion);

        Assert.Equal(40, version.Length);
        Assert.StartsWith("1.0.0+", version);
    }
}
