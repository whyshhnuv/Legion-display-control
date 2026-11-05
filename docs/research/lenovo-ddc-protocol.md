# Lenovo monitor control protocol: notes

What Lenovo monitors (here a **Legion 24-10**, EDID `LEN68C4`, machine type `68C4`, MCCS 2.1, HDMI, 240 Hz, firmware
`255`) understand over DDC/CI.

## TL;DR

The same transport as the ASUS notes: **standard DDC/CI over the Windows `dxva2.dll` Monitor Configuration API**
(`GetPhysicalMonitorsFromHMONITOR`, `CapabilitiesRequestAndCapabilitiesReply`, `GetVCPFeatureAndVCPFeatureReply` /
`SetVCPFeature`). Lenovo's own "Accessories and Display Control" app (`MonitorControl.dll`) imports exactly these
functions — there is no proprietary channel. **Brightness, contrast, RGB gain, gamma, input, volume and mute already
worked** because they are standard MCCS codes both vendors share.

What did *not* work: the toolkit's `0xE0–0xFF` vendor space was hardcoded to **ASUS** semantics, so codes the Legion
advertises were mislabelled (0xEA shown as "FPS counter", 0xF9 as "Screen move", 0xF8 as "Pixel cleaning reminder",
0xEF read back as a bogus "ASUS VCP version"). And the Legion's real features (overdrive, DCR, HDR, game mode,
scaling, lock) had no mapping. Many "unsupported" controls are genuinely absent — the monitor does not advertise them.

## How a monitor is identified

The capabilities string's `model()` is a **marketing name** (`Legion 24-10`), not an id. The reliable id is the
**EDID** model, which the CCD API hands back as `LEN68C4` (the `C:/Users/sangi/displaycontrol/monitor-driver/`
`Legion 24-10.INF` matches on `monitor\LEN68C4`). The toolkit therefore detects the vendor from the EDID model
prefix: `LEN` → Lenovo, `AUS` → ASUS, anything else → unknown (standard features only).

Lenovo's app maps the same monitor by **machine type** `68C4` in `supportedmodels.json`, cross-checked against the
`0xC6` "Application Enable Key" block the monitor reports. The `0xC6` block is a per-feature enable table (bit layout
in `AppEnableKeyDef.json`). For the Legion it confirms: Overdrive, Game mode, HDR, Display Scaling, DCR, Color
Gamut, Gamma, RGB, Input, Resolution, Refresh Rate, Firmware update and Restore-factory are **on**; OLED Care,
Overclock, PIP/PBP, KVM, Face Tracking and Smart Audio are **off**. That is why those features do not exist to read
or write — the monitor does not offer them, so they stay hidden.

## VCP codes the Legion maps to a feature we expose

Source: Lenovo's `VCPCodeDef.json` (value meanings) and the Legion's own capabilities string (which values *this*
model advertises).

| Code | Name | Values this monitor advertises | Notes |
|------|------|-------------------------------|-------|
| `0x86` | Display Scaling | `02` Full Screen, `05` Original AR | Full MCCS set is `01 02 05 0B 0F 10 0C`; the rest are filtered out |
| `0x14` | Color Preset | `01` sRGB, `05` Reddish 6500K, `06` Neutral 7500K, `08` Bluish 9300K, `0B` Custom | On a Lenovo this is *color preset*, not a Kelvin list. The OSD shows `05` **Warm**, `06` **Neutral**, `08` **Cool** |
| `0x72` | Gamma | low bytes `50 64 78 8C A0` → 1.8/2.0/2.2/2.4/2.6 | Advertised as **single bytes**, not the 16-bit `0x5000` form ASUS uses. The OSD labels the steps as offsets from the 2.2 default: 1.8 = −0.4, 2.0 = −0.2, 2.4 = +0.2, 2.6 = +0.4 |
| `0x60` | Input Source | `0F` DP, `11` HDMI 1, `12` HDMI 2 | Matches the Legion's two HDMI + one DP |
| `0xE0` | Over Drive | `00` Off, `03`–`06` Level 1–4 | The `01`/`02` Normal/Extreme values exist for other models but this one doesn't advertise them |
| `0xEA` | Dynamic Contrast Ratio | `00` Off, `01` On | **Collides with ASUS 0xEA = FPS counter** |
| `0xEF` | HDR | `00` Off, `01` Auto | Other models list many presets; this one only Off/Auto. **Collides with ASUS 0xEF = VCP version** |
| `0xF9` | Game Mode | `01`–`06`, `0A` (FPS 1, FPS 2, Racing, RTS, Custom 1, Custom 2, Standard) | The list omits `00` Off — there is no off, "Standard" is the register's default. Switching modes is a **bundle**: it also rewrites brightness (`0x10`), color preset (`0x14`), gamma (`0x72`) and overdrive (`0xE0`), so the session re-reads those after a write. **Collides with ASUS 0xF9 = Screen move** |
| `0xF6` | Lock Monitor | bit `0` display, `1` OSD, `2` USB | We expose bit 0 (lock the front-panel buttons); DDC/CI is unaffected |
| `0xFA` | Refresh-rate display | bit `0x0800` (off `0xF5E0`, on `0xFDE0`) | A 16-bit feature manifest; the advertised values are the feature codes each bit drives, **not** a per-bit mask, so support is "the code is present". Measured by toggling from the Lenovo app. We expose the refresh-rate-display bit only |

`0xE0`/`0xEA`/`0xF9`/`0xEF` are the codes that collide with ASUS. The fix is a **vendor gate**: a feature that
claims a vendor only shows on a monitor of that vendor, so the ASUS "FPS counter" never appears on the Legion and the
Lenovo "Dynamic Contrast Ratio" never appears on an ASUS.

## Codes the Legion advertises but we deliberately do **not** map

These are in the capabilities string, but their meaning or safe value set could not be established reliably from the
vendor data, so we do not write them: `0xF7` / `0xF8` (nested sub-feature index protocol — FreeSync toggle, lighting,
per-mode sub-settings), `0xFB`/`0xFC` (Gaming Dashboard controls), `0xC9` (firmware version — a 3-byte reply that
doesn't fit the 16-bit `dxva2` transport), `0xAA`, `0xAC`, `0xAE`, `0xB2`, `0xB6`, `0xC8`, `0xDF`, `0x52`. The other
bits of the `0xFA` manifest (brightness/contrast/color/gamma/scaling/overdrive/DCR/HDR/game-mode toggles) are left
unmapped too — the manifest lists them, but each would need its own verification before writing. If a user wants one of
these, it needs its own verification before adding a register write.

## What this means for the app

- **Works out of the box (standard codes):** brightness, contrast, color temperature, red/green/blue, gamma, input,
  volume, mute.
- **Now mapped (Lenovo vendor features):** overdrive, dynamic contrast, monitor HDR mode, game mode, display scaling,
  lock-monitor-buttons, refresh-rate display.
- **Correctly hidden:** everything the capabilities string does not advertise (sharpness, saturation, six-axis,
  picture mode, blue light, VRR, etc.). The C6 enable table confirms they are genuinely off on this panel.
