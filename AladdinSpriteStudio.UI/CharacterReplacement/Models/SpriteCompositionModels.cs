namespace AladdinSpriteStudio.UI.CharacterReplacement.Models;

/// <summary>
/// Define una pieza lógica de una pose. Una pieza puede representar uno
/// o varios tiles 8 × 8 colocados por la rutina del juego.
/// </summary>
public sealed class SpritePieceDefinition
{
    public string Id { get; set; } =
        Guid.NewGuid().ToString("N");

    public string Name { get; set; } =
        string.Empty;

    /// <summary>
    /// Tile inicial dentro del recurso gráfico del personaje nuevo.
    /// </summary>
    public int SourceTileIndex { get; set; }

    /// <summary>
    /// Tile que la ROM o la composición original espera utilizar.
    /// Puede permanecer nulo durante la investigación.
    /// </summary>
    public int? DestinationTileIndex { get; set; }

    /// <summary>
    /// Offset lógico opcional dentro de la ROM, sin encabezado de copiador.
    /// </summary>
    public int? RomLogicalOffset { get; set; }

    /// <summary>
    /// Índice opcional del tile dentro de VRAM cuando se conozca.
    /// </summary>
    public int? VramTileIndex { get; set; }

    public int X { get; set; }

    public int Y { get; set; }

    public int WidthInTiles { get; set; } = 1;

    public int HeightInTiles { get; set; } = 1;

    public bool FlipHorizontal { get; set; }

    public bool FlipVertical { get; set; }

    public int PaletteIndex { get; set; }

    public int Priority { get; set; }

    public bool IsEnabled { get; set; } = true;

    public int PixelWidth =>
        checked(
            WidthInTiles *
            8);

    public int PixelHeight =>
        checked(
            HeightInTiles *
            8);

    public IReadOnlyList<string> Validate()
    {
        List<string> errors =
            new();

        if (string.IsNullOrWhiteSpace(Id))
        {
            errors.Add(
                "La pieza no tiene identificador.");
        }

        if (SourceTileIndex < 0)
        {
            errors.Add(
                "SourceTileIndex no puede ser negativo.");
        }

        if (DestinationTileIndex < 0)
        {
            errors.Add(
                "DestinationTileIndex no puede ser negativo.");
        }

        if (RomLogicalOffset < 0)
        {
            errors.Add(
                "RomLogicalOffset no puede ser negativo.");
        }

        if (VramTileIndex < 0)
        {
            errors.Add(
                "VramTileIndex no puede ser negativo.");
        }

        if (WidthInTiles <= 0)
        {
            errors.Add(
                "WidthInTiles debe ser mayor que cero.");
        }

        if (HeightInTiles <= 0)
        {
            errors.Add(
                "HeightInTiles debe ser mayor que cero.");
        }

        if (PaletteIndex < 0)
        {
            errors.Add(
                "PaletteIndex no puede ser negativo.");
        }

        if (Priority < 0)
        {
            errors.Add(
                "Priority no puede ser negativo.");
        }

        return errors;
    }
}

/// <summary>
/// Pose lógica completa formada por piezas.
/// </summary>
public sealed class PoseFrameDefinition
{
    public string Id { get; set; } =
        Guid.NewGuid().ToString("N");

    public string Name { get; set; } =
        string.Empty;

    /// <summary>
    /// Índice de la región correspondiente en la hoja de referencia
    /// de Aladdin.
    /// </summary>
    public int TargetFrameIndex { get; set; }

    public int CanvasWidth { get; set; }

    public int CanvasHeight { get; set; }

    public int AnchorX { get; set; }

    public int AnchorY { get; set; }

    public bool IsVerifiedInRom { get; set; }

    public string VerificationNotes { get; set; } =
        string.Empty;

    public List<SpritePieceDefinition> Pieces { get; set; } =
        new();

    public IReadOnlyList<string> Validate()
    {
        List<string> errors =
            new();

        if (string.IsNullOrWhiteSpace(Id))
        {
            errors.Add(
                "La pose no tiene identificador.");
        }

        if (TargetFrameIndex < 0)
        {
            errors.Add(
                "TargetFrameIndex no puede ser negativo.");
        }

        if (CanvasWidth <= 0)
        {
            errors.Add(
                "CanvasWidth debe ser mayor que cero.");
        }

        if (CanvasHeight <= 0)
        {
            errors.Add(
                "CanvasHeight debe ser mayor que cero.");
        }

        HashSet<string> ids =
            new(
                StringComparer.OrdinalIgnoreCase);

        for (int index = 0;
             index < Pieces.Count;
             index++)
        {
            SpritePieceDefinition piece =
                Pieces[index];

            foreach (string error
                     in piece.Validate())
            {
                errors.Add(
                    $"Pieza {index:000}: {error}");
            }

            if (!ids.Add(piece.Id))
            {
                errors.Add(
                    $"La pieza {piece.Id} está repetida.");
            }
        }

        return errors;
    }
}

