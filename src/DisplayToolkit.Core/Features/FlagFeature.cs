using DisplayToolkit.Core.Capabilities;
using DisplayToolkit.Core.Ddc;

namespace DisplayToolkit.Core.Features;

/// <summary>
/// An on/off setting stored as one bit of an ASUS bitmask register (<c>0xFC</c> or <c>0xFD</c>). The value is 0 or 1.
/// The capabilities string lists the register's supported-bits mask, so support is per bit.
/// </summary>
public sealed class FlagFeature(string id, string name, byte code, int bit) : Feature(id, name, code)
{
    public int Bit { get; } = bit;

    private uint Mask => 1u << Bit;

    public override bool IsPartialRegister => true;

    public override bool IsSupportedBy(MonitorCapabilities capabilities) =>
        capabilities.ValuesOf(Code) is [var supportedBits, ..] && (supportedBits & Mask) != 0 && VendorMatches(capabilities);

    public override uint Decode(VcpReply reply) => (reply.Current & Mask) != 0 ? 1u : 0u;

    public override uint DecodeMaximum(VcpReply reply) => 1;

    public override uint Encode(uint value, uint register) => value != 0 ? register | Mask : register & ~Mask;
}

/// <summary>
/// One bit of a register whose advertised values are a list of feature codes, not a per-bit mask — the Legion's
/// <c>0xFA</c> feature manifest lists the features each bit drives, so a bit's presence can't be ANDed out of the
/// value list. Support is therefore "the code is present".
/// </summary>
public sealed class BitFeature(string id, string name, byte code, int bit) : Feature(id, name, code)
{
    public int Bit { get; } = bit;

    private uint Mask => 1u << Bit;

    public override bool IsPartialRegister => true;

    public override bool IsSupportedBy(MonitorCapabilities capabilities) => capabilities.Supports(Code) && VendorMatches(capabilities);

    public override uint Decode(VcpReply reply) => (reply.Current & Mask) != 0 ? 1u : 0u;

    public override uint DecodeMaximum(VcpReply reply) => 1;

    public override uint Encode(uint value, uint register) => value != 0 ? register | Mask : register & ~Mask;
}
