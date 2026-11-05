using DisplayToolkit.Core.Capabilities;

namespace DisplayToolkit.Core.Features;

/// <summary>One choice of an <see cref="EnumFeature"/>: the raw value written to the monitor and its English name.</summary>
public sealed record FeatureOption(uint Value, string Name)
{
    /// <summary>
    /// The monitor asks the user to confirm this choice in its on-screen menu before applying it (proximity
    /// "Tailored", which then calibrates). The register keeps its old value until then.
    /// </summary>
    public bool NeedsConfirmation { get; init; }

    /// <summary>
    /// A per-vendor name for a shared register (0x14 reads "7500K" on an ASUS but "Neutral" on a Lenovo), or null to
    /// use <see cref="Name"/> for every vendor.
    /// </summary>
    public IReadOnlyDictionary<MonitorVendor, string>? VendorNames { get; init; }

    /// <summary>Builds a choice whose <paramref name="vendor"/> shows <paramref name="vendorName"/> instead of <paramref name="name"/>.</summary>
    public static FeatureOption Named(uint value, string name, MonitorVendor vendor, string vendorName) =>
        new(value, name) { VendorNames = new Dictionary<MonitorVendor, string> { [vendor] = vendorName } };
}
