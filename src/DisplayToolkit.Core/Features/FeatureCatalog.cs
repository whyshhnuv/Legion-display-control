using DisplayToolkit.Core.Capabilities;

namespace DisplayToolkit.Core.Features;

/// <summary>
/// Every monitor setting Display Toolkit knows about. A monitor exposes the subset its capabilities string supports,
/// and features that name a vendor only apply to that vendor (ASUS and Lenovo's vendor VCP codes overlap). Values and
/// semantics: ASUS in <c>docs/research/asus-ddc-protocol.md</c>, Lenovo in <c>docs/research/lenovo-ddc-protocol.md</c>.
/// </summary>
public static class FeatureCatalog
{
    /// <summary>The vendor-specific (0xE0-0xFF) settings are only these vendors' own.</summary>
    private static readonly HashSet<MonitorVendor> Asus = [MonitorVendor.Asus];
    private static readonly HashSet<MonitorVendor> Lenovo = [MonitorVendor.Lenovo];

    // Picture
    // Settings marked AvailableInHdr = false are greyed out by ASUS DisplayWidget Center in HDR, and brightness
    // writes were verified to have no effect in HDR on a PG32UCWM.
    public static readonly RangeFeature Brightness = new("brightness", "Brightness", Vcp.Brightness) { AvailableInHdr = false };
    public static readonly RangeFeature Contrast = new("contrast", "Contrast", Vcp.Contrast);
    public static readonly RangeFeature Sharpness = new("sharpness", "Sharpness", Vcp.Sharpness);
    public static readonly RangeFeature Saturation = new("saturation", "Saturation", Vcp.Saturation);

    /// <summary>The GameVisual preset, in the low byte of its register; the high byte is <see cref="ColorGamut"/>.</summary>
    public static readonly ByteFieldFeature PictureMode = new("picture-mode", "Picture mode", Vcp.AsusGameVisual, highByte: false,
    [
        new(1, "Cinema"),
        new(2, "Scenery"),
        new(3, "sRGB"),
        new(4, "User"),
        new(5, "Racing"),
        new(6, "RTS/RPG"),
        new(7, "FPS"),
        new(8, "MOBA"),
        new(9, "Night vision"),
        new(10, "sRGB Cal"),
    ])
    {
        // In HDR the register reads 5 (Racing in SDR) regardless of the preset.
        AvailableInHdr = false,
        Vendors = Asus,
    };

    /// <summary>
    /// The range of colors the current picture mode uses, in the high byte of the picture mode register. Only on
    /// monitors that advertise the high-byte choices (ASUS calls it Color Richness).
    /// </summary>
    public static readonly ByteFieldFeature ColorGamut = new("color-gamut", "Color gamut", Vcp.AsusGameVisual, highByte: true,
    [
        new(0, "Native (wide gamut)"),
        new(1, "sRGB"),
        new(2, "DCI-P3"),
    ])
    {
        AvailableInHdr = false,
        Vendors = Asus,
    };

    // Group/preset pairs. HDR10 presets are hardware-verified; the Dolby Vision pairing (group 2 with presets 5–7)
    // follows the capabilities string and still needs verification with Dolby Vision content.
    public static readonly HdrModeFeature HdrMode = new("hdr-mode", "HDR mode", Vcp.AsusHdrMode,
    [
        new(0x0101, "Cinema HDR"),
        new(0x0102, "Gaming HDR"),
        new(0x0103, "Console HDR"),
        new(0x0104, "True Black 400"),
        new(0x0205, "Dolby Vision Bright"),
        new(0x0206, "Dolby Vision Dark"),
        new(0x0207, "Dolby Vision Gaming"),
    ])
    {
        Settling = WriteSettling.ModeSwitch,
        Vendors = Asus,
    };

