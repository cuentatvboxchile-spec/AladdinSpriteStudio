using AladdinSpriteStudio.UI.CharacterReplacement.Conversion;
using AladdinSpriteStudio.UI.CharacterReplacement.Models;

namespace AladdinSpriteStudio.UI.CharacterReplacement.Rendering;

/// <summary>
/// Crea paletas destinadas únicamente a visualizar tiles dentro del editor.
///
/// La paleta visual de Aladdin se obtiene de la hoja original cargada y se
/// reduce a 16 índices mediante el cuantizador existente. No modifica la ROM,
/// la paleta CGRAM del juego ni los datos 4BPP.
/// </summary>
public static class TilePreviewPaletteProvider
{
    public static IReadOnlyList<System.Drawing.Color>
        CreateGrayscalePalette()
    {
        List<System.Drawing.Color> palette =
            new()
            {
                System.Drawing.Color.Transparent
            };

        for (int index = 1;
             index < 16;
             index++)
        {
            int value =
                (int)Math.Round(
                    index *
                    255.0 /
                    15.0);

            palette.Add(
                System.Drawing.Color.FromArgb(
                    255,
                    value,
                    value,
                    value));
        }

        return palette;
    }

    public static IReadOnlyList<System.Drawing.Color>
        CreateAladdinPreviewPalette(
            SpriteSheetDocument aladdinDocument)
    {
        ArgumentNullException.ThrowIfNull(
            aladdinDocument);

        SnesSpritePaletteQuantizationOptions options =
            new()
            {
                MaximumPaletteColors =
                    16,

                ReserveIndexZeroForTransparency =
                    true,

                BackgroundColor =
                    aladdinDocument.BackgroundColor,

                BackgroundTolerance =
                    20,

                AlphaThreshold =
                    16,

                SamplingStride =
                    1,

                UseDithering =
                    false
            };

        using SnesSpritePaletteQuantizationResult result =
            SnesSpritePaletteQuantizer.Quantize(
                aladdinDocument.Image,
                options);

        List<System.Drawing.Color> palette =
            result.Palette
                .Take(16)
                .ToList();

        while (palette.Count < 16)
        {
            palette.Add(
                System.Drawing.Color.Black);
        }

        palette[0] =
            System.Drawing.Color.Transparent;

        return palette;
    }
}
