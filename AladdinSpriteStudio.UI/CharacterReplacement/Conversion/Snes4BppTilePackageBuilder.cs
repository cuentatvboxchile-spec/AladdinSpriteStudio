using AladdinSpriteStudio.UI.CharacterReplacement.Models;

namespace AladdinSpriteStudio.UI.CharacterReplacement.Conversion;

public sealed class Snes4BppTilePackageOptions
{
    public bool RequireAllFramesMapped { get; init; } = true;
    public bool SkipTransparentTiles { get; init; } = true;
    public bool DeduplicateExactTiles { get; init; } = true;
    public bool PadFramesToTileGrid { get; init; } = true;
}

public sealed class Snes4BppTileReference
{
    public required int TileColumn { get; init; }
    public required int TileRow { get; init; }

    /// <summary>
    /// Índice dentro del bloque global de tiles. -1 significa que el
    /// tile era completamente transparente y fue omitido.
    /// </summary>
    public required int UniqueTileIndex { get; init; }

    public required bool IsTransparent { get; init; }
}

public sealed class Snes4BppFrameRecord
{
    public required int TargetFrameIndex { get; init; }
    public required string TargetFrameName { get; init; }
    public required System.Drawing.Rectangle OriginalBounds { get; init; }
    public required int PaddedWidth { get; init; }
    public required int PaddedHeight { get; init; }
    public required int TileColumns { get; init; }
    public required int TileRows { get; init; }
    public required bool IsMapped { get; init; }

    public required IReadOnlyList<Snes4BppTileReference> Tiles
    {
        get;
        init;
    }
}

public sealed class Snes4BppTilePackageResult
{
    private readonly List<byte[]> _uniqueTiles;
    private readonly List<Snes4BppFrameRecord> _frames;

    internal Snes4BppTilePackageResult(
        IReadOnlyList<byte[]> uniqueTiles,
        IReadOnlyList<Snes4BppFrameRecord> frames,
        int transparentReferences,
        int duplicateReferences,
        int sourceWidth,
        int sourceHeight)
    {
        ArgumentNullException.ThrowIfNull(uniqueTiles);
        ArgumentNullException.ThrowIfNull(frames);

        _uniqueTiles =
            uniqueTiles
                .Select(tile => tile.ToArray())
                .ToList();

        _frames =
            frames.ToList();

        TransparentTileReferenceCount =
            transparentReferences;

        DuplicateTileReferenceCount =
            duplicateReferences;

        SourceWidth =
            sourceWidth;

        SourceHeight =
            sourceHeight;
    }

    public IReadOnlyList<byte[]> UniqueTiles => _uniqueTiles;
    public IReadOnlyList<Snes4BppFrameRecord> Frames => _frames;

    public int SourceWidth { get; }
    public int SourceHeight { get; }
    public int TransparentTileReferenceCount { get; }
    public int DuplicateTileReferenceCount { get; }

    public int UniqueTileCount =>
        _uniqueTiles.Count;

    public int TotalTileReferenceCount =>
        _frames.Sum(frame => frame.Tiles.Count);

    public int TileDataByteCount =>
        UniqueTileCount * 32;