    // Color
    // 0x14 is a Kelvin list on an ASUS but a named color preset on a Lenovo; the per-vendor names come from each vendor's OSD.
    public static readonly EnumFeature ColorTemperature = new("color-temperature", "Color temperature", Vcp.ColorPreset,
    [
        new(3, "4000K"),
        new(4, "5000K"),
        FeatureOption.Named(5, "6500K", MonitorVendor.Lenovo, "Warm"),
        FeatureOption.Named(6, "7500K", MonitorVendor.Lenovo, "Neutral"),
        new(7, "8200K"),
        FeatureOption.Named(8, "9300K", MonitorVendor.Lenovo, "Cool"),
        new(9, "10000K"),
        new(11, "Custom"), // "User" in the monitor's menu, which clashes with the User picture mode.
    ]);

    public static readonly RangeFeature RedGain = new("red-gain", "Red", Vcp.RedGain);
    public static readonly RangeFeature GreenGain = new("green-gain", "Green", Vcp.GreenGain);
    public static readonly RangeFeature BlueGain = new("blue-gain", "Blue", Vcp.BlueGain);

    // The Legion's OSD shows the steps as offsets from the 2.2 default, so the label carries both on a Lenovo.
    public static readonly EnumFeature Gamma = new("gamma", "Gamma", Vcp.Gamma,
    [
        FeatureOption.Named(0x5000, "1.8", MonitorVendor.Lenovo, "1.8 (−0.4)"),
        FeatureOption.Named(0x6400, "2.0", MonitorVendor.Lenovo, "2.0 (−0.2)"),
        new(0x7800, "2.2"),
        FeatureOption.Named(0x8C00, "2.4", MonitorVendor.Lenovo, "2.4 (+0.2)"),
        FeatureOption.Named(0xA000, "2.6", MonitorVendor.Lenovo, "2.6 (+0.4)"),
    ]);

    public static readonly RangeFeature SaturationRed = new("saturation-red", "Red saturation", Vcp.SaturationRed);
    public static readonly RangeFeature SaturationYellow = new("saturation-yellow", "Yellow saturation", Vcp.SaturationYellow);
    public static readonly RangeFeature SaturationGreen = new("saturation-green", "Green saturation", Vcp.SaturationGreen);
    public static readonly RangeFeature SaturationCyan = new("saturation-cyan", "Cyan saturation", Vcp.SaturationCyan);
    public static readonly RangeFeature SaturationBlue = new("saturation-blue", "Blue saturation", Vcp.SaturationBlue);
    public static readonly RangeFeature SaturationMagenta = new("saturation-magenta", "Magenta saturation", Vcp.SaturationMagenta);

    // Gaming and comfort
    public static readonly RangeFeature BlueLightFilter = new("blue-light", "Blue light filter", Vcp.AsusBlueLightFilter)
    {
        AvailableInHdr = false,
        Vendors = Asus,
    };
    public static readonly RangeFeature ShadowBoost = new("shadow-boost", "Shadow boost", Vcp.AsusShadowBoost)
    {
        AvailableInHdr = false,
        Vendors = Asus,
    };
    // Both change the signal timing, so Windows drops and re-detects the monitor (VRR verified on a PG32UCWM).
    public static readonly FlagFeature VariableRefreshRate = new("vrr", "Variable refresh rate", Vcp.AsusToggles2, bit: 0)
    {
        Settling = WriteSettling.ModeSwitch,
        Vendors = Asus,
    };

    public static readonly FlagFeature FrameRateBoost = new("frame-rate-boost", "Frame-rate boost", Vcp.AsusToggles2, bit: 8)
    {
        Settling = WriteSettling.ModeSwitch,
        Vendors = Asus,
    };
    /// <summary>Sharpens the picture when the monitor scales up a lower resolution. 0 is off.</summary>
    public static readonly RangeFeature UpscalingSharpness = new("upscaling-sharpness", "Upscaling sharpness (Smart Pixel)", Vcp.AsusUpscalingSharpness)
    {
        Vendors = Asus,
    };

    public static readonly SwitchFeature Elmb = new("elmb", "ELMB", Vcp.AsusElmb)
    {
        Vendors = Asus,
    };

