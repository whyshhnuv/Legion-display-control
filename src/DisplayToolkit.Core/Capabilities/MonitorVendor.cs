namespace DisplayToolkit.Core.Capabilities;

/// <summary>
/// The monitor's manufacturer. Vendor-specific VCP codes overlap: ASUS and Lenovo both use <c>0xE0</c>–<c>0xFF</c>
/// with different meanings, so a feature that claims one vendor only shows on monitors of that vendor.
/// </summary>
public enum MonitorVendor
{
    Unknown,
    Asus,
    Lenovo
}

/// <summary>
/// The vendor from the EDID model (the <c>AUS32B1</c> / <c>LEN68C4</c> id), which is reliable; the capabilities
/// string's <c>model()</c> holds a marketing name instead.
/// </summary>
public static class MonitorVendorExtensions
{
    public static MonitorVendor FromEdidModel(this string? model)
    {
        if (string.IsNullOrWhiteSpace(model))
        {
            return MonitorVendor.Unknown;
        }
        if (model.StartsWith("LEN", StringComparison.OrdinalIgnoreCase))
        {
            return MonitorVendor.Lenovo;
        }
        if (model.StartsWith("AUS", StringComparison.OrdinalIgnoreCase))
        {
            return MonitorVendor.Asus;
        }
        return MonitorVendor.Unknown;
    }
}
