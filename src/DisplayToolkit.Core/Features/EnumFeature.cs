using DisplayToolkit.Core.Capabilities;

namespace DisplayToolkit.Core.Features;

/// <summary>A setting with a fixed set of choices, such as the GameVisual preset.</summary>
public class EnumFeature(string id, string name, byte code, IReadOnlyList<FeatureOption> options) : Feature(id, name, code)
{
    /// <summary>Every choice we know about, whether or not a given monitor supports it.</summary>
    public IReadOnlyList<FeatureOption> Options { get; } = options;

    public override bool IsSupportedBy(MonitorCapabilities capabilities) => capabilities.Supports(Code) && VendorMatches(capabilities);

    /// <summary>
    /// The choices this monitor advertises, under this monitor's names. If the monitor lists no values for the code,
    /// all known choices are offered.
    /// </summary>
    public virtual IReadOnlyList<FeatureOption> SupportedOptions(MonitorCapabilities capabilities)
    {
        var advertised = capabilities.ValuesOf(Code);
        return [.. Options.Where(option =>
            advertised.Count == 0
            || advertised.Contains(option.Value)
            // A monitor may list only one byte of a 16-bit choice (the Legion lists gamma as 0x50 64 78 8C A0, not 0x5000 …).
            || (option.Value > 0xFF && (advertised.Contains(option.Value & 0xFF) || advertised.Contains((option.Value >> 8) & 0xFF))))
            .Select(option => WithName(option, capabilities))];
    }

    /// <summary><paramref name="option"/> renamed for this monitor, when it has a vendor-specific name.</summary>
    private static FeatureOption WithName(FeatureOption option, MonitorCapabilities capabilities) =>
        option.VendorNames is { } names && names.TryGetValue(capabilities.Vendor, out var name) && name != option.Name
            ? option with { Name = name }
            : option;

    public override bool NeedsConfirmation(uint value) => FindOption(value)?.NeedsConfirmation == true;

    public FeatureOption? FindOption(uint value) => Options.FirstOrDefault(option => option.Value == value);
}
