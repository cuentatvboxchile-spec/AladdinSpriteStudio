using System.Security.Cryptography;

namespace AladdinSpriteStudio.UI.CharacterReplacement.RomTesting;

/// <summary>
/// Parámetros para realizar una modificación controlada en una copia
/// de la ROM. Nunca se permite escribir sobre el archivo original.
/// </summary>
public sealed class RomTilePatchOptions
{
    public required string OriginalRomPath { get; init; }

    public required string OutputRomPath { get; init; }

    /// <summary>
    /// Offset lógico mostrado al usuario. Si TreatOffsetAsHeaderless es
    /// verdadero y se detecta un encabezado de copiador de 0x200 bytes,
    /// el servicio lo suma automáticamente.
    /// </summary>
    public required int LogicalOffset { get; init; }

    public required byte[] PatchBytes { get; init; }

    public bool TreatOffsetAsHeaderless { get; init; } = true;

    public bool DetectCopierHeader { get; init; } = true;

    public bool RequirePatchLengthMultipleOf32 { get; init; } = true;

    public bool CreateSegmentBackup { get; init; } = true;

    public bool CreateManifest { get; init; } = true;
}

/// <summary>
/// Resultado de una prueba controlada de escritura.
/// </summary>
public sealed class RomTilePatchResult
{
    public required string OriginalRomPath { get; init; }

    public required string OutputRomPath { get; init; }

    public required int LogicalOffset { get; init; }

    public required int ActualFileOffset { get; init; }

    public required int PatchLength { get; init; }

    public required bool CopierHeaderDetected { get; init; }

    public required string OriginalRomSha256 { get; init; }

    public required string OutputRomSha256 { get; init; }

    public string? BackupPath { get; init; }

    public string? ManifestPath { get; init; }
}

/// <summary>
/// Crea una copia de la ROM y reemplaza un segmento concreto de bytes.
/// También respalda los bytes originales y registra hashes y offsets.
/// </summary>
public static class RomTilePatchService
{
    private const int CopierHeaderSize = 0x200;
    private const int SnesBankSize = 0x8000;

    public static RomTilePatchResult CreatePatchedCopy(
        RomTilePatchOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        ValidateOptions(options);

        string originalPath =
            Path.GetFullPath(options.OriginalRomPath);

        string outputPath =
            Path.GetFullPath(options.OutputRomPath);

        if (string.Equals(
                originalPath,
                outputPath,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "La ROM de salida debe ser distinta de la ROM original.");
        }

        byte[] originalRom =
            File.ReadAllBytes(originalPath);

        bool copierHeaderDetected =
            options.DetectCopierHeader &&
            HasCopierHeader(originalRom.Length);

        int actualOffset =
            options.LogicalOffset;

        if (options.TreatOffsetAsHeaderless &&
            copierHeaderDetected)
        {
            actualOffset +=
                CopierHeaderSize;
        }

        if (actualOffset < 0 ||
            actualOffset > originalRom.Length)
        {
            throw new ArgumentOutOfRangeException(
                nameof(options.LogicalOffset),
                "El offset está fuera de la ROM.");
        }

        if ((long)actualOffset +
                options.PatchBytes.Length >
            originalRom.Length)
        {
            throw new InvalidOperationException(
                "El bloque nuevo supera el final de la ROM.");
        }

        Directory.CreateDirectory(
            Path.GetDirectoryName(outputPath)
            ?? Environment.CurrentDirectory);

        byte[] originalSegment =
            new byte[options.PatchBytes.Length];

        Buffer.BlockCopy(
            originalRom,
            actualOffset,
            originalSegment,
            0,
            originalSegment.Length);

        byte[] patchedRom =
            originalRom.ToArray();

        Buffer.BlockCopy(
            options.PatchBytes,
            0,
            patchedRom,
            actualOffset,
            options.PatchBytes.Length);

        File.WriteAllBytes(
            outputPath,
            patchedRom);

        string? backupPath =
            null;

        if (options.CreateSegmentBackup)
        {
            backupPath =
                outputPath +
                $".offset_{actualOffset:X6}.original.bin";

            File.WriteAllBytes(
                backupPath,
                originalSegment);
        }

        string originalHash =
            ComputeSha256(originalRom);

        string outputHash =
            ComputeSha256(patchedRom);

        string? manifestPath =
            null;

        if (options.CreateManifest)
        {
            manifestPath =
                outputPath +
                ".patch_info.txt";

            File.WriteAllText(
                manifestPath,
                CreateManifestText(
                    options,
                    originalPath,
                    outputPath,
                    actualOffset,
                    copierHeaderDetected,
                    originalHash,
                    outputHash,
                    backupPath));
        }

        return new RomTilePatchResult
        {
            OriginalRomPath =
                originalPath,

            OutputRomPath =
                outputPath,

            LogicalOffset =
                options.LogicalOffset,

            ActualFileOffset =
                actualOffset,

            PatchLength =
                options.PatchBytes.Length,

            CopierHeaderDetected =
                copierHeaderDetected,

            OriginalRomSha256 =
                originalHash,

            OutputRomSha256 =
                outputHash,

            BackupPath =
                backupPath,

            ManifestPath =
                manifestPath
        };
    }