    public byte[] GetCombined4BppBytes()
    {
        byte[] result =
            new byte[TileDataByteCount];

        int position = 0;

        foreach (byte[] tile in _uniqueTiles)
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

    public string ExportPackage(
        string parentDirectory,
        string baseName,
        byte[]? paletteBgr555 = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(parentDirectory);
        ArgumentException.ThrowIfNullOrWhiteSpace(baseName);

        string safeName =
            SanitizeFileName(baseName);

        string outputDirectory =
            Path.Combine(
                parentDirectory,
                safeName + "_Tiles4BPP");

        Directory.CreateDirectory(outputDirectory);

        File.WriteAllBytes(
            Path.Combine(
                outputDirectory,
                safeName + "_tiles_4bpp.bin"),
            GetCombined4BppBytes());

        File.WriteAllLines(
            Path.Combine(
                outputDirectory,
                safeName + "_mapa_poses.csv"),
            CreateFrameMapCsv());

        File.WriteAllLines(
            Path.Combine(
                outputDirectory,
                safeName + "_mapa_tiles.csv"),
            CreateTileMapCsv());

        File.WriteAllText(
            Path.Combine(
                outputDirectory,
                safeName + "_informe_tiles.txt"),
            CreateTextReport());

        if (paletteBgr555 is not null)
        {
            if (paletteBgr555.Length != 32)
            {
                throw new ArgumentException(
                    "La paleta SNES debe contener exactamente 32 bytes.",
                    nameof(paletteBgr555));
            }

            File.WriteAllBytes(
                Path.Combine(
                    outputDirectory,
                    safeName + "_paleta_bgr555.bin"),
                paletteBgr555);
        }

        return outputDirectory;
    }

    public string CreateTextReport()
    {
        List<string> lines =
        [
            "ALADDIN SPRITE STUDIO",
            "PAQUETE TÉCNICO DE TILES SNES 4BPP",
            "",
            $"Fecha: {DateTime.Now:yyyy-MM-dd HH:mm:ss}",
            $"Hoja fuente: {SourceWidth} × {SourceHeight}",
            $"Poses procesadas: {Frames.Count}",
            $"Referencias totales: {TotalTileReferenceCount}",
            $"Tiles transparentes omitidos: " +
            $"{TransparentTileReferenceCount}",
            $"Referencias duplicadas reutilizadas: " +
            $"{DuplicateTileReferenceCount}",
            $"Tiles únicos: {UniqueTileCount}",
            $"Tamaño del bloque 4BPP: {TileDataByteCount:N0} bytes",
            "",
            "ADVERTENCIA",
            "-----------",
            "El archivo generado contiene tiles SNES 8 × 8 y datos " +
            "planar 4BPP válidos. Todavía no demuestra que el orden de " +
            "tiles coincida con el usado por Aladdin. Antes de insertar " +
            "datos deben identificarse las tablas de animación, OAM, " +
            "paletas, índices y posible compresión de la ROM.",
            "",
            "POSES",
            "-----"
        ];

        foreach (Snes4BppFrameRecord frame
                 in _frames.OrderBy(item => item.TargetFrameIndex))
        {
            lines.Add(
                $"Pose {frame.TargetFrameIndex + 1:000}: " +
                $"{frame.OriginalBounds.Width} × " +
                $"{frame.OriginalBounds.Height} px, " +
                $"{frame.TileColumns} × {frame.TileRows} tiles, " +
                $"asignada: {(frame.IsMapped ? "sí" : "no")}");
        }

        return string.Join(Environment.NewLine, lines);
    }

    private IEnumerable<string> CreateFrameMapCsv()
    {
        yield return
            "pose_indice,pose_numero,nombre,x,y,ancho,alto," +
            "ancho_relleno,alto_relleno,columnas_tiles,filas_tiles,asignada";

        foreach (Snes4BppFrameRecord frame
                 in _frames.OrderBy(item => item.TargetFrameIndex))
        {
            yield return string.Join(
                ",",
                frame.TargetFrameIndex,
                frame.TargetFrameIndex + 1,
                EscapeCsv(frame.TargetFrameName),
                frame.OriginalBounds.X,
                frame.OriginalBounds.Y,
                frame.OriginalBounds.Width,
                frame.OriginalBounds.Height,
                frame.PaddedWidth,
                frame.PaddedHeight,
                frame.TileColumns,
                frame.TileRows,
                frame.IsMapped ? "1" : "0");
        }
    }

    private IEnumerable<string> CreateTileMapCsv()
    {
        yield return
            "pose_indice,pose_numero,columna_tile,fila_tile," +
            "indice_tile_unico,transparente";

        foreach (Snes4BppFrameRecord frame
                 in _frames.OrderBy(item => item.TargetFrameIndex))
        {
            foreach (Snes4BppTileReference tile
                     in frame.Tiles
                         .OrderBy(item => item.TileRow)
                         .ThenBy(item => item.TileColumn))
            {
                yield return string.Join(
                    ",",
                    frame.TargetFrameIndex,
                    frame.TargetFrameIndex + 1,
                    tile.TileColumn,
                    tile.TileRow,
                    tile.UniqueTileIndex,
                    tile.IsTransparent ? "1" : "0");
            }
        }
    }

    private static string EscapeCsv(string value)
    {
        value ??= string.Empty;

        if (!value.Contains(',') &&
            !value.Contains('"') &&
            !value.Contains('\n') &&
            !value.Contains('\r'))
        {
            return value;
        }

        return "\"" +
               value.Replace("\"", "\"\"") +
               "\"";
    }

    private static string SanitizeFileName(string value)
    {
        char[] invalid =
            Path.GetInvalidFileNameChars();

        string result =
            new(
                value
                    .Select(character =>
                        invalid.Contains(character)
                            ? '_'
                            : character)
                    .ToArray());

        result = result.Trim();

        return string.IsNullOrWhiteSpace(result)
            ? "personaje_snes"
            : result;
    }
}

public static class Snes4BppTilePackageBuilder
{
    public static Snes4BppTilePackageResult Build(
        SnesSpritePaletteQuantizationResult quantization,
        CharacterReplacementProject project,
        SpriteSheetDocument targetDocument,
        Snes4BppTilePackageOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(quantization);
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(targetDocument);

        options ??= new Snes4BppTilePackageOptions();

        byte[,] sourceIndices =
            quantization.PixelIndices;

        int sourceWidth =
            sourceIndices.GetLength(0);

        int sourceHeight =
            sourceIndices.GetLength(1);

        if (sourceWidth != targetDocument.Width ||
            sourceHeight != targetDocument.Height)
        {
            throw new InvalidOperationException(
                "La matriz cuantizada no coincide con la hoja objetivo.");
        }

        int pendingCount =
            project.GetUnmappedTargetCount();

        if (options.RequireAllFramesMapped &&
            pendingCount > 0)
        {
            throw new InvalidOperationException(
                $"No se puede crear el paquete final porque quedan " +
                $"{pendingCount} poses sin asignar.");
        }

        List<byte[]> uniqueTiles = new();

        Dictionary<string, int> tileIndexes =
            new(StringComparer.Ordinal);

        List<Snes4BppFrameRecord> frameRecords = new();

        int transparentReferences = 0;
        int duplicateReferences = 0;

        foreach (SpriteFrame frame
                 in targetDocument.Frames
                     .OrderBy(item => item.Index))
        {
            System.Drawing.Rectangle bounds =
                System.Drawing.Rectangle.Intersect(
                    new System.Drawing.Rectangle(
                        0,
                        0,
                        sourceWidth,
                        sourceHeight),
                    frame.Bounds);

            if (bounds.Width <= 0 ||
                bounds.Height <= 0)
            {
                throw new InvalidOperationException(
                    $"La pose {frame.Index + 1:000} tiene un " +
                    "rectángulo inválido.");
            }

            int paddedWidth =
                options.PadFramesToTileGrid
                    ? RoundUp(bounds.Width, 8)
                    : bounds.Width;

            int paddedHeight =
                options.PadFramesToTileGrid
                    ? RoundUp(bounds.Height, 8)
                    : bounds.Height;

            if (paddedWidth % 8 != 0 ||
                paddedHeight % 8 != 0)
            {
                throw new InvalidOperationException(
                    $"La pose {frame.Index + 1:000} no es múltiplo " +
                    "de 8 y el relleno está desactivado.");
            }

            int columns =
                paddedWidth / 8;

            int rows =
                paddedHeight / 8;

            List<Snes4BppTileReference> references =
                new(columns * rows);

            for (int tileRow = 0;
                 tileRow < rows;
                 tileRow++)
            {
                for (int tileColumn = 0;
                     tileColumn < columns;
                     tileColumn++)
                {
                    byte[] tileIndices =
                        ExtractTileIndices(
                            sourceIndices,
                            bounds,
                            tileColumn,
                            tileRow);

                    bool transparent =
                        tileIndices.All(value => value == 0);

                    if (transparent &&
                        options.SkipTransparentTiles)
                    {
                        transparentReferences++;

                        references.Add(
                            new Snes4BppTileReference
                            {
                                TileColumn = tileColumn,
                                TileRow = tileRow,
                                UniqueTileIndex = -1,
                                IsTransparent = true
                            });

                        continue;
                    }

                    ValidateTileIndices(
                        tileIndices,
                        frame.Index,
                        tileColumn,
                        tileRow);

                    byte[] encoded =
                        EncodeTile4Bpp(tileIndices);

                    int uniqueIndex;

                    if (options.DeduplicateExactTiles)
                    {
                        string key =
                            Convert.ToHexString(tileIndices);

                        if (tileIndexes.TryGetValue(
                                key,
                                out int existingIndex))
                        {
                            uniqueIndex = existingIndex;
                            duplicateReferences++;
                        }
                        else
                        {
                            uniqueIndex = uniqueTiles.Count;
                            uniqueTiles.Add(encoded);
                            tileIndexes[key] = uniqueIndex;
                        }
                    }
                    else
                    {
                        uniqueIndex = uniqueTiles.Count;
                        uniqueTiles.Add(encoded);
                    }

                    references.Add(
                        new Snes4BppTileReference
                        {
                            TileColumn = tileColumn,
                            TileRow = tileRow,
                            UniqueTileIndex = uniqueIndex,
                            IsTransparent = transparent
                        });
                }
            }

            frameRecords.Add(
                new Snes4BppFrameRecord
                {
                    TargetFrameIndex = frame.Index,
                    TargetFrameName = frame.Name,
                    OriginalBounds = frame.Bounds,
                    PaddedWidth = paddedWidth,
                    PaddedHeight = paddedHeight,
                    TileColumns = columns,
                    TileRows = rows,
                    IsMapped =
                        project.GetMapping(frame) is not null,
                    Tiles = references
                });
        }

        return new Snes4BppTilePackageResult(
            uniqueTiles,
            frameRecords,
            transparentReferences,
            duplicateReferences,
            sourceWidth,
            sourceHeight);
    }