    // GamePlus (overlays drawn by the monitor)
    public static readonly EnumFeature Crosshair = new("crosshair", "Crosshair", Vcp.AsusCrosshair,
    [
        new(0, "Off"),
        new(7, "Blue dot"),
        new(8, "Green dot"),
        new(9, "Blue mini duplex"),
        new(10, "Green mini duplex"),
        new(11, "Blue heavy duplex"),
        new(12, "Green heavy duplex"),

        // Not in the monitor's menu, but advertised and working (PG32UCWM).
        new(13, "Adaptive dot"),
        new(14, "Adaptive crosshair, small"),
        new(15, "Adaptive crosshair, large"),
    ])
    {
        Vendors = Asus,
    };

    public static readonly EnumFeature FpsCounter = new("fps-counter", "FPS counter", Vcp.AsusFpsCounter,
    [
        new(0, "Off"),
        new(1, "Number"),
        new(2, "Bar"),
    ])
    {
        Vendors = Asus,
    };

    public static readonly EnumFeature Timer = new("timer", "Timer", Vcp.AsusGamePlusTimer,
    [
        new(0, "Off"),
        new(1, "30 s"),
        new(2, "40 s"),
        new(3, "50 s"),
        new(4, "60 s"),
        new(5, "90 s"),
    ])
    {
        Vendors = Asus,
    };

    public static readonly SwitchFeature DisplayAlignment = new("display-alignment", "Display alignment", Vcp.AsusDisplayAlignment)
    {
        Vendors = Asus,
    };

    // OLED care
    public static readonly FlagFeature ScreenDimming = new("screen-dimming", "Screen dimming control", Vcp.AsusToggles2, bit: 3) { Vendors = Asus };
    public static readonly FlagFeature LogoDetection = new("logo-detection", "Logo detection", Vcp.AsusToggles2, bit: 5) { Vendors = Asus };
    public static readonly FlagFeature UniformBrightness = new("uniform-brightness", "Uniform brightness", Vcp.AsusToggles2, bit: 6) { Vendors = Asus };
    public static readonly FlagFeature TaskbarDetection = new("taskbar-detection", "Taskbar detection", Vcp.AsusToggles2, bit: 11) { Vendors = Asus };
    public static readonly FlagFeature BoundaryDetection = new("boundary-detection", "Boundary detection", Vcp.AsusToggles2, bit: 12) { Vendors = Asus };
    public static readonly FlagFeature OuterDimming = new("outer-dimming", "Outer dimming control", Vcp.AsusToggles2, bit: 13) { Vendors = Asus };
    public static readonly FlagFeature GlobalDimming = new("global-dimming", "Global dimming control", Vcp.AsusToggles2, bit: 14) { Vendors = Asus };

    /// <summary>Writing 1 starts pixel cleaning: the screen goes dark for about 6 minutes.</summary>
    public static readonly FlagFeature PixelCleaning = new("pixel-cleaning", "Pixel cleaning", Vcp.AsusToggles2, bit: 4)
    {
        Settling = WriteSettling.Unverified,
        Vendors = Asus,
    };

    // Changes the panel timing: Windows drops and re-detects the monitor (verified on a PG32UCWM).
    public static readonly SwitchFeature OledAntiFlicker = new("oled-anti-flicker", "OLED anti-flicker", Vcp.AsusOledAntiFlicker)
    {
        Settling = WriteSettling.ModeSwitch,
        Vendors = Asus,
    };

    public static readonly EnumFeature ScreenMove = new("screen-move", "Screen move", Vcp.AsusScreenMove,
    [
        new(0, "Off"),
        new(1, "Light"),
        new(2, "Middle"),
        new(3, "Strong"),
    ])
    {
        Vendors = Asus,
    };

    public static readonly EnumFeature PixelCleaningReminder = new("pixel-cleaning-reminder", "Pixel cleaning reminder", Vcp.AsusPixelCleaningReminder,
    [
        new(0, "Off"),
        new(2, "2 hours"),
        new(4, "4 hours"),
        new(8, "8 hours"),
    ])
    {
        Vendors = Asus,
    };

    // Lighting
    public static readonly ByteFieldFeature AuraEffect = new("aura-effect", "Aura lighting", Vcp.AsusAura, highByte: false,
    [
        new(0, "Off"),
        new(1, "Aura Sync"),
        new(2, "Rainbow"),
        new(3, "Color cycle"),
        new(4, "Static"),
        new(5, "Breathing"),
        new(6, "Strobing"),
    ])
    {
        LeftThroughOff = new HashSet<uint> { 1 },
        Vendors = Asus,
    };

