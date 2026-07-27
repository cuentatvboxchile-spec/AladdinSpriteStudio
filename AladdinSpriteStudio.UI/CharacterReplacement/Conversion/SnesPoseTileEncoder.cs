using AladdinSpriteStudio.UI.CharacterReplacement.Models;

namespace AladdinSpriteStudio.UI.CharacterReplacement.Conversion;

/// <summary>
/// Opciones para convertir una sola pose objetivo en tiles SNES 8 × 8.
/// Para pruebas de ROM se conservan también los tiles transparentes,
/// porque el bloque escrito debe tener un tamaño y orden predecibles.
/// </summary>
public sealed class SnesPoseTileEncodingOptions
{
    public bool PadToTileGrid { get; init; } = true;

    public int TileWidth { get; init; } = 8;

    public int TileHeight { get; init; } = 8;
}

/// <summary>
/// Resultado de convertir una pose en tiles SNES 4BPP.
/// </summary>
public sealed class SnesPoseTileEncodingResult
{
    private readonly List<byte[]> _tiles;

    internal SnesPoseTileEncodingResult(
        SpriteFrame frame,
        int paddedWidth,
        int paddedHeight,
        int tileColumns,
        int tileRows,
        IReadOnlyList<byte[]> tiles)
    {
        Frame =
            frame ??
            throw new ArgumentNullException(nameof(frame));

        PaddedWidth =
            paddedWidth;

        PaddedHeight =
            paddedHeight;

        TileColumns =
            tileColumns;

        TileRows =
            tileRows;

        _tiles =
            tiles
                .Select(tile => tile.ToArray())
                .ToList();
    }

    public SpriteFrame Frame { get; }

    public int PaddedWidth { get; }

    public int PaddedHeight { get; }

    public int TileColumns { get; }

    public int TileRows { get; }

    public IReadOnlyList<byte[]> Tiles =>
        _tiles;

    public int TileCount =>
        _tiles.Count;

    public int ByteCount =>
        TileCount * 32;

    public byte[] GetCombinedBytes()
    {
        byte[] result =
            new byte[ByteCount];

        int position =
            0;

        foreach (byte[] tile in _tiles)
        {
            if (tile.Length != 32)
            {
                throw new InvalidOperationException(
                    "Todos los tiles SNES 4BPP deben medir 32 bytes.");
            }

            Buffer.BlockCopy(
                tile,
                0,
                result,
                position,
                tile.Length);

            position += tile.Length;
        }

        return result;
    }

    /// <summary>
    /// Genera una vista previa de los tiles ya ordenados en la misma
    /// cuadrícula utilizada para la pose.
    /// </summary>
    public System.Drawing.Bitmap CreateIndexedPreview(
        IReadOnlyList<System.Drawing.Color> palette)
    {
        ArgumentNullException.ThrowIfNull(palette);

        if (palette.Count < 16)
        {
            throw new ArgumentException(
                "La paleta debe contener al menos 16 entradas.",
                nameof(palette));
        }

        System.Drawing.Bitmap preview =
            new(
                PaddedWidth,
                PaddedHeight,
                System.Drawing.Imaging.PixelFormat.Format32bppArgb);

        for (int tileIndex = 0;
             tileIndex < _tiles.Count;
             tileIndex++)
        {
            byte[] decoded =
                DecodeTile4Bpp(
                    _tiles[tileIndex]);

            int tileColumn =
                tileIndex % TileColumns;

            int tileRow =
                tileIndex / TileColumns;

            for (int y = 0; y < 8; y++)
            {
                for (int x = 0; x < 8; x++)
                {
                    int paletteIndex =
                        decoded[y * 8 + x];

                    System.Drawing.Color color =
                        palette[paletteIndex];

                    preview.SetPixel(
                        tileColumn * 8 + x,
                        tileRow * 8 + y,
                        paletteIndex == 0
                            ? System.Drawing.Color.Transparent
                            : System.Drawing.Color.FromArgb(
                                255,
                                color.R,
                                color.G,
                                color.B));
                }
            }
        }

        return preview;
    }