    /// <summary>
    /// Codifica 64 índices (8 × 8) en los 32 bytes planares de SNES 4BPP.
    /// </summary>
    public static byte[] EncodeTile4Bpp(
        IReadOnlyList<byte> tileIndices)
    {
        ArgumentNullException.ThrowIfNull(tileIndices);

        if (tileIndices.Count != 64)
        {
            throw new ArgumentException(
                "Un tile 8 × 8 debe contener exactamente 64 índices.",
                nameof(tileIndices));
        }

        byte[] result = new byte[32];

        for (int row = 0;
             row < 8;
             row++)
        {
            byte plane0 = 0;
            byte plane1 = 0;
            byte plane2 = 0;
            byte plane3 = 0;

            for (int column = 0;
                 column < 8;
                 column++)
            {
                byte value =
                    tileIndices[row * 8 + column];

                if (value > 15)
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(tileIndices),
                        "Los índices 4BPP deben estar entre 0 y 15.");
                }

                int bit = 7 - column;

                plane0 |=
                    (byte)((value & 0x01) << bit);

                plane1 |=
                    (byte)(((value >> 1) & 0x01) << bit);

                plane2 |=
                    (byte)(((value >> 2) & 0x01) << bit);

                plane3 |=
                    (byte)(((value >> 3) & 0x01) << bit);
            }

