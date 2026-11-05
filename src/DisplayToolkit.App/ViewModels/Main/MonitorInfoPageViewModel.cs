using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DisplayToolkit.Core;
using DisplayToolkit.Core.Features;

namespace DisplayToolkit.App.ViewModels.Main;

/// <summary>One line of the feature table.</summary>
public sealed record FeatureRow(string Name, string Code, string Value);

/// <summary>Everything Display Toolkit knows about the monitor, for curiosity and bug reports.</summary>
public sealed partial class MonitorInfoPageViewModel(MonitorViewModel monitor) : MainPageViewModel("Monitor information")
{
    public MonitorViewModel Monitor { get; } = monitor;

    public string Model => Monitor.Session.Id.Model;

    public string DevicePath => Monitor.Session.Id.Instance;

    public string Link => Monitor.Link?.ToString() ?? "Unknown";

    public string MccsVersion => Monitor.Session.Capabilities.MccsVersion ?? "Unknown";

    public string Capabilities => Monitor.Session.Capabilities.Raw;

    [ObservableProperty]
    public partial string Firmware { get; private set; } = "Reading…";

    /// <summary>The vendor-specific VCP version line, or the vendor's name when it has no such register.</summary>
    [ObservableProperty]
    public partial string ProtocolInfo { get; private set; } = "Reading…";

    [ObservableProperty]
    public partial IReadOnlyList<FeatureRow> Features { get; private set; } = [];

    [ObservableProperty]
    public partial bool Copied { get; private set; }

    public string UnsupportedFeatures => string.Join(", ", FeatureCatalog.All.Except(Monitor.Session.Features).Select(feature => feature.Name));

    /// <summary>Shows what's known at once, then the two quick reads, then the slower full refresh.</summary>
    public override async Task OnShownAsync()
    {
        UpdateFeatures();

        // 0xFF/0xEF are the ASUS version registers. Lenovo's firmware version lives in 0xC9, whose three-byte reply
        // doesn't fit the 16-bit dxva2 transport, so it can't be read here.
        var isAsus = Monitor.Session.Id.Model.StartsWith("AUS", System.StringComparison.OrdinalIgnoreCase);
        var firmware = isAsus ? await Monitor.Session.ReadRawAsync(Vcp.AsusFirmware) : null;
        Firmware = firmware is { } reply ? reply.Maximum.ToString(System.Globalization.CultureInfo.InvariantCulture) : "Unknown";
        var version = isAsus ? await Monitor.Session.ReadRawAsync(Vcp.AsusVcpVersion) : null;
        ProtocolInfo = isAsus
            ? $"ASUS VCP {(version is { } v ? $"{(v.Maximum >> 8) & 0xFF}.{v.Maximum & 0xFF:X2}" : "Unknown")} · MCCS {MccsVersion}"
            : $"Lenovo monitor · MCCS {MccsVersion}";

        await Monitor.RefreshAsync();
        UpdateFeatures();
    }

    private void UpdateFeatures() =>
        Features = [.. Monitor.Session.Features.Select(feature => new FeatureRow(feature.Name, $"0x{feature.Code:X2}", Describe(Monitor[feature]!)))];

    [RelayCommand]
    private void CopyDiagnostics()
    {
        // Invariant culture: the text goes into bug reports read by people anywhere.
        string[] lines =
        [
            FormattableString.Invariant($"Display Toolkit {typeof(MonitorInfoPageViewModel).Assembly.GetName().Version?.ToString(3)}"),
            FormattableString.Invariant($"Windows {Environment.OSVersion.Version}"),
            $"Monitor: {Monitor.Name} ({Model})",
            $"Link: {Link}",
            $"Firmware: {Firmware}; {ProtocolInfo}",
            $"Capabilities: {Capabilities}",
            string.Empty,
            .. Features.Select(row => $"{row.Code} {row.Name}: {row.Value}"),
            string.Empty,
            $"Not supported: {UnsupportedFeatures}",
        ];
        Clipboard.SetText(string.Join(Environment.NewLine, lines));
        Copied = true;
    }

    private static string Describe(FeatureState state) => state.Feature switch
    {
        HdrModeFeature when state.Value == 0 => "No HDR signal",
        EnumFeature enumFeature => enumFeature.FindOption(state.Value)?.Name ?? $"0x{state.Value:X}",
        FlagFeature => state.Value != 0 ? "On" : "Off",
        _ => $"{state.Value} of {state.Maximum}",
    } + (state.IsLocked ? " (locked in this mode)" : string.Empty);
}
