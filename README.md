# Noise Machine .NET

Made for both relaxation and tinkering, using the Avalonia UI.
Noise can be adjusted with a filter (coming soon, not implemented yet) of variable settings, like low-pass, band-pass, and high-pass. The tone is a sine wave that can be binaural beats, isochronic tones, or solfeggio frequencies.
Built for .NET 7 or higher.

![Preview as of 2023/9/25](preview.png)

## Available noise types

- White (kind of loud)
- Pink (loud)
- Perlin (very quiet)
- Brownian (moderately quiet)

## Available tone types

- Binaural beats (right ear = left ear + frequency)
- Isochronic tones (volume goes on and off at a rate of the set frequency)
- Solfeggio frequencies (tone is at the set frequency)

## Licenses

- Project is under the GNU GPL 3.0 or later license
- Individual code and components are under the GNU Lesser GPL 3.0 or later license unless specified
- [mackron/miniaudio](https://github.com/mackron/miniaudio) is under the public domain or MIT no-attribution license
-- Bindings to Miniaudio are generated using ClangSharp which is under the MIT license
- AvaloniaUI is under the MIT license
- C# implementation of the Perlin noise algorithm is from [Gaming32/ArrayV](https://github.com/Gaming32/ArrayV), which is under the MIT license
- Pink noise algorithm is under the BSD-3-clause license
- Random extensions are from <https://bitbucket.org/Superbest/superbest-random> which is under an unknown license.
