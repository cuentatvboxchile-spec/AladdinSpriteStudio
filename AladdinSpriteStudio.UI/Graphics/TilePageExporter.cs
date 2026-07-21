using System.Drawing.Imaging;

namespace AladdinSpriteStudio.UI.Graphics;

/// <summary>
/// Exporta una página de tiles SNES 4BPP
/// como imagen PNG y datos binarios originales.
/// </summary>
public static class TilePageExporter
{
    private const int TileWidth = 8;
    private const int TileHeight = 8;

    public const int TilesPerRow = 16;
    public const int TilesPerColumn = 16;

    public const int TilesPerPage =
        TilesPerRow * TilesPerColumn;

    public const int BytesPerPage =
        TilesPerPage *
        Decoder4Bpp.BytesPerTile;

    /// <summary>
    /// Calcula cuántos tiles pueden exportarse desde una posición.
    /// Puede limitarse mediante un offset final exclusivo.
    /// </summary>
    public static int GetTileCountForPage(
        byte[] romData,
        int startOffset,
        int? endOffsetExclusive = null)
    {
        ArgumentNullException.ThrowIfNull(romData);

        int effectiveEnd =
            endOffsetExclusive.HasValue
                ? Math.Min(
                    endOffsetExclusive.Value,
                    romData.Length)
                : romData.Length;

        if (startOffset < 0 ||
            startOffset >= effectiveEnd)
        {
            return 0;
        }

        int availableBytes =
            effectiveEnd - startOffset;

        int availableTiles =
            availableBytes /
            Decoder4Bpp.BytesPerTile;

        return Math.Min(
            availableTiles,
            TilesPerPage);
    }

    /// <summary>
    /// Exporta una página de hasta 256 tiles.
    /// El offset final, cuando se proporciona, no se incluye.
    /// </summary>
    public static int ExportPage(
        byte[] romData,
        int startOffset,
        Color[] palette,
        string pngPath,
        string binPath,
        int? endOffsetExclusive = null)
    {
        ArgumentNullException.ThrowIfNull(romData);
        ArgumentNullException.ThrowIfNull(palette);

        if (string.IsNullOrWhiteSpace(pngPath))
        {
            throw new ArgumentException(
                "Debe indicar la ruta del archivo PNG.",
                nameof(pngPath));
        }

        if (string.IsNullOrWhiteSpace(binPath))
        {
            throw new ArgumentException(
                "Debe indicar la ruta del archivo BIN.",
                nameof(binPath));
        }

        if (palette.Length !=
            SnesPalette.ColorsPerPalette)
        {
            throw new ArgumentException(
                "La paleta debe contener exactamente 16 colores.",
                nameof(palette));
        }

        if (startOffset < 0 ||
            startOffset %
            Decoder4Bpp.BytesPerTile != 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(startOffset),
                "El offset inicial debe ser positivo " +
                "y estar alineado a 32 bytes.");
        }

        if (endOffsetExclusive.HasValue)
        {
            if (endOffsetExclusive.Value <=
                startOffset)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(endOffsetExclusive),
                    "El offset final debe ser mayor " +
                    "que el offset inicial.");
            }

            if (endOffsetExclusive.Value >
                romData.Length)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(endOffsetExclusive),
                    "El offset final supera el tamaño de la ROM.");
            }
        }

        int tileCount =
            GetTileCountForPage(
                romData,
                startOffset,
                endOffsetExclusive);

        if (tileCount <= 0)
        {
            throw new InvalidOperationException(
                "No hay tiles válidos para exportar " +
                "desde el offset indicado.");
        }

        int imageWidth =
            TilesPerRow * TileWidth;

        int requiredRows =
            (tileCount +
             TilesPerRow - 1) /
            TilesPerRow;

        int imageHeight =
            requiredRows * TileHeight;

        using Bitmap bitmap = new(
            imageWidth,
            imageHeight,
            PixelFormat.Format32bppArgb);

        using (System.Drawing.Graphics graphics =
               System.Drawing.Graphics.FromImage(
                   bitmap))
        {
            graphics.Clear(
                Color.Transparent);
        }

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
                tileIndex %
                TilesPerRow;

            int tileRow =
                tileIndex /
                TilesPerRow;

            int tileX =
                tileColumn *
                TileWidth;

            int tileY =
                tileRow *
                TileHeight;

            DrawTile(
                bitmap,
                pixels,
                palette,
                tileX,
                tileY);
        }

        CreateParentDirectory(
            pngPath);

        CreateParentDirectory(
            binPath);

        bitmap.Save(
            pngPath,
            ImageFormat.Png);

        int byteCount =
            tileCount *
            Decoder4Bpp.BytesPerTile;

        byte[] rawData =
            romData
                .AsSpan(
                    startOffset,
                    byteCount)
                .ToArray();

        File.WriteAllBytes(
            binPath,
            rawData);

        return tileCount;
    }

    private static void DrawTile(
        Bitmap bitmap,
        byte[,] pixels,
        Color[] palette,
        int tileX,
        int tileY)
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

                bitmap.SetPixel(
                    tileX + column,
                    tileY + row,
                    color);
            }
        }
    }

    private static void CreateParentDirectory(
        string filePath)
    {
        string? directory =
            Path.GetDirectoryName(
                filePath);

        if (!string.IsNullOrWhiteSpace(
                directory))
        {
            Directory.CreateDirectory(
                directory);
        }
    }
}