    /// <summary>The color of the Static, Breathing and Strobing effects. Picking one of those without a color gives red.</summary>
    public static readonly ByteFieldFeature AuraColor = new("aura-color", "Aura color", Vcp.AsusAura, highByte: true,
    [
        new(1, "Red"),
        new(2, "Green"),
        new(3, "Blue"),
        new(4, "Cyan"),
        new(5, "Magenta"),
        new(6, "Yellow"),
    ])
    {
        Vendors = Asus,
    };

    public static readonly ByteFieldFeature ProximityDistance = new("proximity-distance", "Proximity sensor", Vcp.AsusProximitySensor, highByte: false,
    [
        new(0, "Off"),
        new(3, "Up to 60 cm"),
        new(2, "Up to 90 cm"),
        new(1, "Up to 120 cm"),
        new(0xFF, "Tailored") { NeedsConfirmation = true },
    ])
    {
        Vendors = Asus,
    };

    public static readonly ByteFieldFeature ProximityScreenOff = new("proximity-screen-off", "Turn screen off after", Vcp.AsusProximitySensor, highByte: true,
    [
        new(0, "Off"),
        new(5, "5 minutes"),
        new(10, "10 minutes"),
        new(15, "15 minutes"),
    ])
    {
        Vendors = Asus,
    };

    public static readonly RangeFeature ProximitySensitivity = new("proximity-sensitivity", "Proximity sensitivity", Vcp.AsusProximitySensitivity)
    {
        IsReadOnly = true,
        Vendors = Asus,
    };

    // Input and sound
    public static readonly EnumFeature InputSource = new("input-source", "Input", Vcp.InputSource,
    [
        new(0x0F, "DisplayPort"),
        new(0x10, "DisplayPort 2"),
        new(0x11, "HDMI 1"),
        new(0x12, "HDMI 2"),
        new(0x13, "HDMI 3"),
        new(0x1A, "USB-C"),
        new(0x1B, "USB-C 2"),
    ]);

    public static readonly RangeFeature Volume = new("volume", "Volume", Vcp.AudioVolume);
    public static readonly SwitchFeature Mute = new("mute", "Mute", Vcp.AudioMute, offValue: 2, onValue: 1);

    // Monitor (system setup)
    public static readonly FlagFeature PowerIndicator = new("power-indicator", "Power indicator", Vcp.AsusToggles1, bit: 0) { Vendors = Asus };
    public static readonly FlagFeature PowerKeyLock = new("power-key-lock", "Power key lock", Vcp.AsusToggles1, bit: 1) { Vendors = Asus };
    public static readonly FlagFeature KeyLock = new("key-lock", "Key lock", Vcp.AsusToggles1, bit: 2) { Vendors = Asus };
    public static readonly FlagFeature InputAutoDetection = new("input-auto-detection", "Input auto-detection", Vcp.AsusToggles1, bit: 4) { Vendors = Asus };

    // Lenovo (Legion 24-10). 0x86 and 0x14 share codes with standard MCCS registers that mean different things on
    // Lenovos (scaling, color preset); the vendor gate keeps the standard features off them.
    /// <summary>Response overdrive: how fast the panel chases a new picture. Off and the Level settings are what this panel offers.</summary>
    public static readonly EnumFeature Overdrive = new("overdrive", "Response overdrive", Vcp.LenovoOverdrive,
    [
        new(0, "Off"),
        new(3, "Level 1"),
        new(4, "Level 2"),
        new(5, "Level 3"),
        new(6, "Level 4"),
    ])
    {
        Vendors = Lenovo,
    };

    /// <summary>Boosts contrast in dark scenes; the panel adjusts its backlight curve.</summary>
    public static readonly SwitchFeature DynamicContrastRatio = new("dynamic-contrast-ratio", "Dynamic contrast ratio", Vcp.LenovoDynamicContrastRatio)
    {
        Vendors = Lenovo,
    };

