using DisplayToolkit.Core.Capabilities;
using DisplayToolkit.Core.Ddc;
using DisplayToolkit.Core.Features;
using DisplayToolkit.Core.Tests.Fakes;

namespace DisplayToolkit.Core.Tests.Features;

public sealed class FeatureCatalogTests
{
    private static readonly MonitorCapabilities Pg32ucwm = CapabilitiesParser.Parse(
        SimulatedMonitor.Pg32ucwmCapabilities, "AUS32B1".FromEdidModel());

    // The capabilities' model() is a marketing name; the vendor comes from the EDID model (LEN68C4).
    private static readonly MonitorCapabilities Legion2410 = CapabilitiesParser.Parse(
        SimulatedMonitor.Legion2410Capabilities, "LEN68C4".FromEdidModel());

    [Fact]
    public void Feature_ids_are_unique()
    {
        var ids = FeatureCatalog.All.Select(feature => feature.Id).ToList();

        Assert.Equal(ids.Count, ids.Distinct().Count());
    }

    [Fact]
    public void Every_feature_is_listed_in_All()
    {
        var declared = typeof(FeatureCatalog).GetFields()
            .Where(field => field.IsStatic && typeof(Feature).IsAssignableFrom(field.FieldType))
            .Select(field => (Feature)field.GetValue(null)!);

        Assert.Equal(declared.ToHashSet(), FeatureCatalog.All.ToHashSet());
    }

    [Theory]
    [InlineData("brightness")]
    [InlineData("picture-mode")]
    [InlineData("hdr-mode")]
    [InlineData("vrr")]
    [InlineData("frame-rate-boost")]
    [InlineData("taskbar-detection")]
    [InlineData("global-dimming")]
    [InlineData("pixel-cleaning")]
    [InlineData("proximity-distance")]
    [InlineData("key-lock")]
    [InlineData("input-auto-detection")]
    public void Pg32ucwm_supports(string id)
    {
        Assert.True(FeatureCatalog.Find(id)!.IsSupportedBy(Pg32ucwm));
    }

    [Fact]
    public void Flag_support_follows_the_supported_bits_mask()
    {
        // Bit 12 (boundary detection) is missing from the PG32UCWM's 0xFD mask 0x6979.
        Assert.False(FeatureCatalog.BoundaryDetection.IsSupportedBy(Pg32ucwm));
        Assert.True(FeatureCatalog.OuterDimming.IsSupportedBy(Pg32ucwm));
    }

    [Fact]
    public void Picture_modes_follow_the_advertised_values()
    {
        var names = FeatureCatalog.PictureMode.SupportedOptions(Pg32ucwm).Select(option => option.Name);

        // The PG32UCWM advertises sRGB Cal (10) instead of plain sRGB (3).
        Assert.Equal(["Cinema", "Scenery", "User", "Racing", "RTS/RPG", "FPS", "MOBA", "Night vision", "sRGB Cal"], names);
    }

    [Fact]
    public void Hdr_presets_combine_advertised_groups_and_presets()
    {
        var values = FeatureCatalog.HdrMode.SupportedOptions(Pg32ucwm).Select(option => option.Value);

        Assert.Equal([0x0101u, 0x0102, 0x0103, 0x0104, 0x0205, 0x0206, 0x0207], values);
    }

    [Fact]
    public void Proximity_fields_split_the_register()
    {
        Assert.Equal([0u, 3, 2, 1, 0xFF], FeatureCatalog.ProximityDistance.SupportedOptions(Pg32ucwm).Select(option => option.Value));
        Assert.Equal([0u, 5, 10, 15], FeatureCatalog.ProximityScreenOff.SupportedOptions(Pg32ucwm).Select(option => option.Value));

        var reply = new VcpReply(0x0501, 0x0FFF);
        Assert.Equal(1u, FeatureCatalog.ProximityDistance.Decode(reply));
        Assert.Equal(5u, FeatureCatalog.ProximityScreenOff.Decode(reply));
        Assert.Equal(0x0A01u, FeatureCatalog.ProximityScreenOff.Encode(10, 0x0501));
        Assert.Equal(0x0503u, FeatureCatalog.ProximityDistance.Encode(3, 0x0501));
    }

    [Fact]
    public void Flags_decode_the_measured_oled_register()
    {
        var reply = new VcpReply(0x6829, 0x6979);

        Assert.Equal(1u, FeatureCatalog.VariableRefreshRate.Decode(reply));
        Assert.Equal(1u, FeatureCatalog.ScreenDimming.Decode(reply));
        Assert.Equal(1u, FeatureCatalog.LogoDetection.Decode(reply));
        Assert.Equal(0u, FeatureCatalog.UniformBrightness.Decode(reply));
        Assert.Equal(1u, FeatureCatalog.TaskbarDetection.Decode(reply));
        Assert.Equal(1u, FeatureCatalog.OuterDimming.Decode(reply));
        Assert.Equal(1u, FeatureCatalog.GlobalDimming.Decode(reply));
        Assert.Equal(0u, FeatureCatalog.PixelCleaning.Decode(reply));
    }

    [Fact]
    public void Flag_encoding_changes_only_its_bit()
    {
        Assert.Equal(0x6869u, FeatureCatalog.UniformBrightness.Encode(1, 0x6829));
        Assert.Equal(0x6829u, FeatureCatalog.UniformBrightness.Encode(0, 0x6869));
    }

    [Fact]
    public void Range_features_ignore_the_high_word()
    {
        // ASUS puts the factory default in the high word of the RGB gain registers.
        var reply = new VcpReply(0x0064_0032, 0x0064);

        Assert.Equal(0x32u, FeatureCatalog.RedGain.Decode(reply));
    }