            result[row * 2] = plane0;
            result[row * 2 + 1] = plane1;
            result[16 + row * 2] = plane2;
            result[16 + row * 2 + 1] = plane3;
        }

        return result;
    }

    private static byte[] ExtractTileIndices(
        byte[,] sourceIndices,
        System.Drawing.Rectangle frameBounds,
        int tileColumn,
        int tileRow)
    {
        byte[] tile = new byte[64];

        for (int localY = 0;
             localY < 8;
             localY++)
        {
            for (int localX = 0;
                 localX < 8;
                 localX++)
            {
                int frameX =
                    tileColumn * 8 + localX;

                int frameY =
                    tileRow * 8 + localY;

                byte value = 0;

                if (frameX < frameBounds.Width &&
                    frameY < frameBounds.Height)
                {
                    value =
                        sourceIndices[
                            frameBounds.X + frameX,
                            frameBounds.Y + frameY];
                }

                tile[localY * 8 + localX] = value;
            }
        }

        return tile;
    }

    private static void ValidateTileIndices(
        IReadOnlyList<byte> tileIndices,
        int frameIndex,
        int tileColumn,
        int tileRow)
    {
        byte maximum =
            tileIndices.Max();

        if (maximum <= 15)
        {
            return;
        }

        throw new InvalidOperationException(
            $"La pose {frameIndex + 1:000}, tile " +
            $"({tileColumn}, {tileRow}), contiene el índice " +
            $"{maximum}; 4BPP solo admite 0–15.");
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