    /// <summary>The Lenovo's HDR switch (0x00 off, 0x01 auto). More presets exist on other models; the filter keeps this to what the monitor advertises.</summary>
    public static readonly EnumFeature LenovoHdr = new("lenovo-hdr", "Monitor HDR mode", Vcp.LenovoHdr,
    [
        new(0, "Off"),
        new(1, "Auto"),
    ])
    {
        Vendors = Lenovo,
    };

    /// <summary>
    /// The picture profile for a genre of game. It is a bundle: switching it retunes brightness, color temperature,
    /// gamma and overdrive, so the session re-reads those after a write. There is no "off" value on this panel — the
    /// modes cycle, and "Standard" (0x0A) is the register's default.
    /// </summary>
    public static readonly EnumFeature GameMode = new("game-mode", "Game mode", Vcp.LenovoGameMode,
    [
        new(1, "FPS 1"),
        new(2, "FPS 2"),
        new(3, "Racing"),
        new(4, "RTS"),
        new(5, "Custom 1"),
        new(6, "Custom 2"),
        new(0xA, "Standard"),
    ])
    {
        Vendors = Lenovo,
        AffectedCodes = [Vcp.Brightness, Vcp.ColorPreset, Vcp.Gamma, Vcp.LenovoOverdrive],
    };

    /// <summary>
    /// Shows the current refresh rate on screen. Bit 0x0800 of the 0xFA feature manifest (off 0xF5E0, on 0xFDE0).
    /// </summary>
    public static readonly BitFeature RefreshRateDisplay = new("refresh-rate-display", "Refresh rate display", Vcp.LenovoFeatureToggles, bit: 11)
    {
        Vendors = Lenovo,
    };

    /// <summary>
    /// How the monitor fits the picture to the panel. The MCCS 0x86 code shares the register with the standard
    /// "image position" on other vendors; here it means aspect-ratio scaling.
    /// </summary>
    public static readonly EnumFeature DisplayScalingFeature = new("display-scaling", "Display scaling", Vcp.DisplayScaling,
    [
        new(0x01, "No scaling"),
        new(0x02, "Full screen"),
        new(0x05, "Original aspect"),
        new(0x0B, "4:3"),
        new(0x0F, "16:9"),
        new(0x10, "16:10"),
        new(0x0C, "5:4"),
    ]);

    /// <summary>
    /// Locks the monitor's own front-panel buttons (bit 0 of 0xF6). Display Toolkit keeps working: it talks to the
    /// monitor over DDC/CI, which the lock doesn't block.
    /// </summary>
    public static readonly FlagFeature LockMonitor = new("lock-monitor", "Lock monitor buttons", Vcp.LenovoLock, bit: 0)
    {
        Vendors = Lenovo,
    };

    public static IReadOnlyList<Feature> All { get; } =
    [
        Brightness, Contrast, Sharpness, Saturation, PictureMode, ColorGamut, HdrMode,
        ColorTemperature, RedGain, GreenGain, BlueGain, Gamma,
        SaturationRed, SaturationYellow, SaturationGreen, SaturationCyan, SaturationBlue, SaturationMagenta,
        BlueLightFilter, ShadowBoost, VariableRefreshRate, FrameRateBoost, UpscalingSharpness, Elmb,
        Crosshair, FpsCounter, Timer, DisplayAlignment,
        ScreenDimming, LogoDetection, UniformBrightness, TaskbarDetection, BoundaryDetection, OuterDimming, GlobalDimming,
        PixelCleaning, OledAntiFlicker, ScreenMove, PixelCleaningReminder,
        ProximityDistance, ProximityScreenOff, ProximitySensitivity,
        AuraEffect, AuraColor,
        InputSource, Volume, Mute,
        PowerIndicator, PowerKeyLock, KeyLock, InputAutoDetection,
        Overdrive, DynamicContrastRatio, LenovoHdr, GameMode, DisplayScalingFeature, LockMonitor, RefreshRateDisplay,
    ];

    public static Feature? Find(string id) => All.FirstOrDefault(feature => feature.Id == id);
}
