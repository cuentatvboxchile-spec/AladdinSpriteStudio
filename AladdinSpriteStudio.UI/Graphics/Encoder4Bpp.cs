namespace AladdinSpriteStudio.UI.Graphics;

/// <summary>
/// Convierte matrices de índices de color de 8 × 8
/// al formato gráfico planar 4BPP utilizado por SNES.
/// </summary>
public static class Encoder4Bpp
{
    public const int TileWidth = 8;
    public const int TileHeight = 8;
    public const int BytesPerTile = 32;

    /// <summary>
    /// Convierte un tile de 8 × 8 con índices de 0 a 15
    /// en 32 bytes SNES 4BPP.
    /// </summary>
    public static byte[] EncodeTile(
        byte[,] pixels)
    {
        ArgumentNullException.ThrowIfNull(pixels);

        if (pixels.GetLength(0) != TileHeight ||
            pixels.GetLength(1) != TileWidth)
        {
            throw new ArgumentException(
                "El tile debe ser una matriz de 8 × 8.",
                nameof(pixels));
        }

        byte[] encoded =
            new byte[BytesPerTile];

        for (int row = 0;
             row < TileHeight;
             row++)
        {
            byte plane0 = 0;
            byte plane1 = 0;
            byte plane2 = 0;
            byte plane3 = 0;

            for (int column = 0;
                 column < TileWidth;
                 column++)
            {
                byte colorIndex =
                    pixels[row, column];

                if (colorIndex > 15)
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(pixels),
                        $"El índice de color {colorIndex} " +
                        "es mayor que 15.");
                }

                int bitPosition =
                    7 - column;

                plane0 |=
                    (byte)(
                        (colorIndex & 0x01)
                        << bitPosition);

                plane1 |=
                    (byte)(
                        ((colorIndex >> 1) & 0x01)
                        << bitPosition);

                plane2 |=
                    (byte)(
                        ((colorIndex >> 2) & 0x01)
                        << bitPosition);

                plane3 |=
                    (byte)(
                        ((colorIndex >> 3) & 0x01)
                        << bitPosition);
            }

            int firstPlaneOffset =
                row * 2;

            int secondPlaneOffset =
                16 + row * 2;

            encoded[firstPlaneOffset] =
                plane0;

            encoded[firstPlaneOffset + 1] =
                plane1;

            encoded[secondPlaneOffset] =
                plane2;

            encoded[secondPlaneOffset + 1] =
                plane3;
        }

        return encoded;
    }
}