    [Fact]
    public void Locked_replies_are_recognised()
    {
        Assert.True(new VcpReply(0xFE, 0xFE).IsLocked);
        Assert.False(new VcpReply(0xFE, 0xFF).IsLocked);
    }

    [Theory]
    [InlineData("overdrive")]
    [InlineData("dynamic-contrast-ratio")]
    [InlineData("lenovo-hdr")]
    [InlineData("game-mode")]
    [InlineData("display-scaling")]
    [InlineData("lock-monitor")]
    [InlineData("refresh-rate-display")]
    public void Legion2410_supports(string id)
    {
        Assert.True(FeatureCatalog.Find(id)!.IsSupportedBy(Legion2410));
    }

    [Fact]
    public void Refresh_rate_display_is_a_bit_of_the_feature_manifest()
    {
        // 0xFA advertises a list of feature codes, not a per-bit mask, so support is "the code is present" and the
        // bit (0x0800) is decoded out of the 16-bit register: off 0xF5E0, on 0xFDE0 (measured on the Legion).
        Assert.Equal(0u, FeatureCatalog.RefreshRateDisplay.Decode(new VcpReply(0xF5E0, 0xF5E0)));
        Assert.Equal(1u, FeatureCatalog.RefreshRateDisplay.Decode(new VcpReply(0xFDE0, 0xF5E0)));
        Assert.Equal(0xFDE0u, FeatureCatalog.RefreshRateDisplay.Encode(1, 0xF5E0));
        Assert.Equal(0xF5E0u, FeatureCatalog.RefreshRateDisplay.Encode(0, 0xFDE0));
    }

    [Fact]
    public void Game_mode_offers_the_osd_names_without_an_off()
    {
        var names = FeatureCatalog.GameMode.SupportedOptions(Legion2410).Select(option => option.Name).ToList();

        Assert.Equal(["FPS 1", "FPS 2", "Racing", "RTS", "Custom 1", "Custom 2", "Standard"], names);
    }

    [Theory]
    [InlineData("fps-counter")]
    [InlineData("screen-move")]
    [InlineData("pixel-cleaning-reminder")]
    [InlineData("picture-mode")]
    [InlineData("vrr")]
    [InlineData("key-lock")]
    [InlineData("blue-light")]
    [InlineData("shadow-boost")]
    [InlineData("crosshair")]
    [InlineData("elmb")]
    public void Legion2410_does_not_show_asus_vendor_features(string id)
    {
        // 0xEA/0xF9/0xF8 overlap: on a Lenovo they mean other things, so the ASUS features must stay hidden.
        Assert.False(FeatureCatalog.Find(id)!.IsSupportedBy(Legion2410));
    }

    [Fact]
    public void Legion2410_keeps_the_standard_features()
    {
        var supported = FeatureCatalog.All.Where(feature => feature.IsSupportedBy(Legion2410)).ToList();

        Assert.Contains(FeatureCatalog.Brightness, supported);
        Assert.Contains(FeatureCatalog.Contrast, supported);
        Assert.Contains(FeatureCatalog.ColorTemperature, supported);
        Assert.Contains(FeatureCatalog.InputSource, supported);
        Assert.Contains(FeatureCatalog.Mute, supported);
    }

    [Fact]
    public void Gamma_options_match_the_advertised_low_bytes()
    {
        // The Legion advertises 0x72 as single bytes (05 78 FB 50 64 78 8C A0), not the 16-bit register values, and
        // its OSD labels the steps as offsets from the 2.2 default.
        var names = FeatureCatalog.Gamma.SupportedOptions(Legion2410).Select(option => option.Name).ToList();

        Assert.Equal(["1.8 (−0.4)", "2.0 (−0.2)", "2.2", "2.4 (+0.2)", "2.6 (+0.4)"], names);

        // …and the ASUS form (full 16-bit values, plain names) still matches.
        Assert.Equal(["1.8", "2.0", "2.2", "2.4", "2.6"], FeatureCatalog.Gamma.SupportedOptions(Pg32ucwm).Select(option => option.Name));
    }

    [Fact]
    public void Color_temperature_uses_the_osd_name_per_vendor()
    {
        // 0x14 is a Kelvin list on an ASUS but a named preset on a Lenovo.
        Assert.Equal(["Warm", "Neutral", "Cool", "Custom"], FeatureCatalog.ColorTemperature.SupportedOptions(Legion2410).Select(option => option.Name));
        Assert.Equal(["4000K", "5000K", "6500K", "7500K", "8200K", "9300K", "10000K", "Custom"],
            FeatureCatalog.ColorTemperature.SupportedOptions(Pg32ucwm).Select(option => option.Name));
    }

    [Fact]
    public void Overdrive_options_follow_the_advertised_levels()
    {
        var names = FeatureCatalog.Overdrive.SupportedOptions(Legion2410).Select(option => option.Name).ToList();

        Assert.Equal(["Off", "Level 1", "Level 2", "Level 3", "Level 4"], names);
    }

    [Fact]
    public void Lock_monitor_reads_its_bit()
    {
        Assert.Equal(0u, FeatureCatalog.LockMonitor.Decode(new VcpReply(0, 1)));
        Assert.Equal(1u, FeatureCatalog.LockMonitor.Decode(new VcpReply(1, 1)));
        Assert.Equal(1u, FeatureCatalog.LockMonitor.Encode(1, 0));
        Assert.Equal(0u, FeatureCatalog.LockMonitor.Encode(0, 1));
    }
}
