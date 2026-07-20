namespace AladdinSpriteStudio.UI.Graphics;

/// <summary>
/// Decodifica tiles gráficos de Super Nintendo
/// almacenados en formato planar de 4 bits por píxel.
/// </summary>
public static class Decoder4Bpp
{
    public const int TileWidth = 8;
    public const int TileHeight = 8;
    public const int BytesPerTile = 32;

    /// <summary>
    /// Decodifica un tile SNES 4BPP de 32 bytes.
    /// Devuelve una matriz de índices de color entre 0 y 15.
    /// </summary>
    public static byte[,] DecodeTile(ReadOnlySpan<byte> tileData)
    {
        if (tileData.Length < BytesPerTile)
        {
            throw new ArgumentException(
                $"Un tile 4BPP necesita al menos {BytesPerTile} bytes.",
                nameof(tileData));
        }

        byte[,] pixels = new byte[TileHeight, TileWidth];

        for (int row = 0; row < TileHeight; row++)
        {
            int rowOffset = row * 2;

            byte plane0 = tileData[rowOffset];
            byte plane1 = tileData[rowOffset + 1];
            byte plane2 = tileData[16 + rowOffset];
            byte plane3 = tileData[16 + rowOffset + 1];

            for (int column = 0; column < TileWidth; column++)
            {
                int bitPosition = 7 - column;

                int bit0 = (plane0 >> bitPosition) & 1;
                int bit1 = (plane1 >> bitPosition) & 1;
                int bit2 = (plane2 >> bitPosition) & 1;
                int bit3 = (plane3 >> bitPosition) & 1;

                byte colorIndex = (byte)(
                    bit0 |
                    (bit1 << 1) |
                    (bit2 << 2) |
                    (bit3 << 3));

                pixels[row, column] = colorIndex;
            }
        }

        return pixels;
    }

    /// <summary>
    /// Decodifica un tile ubicado dentro de un arreglo mayor,
    /// como los datos completos de una ROM.
    /// </summary>
    public static byte[,] DecodeTile(byte[] data, int offset)
    {
        ArgumentNullException.ThrowIfNull(data);

        if (offset < 0 ||
            offset > data.Length - BytesPerTile)
        {
            throw new ArgumentOutOfRangeException(
                nameof(offset),
                "No existen 32 bytes disponibles desde el offset indicado.");
        }

        return DecodeTile(
            data.AsSpan(offset, BytesPerTile));
    }
    

#if DEBUG
    /// <summary>
    /// Comprueba que los cuatro planos produzcan
    /// correctamente los índices 1, 2, 4 y 8.
    /// </summary>
    public static bool RunSelfTest()
    {
        byte[] tileData = new byte[BytesPerTile];

        // Primer píxel: plano 0 activo = índice 1.
        tileData[0] = 0b1000_0000;

        // Segundo píxel: plano 1 activo = índice 2.
        tileData[1] = 0b0100_0000;

        // Tercer píxel: plano 2 activo = índice 4.
        tileData[16] = 0b0010_0000;

        // Cuarto píxel: plano 3 activo = índice 8.
        tileData[17] = 0b0001_0000;

        byte[,] pixels = DecodeTile(tileData);

        return
            pixels[0, 0] == 1 &&
            pixels[0, 1] == 2 &&
            pixels[0, 2] == 4 &&
            pixels[0, 3] == 8 &&
            pixels[0, 4] == 0;
    }
#endif
}