    public static bool HasCopierHeader(
        long romLength)
    {
        return romLength % SnesBankSize ==
               CopierHeaderSize;
    }

    private static void ValidateOptions(
        RomTilePatchOptions options)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            options.OriginalRomPath);

        ArgumentException.ThrowIfNullOrWhiteSpace(
            options.OutputRomPath);

        ArgumentNullException.ThrowIfNull(
            options.PatchBytes);

        if (!File.Exists(options.OriginalRomPath))
        {
            throw new FileNotFoundException(
                "No se encontró la ROM original.",
                options.OriginalRomPath);
        }

        if (options.LogicalOffset < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(options.LogicalOffset));
        }

        if (options.PatchBytes.Length == 0)
        {
            throw new ArgumentException(
                "El bloque de prueba está vacío.",
                nameof(options.PatchBytes));
        }

        if (options.RequirePatchLengthMultipleOf32 &&
            options.PatchBytes.Length % 32 != 0)
        {
            throw new ArgumentException(
                "El bloque debe medir un múltiplo de 32 bytes, " +
                "porque cada tile SNES 4BPP ocupa 32 bytes.",
                nameof(options.PatchBytes));
        }
    }

    private static string ComputeSha256(
        byte[] data)
    {
        byte[] hash =
            SHA256.HashData(data);

        return Convert.ToHexString(hash);
    }

    private static string CreateManifestText(
        RomTilePatchOptions options,
        string originalPath,
        string outputPath,
        int actualOffset,
        bool copierHeaderDetected,
        string originalHash,
        string outputHash,
        string? backupPath)
    {
        List<string> lines =
        [
            "ALADDIN SPRITE STUDIO",
            "PRUEBA CONTROLADA DE TILES EN ROM",
            "",
            $"Fecha: {DateTime.Now:yyyy-MM-dd HH:mm:ss}",
            $"ROM original: {originalPath}",
            $"ROM de prueba: {outputPath}",
            $"Offset lógico: 0x{options.LogicalOffset:X6}",
            $"Offset real de archivo: 0x{actualOffset:X6}",
            $"Encabezado de copiador detectado: " +
            $"{(copierHeaderDetected ? "sí" : "no")}",
            $"Longitud reemplazada: {options.PatchBytes.Length} bytes",
            $"Cantidad equivalente de tiles: " +
            $"{options.PatchBytes.Length / 32}",
            $"Respaldo del segmento: {backupPath ?? "no creado"}",
            "",
            $"SHA-256 original: {originalHash}",
            $"SHA-256 salida: {outputHash}",
            "",
            "ADVERTENCIA",
            "-----------",
            "Este archivo solo registra una escritura controlada. " +
            "No confirma que el offset corresponda a la pose elegida, " +
            "que el juego utilice la misma paleta ni que los gráficos " +
            "estén sin compresión. Pruebe únicamente la copia generada."
        ];

        return string.Join(
            Environment.NewLine,
            lines);
    }
}