    private static byte[] DecodeTile4Bpp(
        IReadOnlyList<byte> tile)
    {
        if (tile.Count != 32)
        {
            throw new ArgumentException(
                "Un tile SNES 4BPP debe contener 32 bytes.",
                nameof(tile));
        }

        byte[] indices =
            new byte[64];

        for (int row = 0; row < 8; row++)
        {
            byte plane0 =
                tile[row * 2];

            byte plane1 =
                tile[row * 2 + 1];

            byte plane2 =
                tile[16 + row * 2];

            byte plane3 =
                tile[16 + row * 2 + 1];

            for (int column = 0; column < 8; column++)
            {
                int bit =
                    7 - column;

                byte value =
                    (byte)(
                        ((plane0 >> bit) & 1) |
                        (((plane1 >> bit) & 1) << 1) |
                        (((plane2 >> bit) & 1) << 2) |
                        (((plane3 >> bit) & 1) << 3));

                indices[row * 8 + column] =
                    value;
            }
        }

        return indices;
    }
}

/// <summary>
/// Convierte una región de la hoja cuantizada en un bloque consecutivo
/// de tiles SNES 4BPP.
/// </summary>
public static class SnesPoseTileEncoder
{
    public static SnesPoseTileEncodingResult Encode(
        SnesSpritePaletteQuantizationResult quantization,
        SpriteFrame frame,
        SnesPoseTileEncodingOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(quantization);
        ArgumentNullException.ThrowIfNull(frame);

        options ??=
            new SnesPoseTileEncodingOptions();

        if (options.TileWidth != 8 ||
            options.TileHeight != 8)
        {
            throw new NotSupportedException(
                "Esta versión solo admite tiles de 8 × 8.");
        }

        byte[,] indices =
            quantization.PixelIndices;

        int imageWidth =
            indices.GetLength(0);

        int imageHeight =
            indices.GetLength(1);

        System.Drawing.Rectangle bounds =
            System.Drawing.Rectangle.Intersect(
                new System.Drawing.Rectangle(
                    0,
                    0,
                    imageWidth,
                    imageHeight),
                frame.Bounds);

        if (bounds.Width <= 0 ||
            bounds.Height <= 0)
        {
            throw new InvalidOperationException(
                "La pose seleccionada no está dentro de la hoja.");
        }

        int paddedWidth =
            options.PadToTileGrid
                ? RoundUp(bounds.Width, 8)
                : bounds.Width;

        int paddedHeight =
            options.PadToTileGrid
                ? RoundUp(bounds.Height, 8)
                : bounds.Height;

        if (paddedWidth % 8 != 0 ||
            paddedHeight % 8 != 0)
        {
            throw new InvalidOperationException(
                "La región no es múltiplo de 8 y el relleno " +
                "a la cuadrícula está desactivado.");
        }

        int tileColumns =
            paddedWidth / 8;

        int tileRows =
            paddedHeight / 8;

        List<byte[]> tiles =
            new(tileColumns * tileRows);

        for (int tileRow = 0;
             tileRow < tileRows;
             tileRow++)
        {
            for (int tileColumn = 0;
                 tileColumn < tileColumns;
                 tileColumn++)
            {
                byte[] tileIndices =
                    ExtractTile(
                        indices,
                        bounds,
                        tileColumn,
                        tileRow);

                byte[] encoded =
                    Snes4BppTilePackageBuilder.EncodeTile4Bpp(
                        tileIndices);

                tiles.Add(encoded);
            }
        }

        return new SnesPoseTileEncodingResult(
            frame,
            paddedWidth,
            paddedHeight,
            tileColumns,
            tileRows,
            tiles);
    }

    private static byte[] ExtractTile(
        byte[,] indices,
        System.Drawing.Rectangle bounds,
        int tileColumn,
        int tileRow)
    {
        byte[] tile =
            new byte[64];

        for (int y = 0; y < 8; y++)
        {
            for (int x = 0; x < 8; x++)
            {
                int localX =
                    tileColumn * 8 + x;

                int localY =
                    tileRow * 8 + y;

                byte value =
                    0;

                if (localX < bounds.Width &&
                    localY < bounds.Height)
                {
                    value =
                        indices[
                            bounds.X + localX,
                            bounds.Y + localY];
                }

                if (value > 15)
                {
                    throw new InvalidOperationException(
                        "La imagen contiene un índice superior a 15.");
                }

                tile[y * 8 + x] =
                    value;
            }
        }

        return tile;
    }

    private static int RoundUp(
        int value,
        int multiple)
    {
        return
            (value + multiple - 1) /
            multiple *
            multiple;
    }
}
