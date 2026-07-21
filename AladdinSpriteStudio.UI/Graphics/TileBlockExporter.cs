using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace AladdinSpriteStudio.UI.Graphics;

/// <summary>
/// Exporta bloques de tiles SNES 4BPP como una hoja PNG
/// y como una copia binaria de los datos originales.
/// </summary>
public static class TileBlockExporter
{
    private const int TileWidth = 8;
    private const int TileHeight = 8;

    /// <summary>
    /// Exporta un rango de la ROM como una hoja de tiles.
    /// El offset final no está incluido.
    /// </summary>
    public static int ExportPngAndRaw(
        byte[] romData,
        int startOffset,
        int endOffsetExclusive,
        Color[] palette,
        string pngFilePath,
        int columns = 16,
        int scale = 4,
        bool transparentColorZero = true)
    {
        ArgumentNullException.ThrowIfNull(romData);
        ArgumentNullException.ThrowIfNull(palette);

        if (string.IsNullOrWhiteSpace(pngFilePath))
        {
            throw new ArgumentException(
                "Debe indicar la ruta del archivo PNG.",
                nameof(pngFilePath));
        }

        if (palette.Length !=
            SnesPalette.ColorsPerPalette)
        {
            throw new ArgumentException(
                "La paleta debe contener exactamente 16 colores.",
                nameof(palette));
        }

        if (startOffset < 0 ||
            endOffsetExclusive > romData.Length ||
            endOffsetExclusive <= startOffset)
        {
            throw new ArgumentOutOfRangeException(
                nameof(startOffset),
                "El rango indicado no es válido para esta ROM.");
        }

        if (startOffset %
            Decoder4Bpp.BytesPerTile != 0)
        {
            throw new ArgumentException(
                "El offset inicial debe estar alineado a 32 bytes.",
                nameof(startOffset));
        }

        int byteCount =
            endOffsetExclusive - startOffset;

        if (byteCount %
            Decoder4Bpp.BytesPerTile != 0)
        {
            throw new ArgumentException(
                "El tamaño del bloque debe ser múltiplo de 32 bytes.",
                nameof(endOffsetExclusive));
        }

        if (columns is < 1 or > 128)
        {
            throw new ArgumentOutOfRangeException(
                nameof(columns),
                "Las columnas deben estar entre 1 y 128.");
        }

        if (scale is < 1 or > 16)
        {
            throw new ArgumentOutOfRangeException(
                nameof(scale),
                "La escala debe estar entre 1 y 16.");
        }

        int tileCount =
            byteCount /
            Decoder4Bpp.BytesPerTile;

        int rows =
            (tileCount + columns - 1) /
            columns;

        int nativeWidth =
            columns * TileWidth;

        int nativeHeight =
            rows * TileHeight;

        using Bitmap nativeBitmap =
            new(
                nativeWidth,
                nativeHeight,
                PixelFormat.Format32bppArgb);

        nativeBitmap.MakeTransparent();

        for (int tileIndex = 0;
             tileIndex < tileCount;
             tileIndex++)
        {
            int tileOffset =
                startOffset +
                tileIndex *
                Decoder4Bpp.BytesPerTile;

            byte[,] pixels =
                Decoder4Bpp.DecodeTile(
                    romData,
                    tileOffset);

            int tileColumn =
                tileIndex % columns;

            int tileRow =
                tileIndex / columns;

            int tileX =
                tileColumn * TileWidth;

            int tileY =
                tileRow * TileHeight;

            DrawTile(
                nativeBitmap,
                pixels,
                palette,
                tileX,
                tileY,
                transparentColorZero);
        }

        int outputWidth =
            nativeWidth * scale;

        int outputHeight =
            nativeHeight * scale;

        using Bitmap outputBitmap =
            new(
                outputWidth,
                outputHeight,
                PixelFormat.Format32bppArgb);

        using (System.Drawing.Graphics graphics =
               System.Drawing.Graphics.FromImage(
                   outputBitmap))
        {
            graphics.Clear(
                Color.Transparent);

            graphics.CompositingMode =
                CompositingMode.SourceCopy;

            graphics.CompositingQuality =
                CompositingQuality.HighSpeed;

            graphics.InterpolationMode =
                InterpolationMode.NearestNeighbor;

            graphics.PixelOffsetMode =
                PixelOffsetMode.Half;

            graphics.SmoothingMode =
                SmoothingMode.None;

            graphics.DrawImage(
                nativeBitmap,
                new Rectangle(
                    0,
                    0,
                    outputWidth,
                    outputHeight),
                new Rectangle(
                    0,
                    0,
                    nativeWidth,
                    nativeHeight),
                GraphicsUnit.Pixel);
        }

        string? directory =
            Path.GetDirectoryName(
                pngFilePath);

        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(
                directory);
        }

        outputBitmap.Save(
            pngFilePath,
            ImageFormat.Png);

        string binaryFilePath =
            Path.ChangeExtension(
                pngFilePath,
                ".bin");

        byte[] rawData =
            romData
                .AsSpan(
                    startOffset,
                    byteCount)
                .ToArray();

        File.WriteAllBytes(
            binaryFilePath,
            rawData);

        return tileCount;
    }

    private static void DrawTile(
        Bitmap bitmap,
        byte[,] pixels,
        Color[] palette,
        int tileX,
        int tileY,
        bool transparentColorZero)
    {
        for (int row = 0;
             row < TileHeight;
             row++)
        {
            for (int column = 0;
                 column < TileWidth;
                 column++)
            {
                byte colorIndex =
                    pixels[row, column];

                int safeColorIndex =
                    Math.Min(
                        colorIndex,
                        (byte)15);

                Color color =
                    palette[safeColorIndex];

                if (transparentColorZero &&
                    safeColorIndex == 0)
                {
                    color =
                        Color.FromArgb(
                            0,
                            color);
                }

                bitmap.SetPixel(
                    tileX + column,
                    tileY + row,
                    color);
            }
        }
    }
}