public enum AnimationLoopMode
{
    Loop,
    Once,
    HoldLastFrame,
    PingPong
}

/// <summary>
/// Referencia a una pose dentro de una animación.
/// </summary>
public sealed class AnimationFrameReference
{
    public required string PoseId { get; set; }

    public int DurationTicks { get; set; } = 1;

    public bool FlipHorizontal { get; set; }

    public bool FlipVertical { get; set; }

    public IReadOnlyList<string> Validate()
    {
        List<string> errors =
            new();

        if (string.IsNullOrWhiteSpace(PoseId))
        {
            errors.Add(
                "PoseId es obligatorio.");
        }

        if (DurationTicks <= 0)
        {
            errors.Add(
                "DurationTicks debe ser mayor que cero.");
        }

        return errors;
    }
}

/// <summary>
/// Secuencia lógica de poses.
/// </summary>
public sealed class AnimationDefinition
{
    public string Id { get; set; } =
        Guid.NewGuid().ToString("N");

    public string Name { get; set; } =
        string.Empty;

    public AnimationLoopMode LoopMode { get; set; } =
        AnimationLoopMode.Loop;

    public bool IsVerifiedInRom { get; set; }

    public string VerificationNotes { get; set; } =
        string.Empty;

    public List<AnimationFrameReference> Frames { get; set; } =
        new();

    public IReadOnlyList<string> Validate(
        IReadOnlySet<string> availablePoseIds)
    {
        ArgumentNullException.ThrowIfNull(
            availablePoseIds);

        List<string> errors =
            new();

        if (string.IsNullOrWhiteSpace(Id))
        {
            errors.Add(
                "La animación no tiene identificador.");
        }

        if (Frames.Count == 0)
        {
            errors.Add(
                "La animación no contiene cuadros.");
        }

        for (int index = 0;
             index < Frames.Count;
             index++)
        {
            AnimationFrameReference frame =
                Frames[index];

            foreach (string error
                     in frame.Validate())
            {
                errors.Add(
                    $"Cuadro {index:000}: {error}");
            }

            if (!string.IsNullOrWhiteSpace(
                    frame.PoseId) &&
                !availablePoseIds.Contains(
                    frame.PoseId))
            {
                errors.Add(
                    $"El cuadro {index:000} referencia una pose inexistente: " +
                    $"{frame.PoseId}.");
            }
        }

        return errors;
    }
}

/// <summary>
/// Documento lógico de poses y animaciones.
/// </summary>
public sealed class SpriteCompositionDocument
{
    public int FormatVersion { get; set; } = 1;

    public List<PoseFrameDefinition> Poses { get; set; } =
        new();

    public List<AnimationDefinition> Animations { get; set; } =
        new();

    public PoseFrameDefinition? FindPose(
        string poseId)
    {
        return
            Poses.FirstOrDefault(
                pose =>
                    string.Equals(
                        pose.Id,
                        poseId,
                        StringComparison.OrdinalIgnoreCase));
    }

    public IReadOnlyList<string> Validate()
    {
        List<string> errors =
            new();

        if (FormatVersion != 1)
        {
            errors.Add(
                $"Versión de composición no compatible: {FormatVersion}.");
        }

        HashSet<string> poseIds =
            new(
                StringComparer.OrdinalIgnoreCase);

        for (int index = 0;
             index < Poses.Count;
             index++)
        {
            PoseFrameDefinition pose =
                Poses[index];

            foreach (string error
                     in pose.Validate())
            {
                errors.Add(
                    $"Pose {index:000}: {error}");
            }

            if (!poseIds.Add(pose.Id))
            {
                errors.Add(
                    $"El identificador de pose {pose.Id} está repetido.");
            }
        }

        HashSet<string> animationIds =
            new(
                StringComparer.OrdinalIgnoreCase);

        for (int index = 0;
             index < Animations.Count;
             index++)
        {
            AnimationDefinition animation =
                Animations[index];

            foreach (string error
                     in animation.Validate(
                         poseIds))
            {
                errors.Add(
                    $"Animación {index:000}: {error}");
            }

            if (!animationIds.Add(
                    animation.Id))
            {
                errors.Add(
                    $"El identificador de animación " +
                    $"{animation.Id} está repetido.");
            }
        }

        return errors;
    }
}
