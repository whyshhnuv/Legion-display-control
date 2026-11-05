using DisplayToolkit.Core.Capabilities;
using DisplayToolkit.Core.Ddc;

namespace DisplayToolkit.Core.Features;

/// <summary>
/// One user-facing monitor setting and how it maps onto a VCP register. Features are immutable descriptors declared in
/// <see cref="FeatureCatalog"/>; their values live in <see cref="Monitors.MonitorSession"/>.
/// </summary>
public abstract class Feature(string id, string name, byte code)
{
    /// <summary>Stable identifier used by the UI, profiles and settings files. Never change an existing id.</summary>
    public string Id { get; } = id;

    /// <summary>English display name.</summary>
    public string Name { get; } = name;

    public byte Code { get; } = code;

    /// <summary>
    /// False for settings the monitor ignores while it shows HDR content. Not all of them report themselves as locked
    /// (<c>0xFE</c>): brightness still reads a normal value but writes have no effect.
    /// </summary>
    public bool AvailableInHdr { get; init; } = true;

    /// <summary>
    /// The vendors whose meaning of <see cref="Code"/> this feature implements, or null for all. Vendor-specific VCP
    /// codes overlap (0xE0 is Overdrive on a Lenovo and a different setting on an ASUS), so a feature that claims a
    /// vendor only shows when the monitor is one of those.
    /// </summary>
    public IReadOnlySet<MonitorVendor>? Vendors { get; init; }

    /// <summary>
    /// True for settings the monitor reports but won't let apps change: it accepts the write and then resets the value
    /// (proximity sensitivity on the PG32UCWM, also from ASUS's own app). Change them in the monitor's menu.
    /// </summary>
    public bool IsReadOnly { get; init; }

    /// <summary>
    /// True when the feature shares its register with other features, so writes must read the register first and
    /// change only this feature's bits.
    /// </summary>
    public virtual bool IsPartialRegister => false;

    /// <summary>
    /// Other registers the monitor changes when this one is written (a game mode retunes brightness, color
    /// temperature, gamma and overdrive). They are re-read after a confirmed write so the UI shows the side effects.
    /// </summary>
    public IReadOnlyList<byte> AffectedCodes { get; init; } = [];

    /// <summary>
    /// Choices the monitor only leaves for another one through 0 (off): changing away from them writes 0 first. Aura
    /// Sync on the PG32UCWM ignores a direct switch to another effect.
    /// </summary>
    public IReadOnlySet<uint> LeftThroughOff { get; init; } = new HashSet<uint>();

    /// <summary>How long the monitor may take before a write reads back correctly.</summary>
    public WriteSettling Settling { get; init; } = WriteSettling.Immediate;

    /// <summary>True when the monitor only applies <paramref name="value"/> after the user confirms it on the monitor.</summary>
    public virtual bool NeedsConfirmation(uint value) => false;

    public abstract bool IsSupportedBy(MonitorCapabilities capabilities);

    /// <summary>Whether <see cref="Vendors"/> (when set) allows the monitor's vendor. Always true for standard features.</summary>
    protected bool VendorMatches(MonitorCapabilities capabilities) =>
        Vendors is null || Vendors.Contains(capabilities.Vendor);

    /// <summary>Extracts this feature's value from a register reply.</summary>
    public virtual uint Decode(VcpReply reply) => reply.Current;

    /// <summary>Extracts this feature's maximum from a register reply.</summary>
    public virtual uint DecodeMaximum(VcpReply reply) => reply.Maximum;

    /// <summary>
    /// Produces the register value to write. <paramref name="register"/> is the current register value, and is only
    /// meaningful when <see cref="IsPartialRegister"/> is true.
    /// </summary>
    public virtual uint Encode(uint value, uint register) => value;

    public override string ToString() => $"{Id} (0x{Code:X2})";
}

public enum WriteSettling
{
    /// <summary>The new value reads back almost immediately.</summary>
    Immediate,

    /// <summary>
    /// The write switches a panel mode (an HDR preset, OLED anti-flicker). Windows re-detects the monitor, which
    /// invalidates handles, and the monitor reports stale values for several seconds.
    /// </summary>
    ModeSwitch,

    /// <summary>
    /// An action, not a setting (pixel cleaning). The monitor may stop answering while it runs, so the write isn't read
    /// back; the value is reported as written.
    /// </summary>
    Unverified,
}
