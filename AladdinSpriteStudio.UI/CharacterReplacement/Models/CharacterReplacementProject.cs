namespace AladdinSpriteStudio.UI.CharacterReplacement.Models;

/// <summary>
/// Conserva la hoja objetivo de Aladdin,
/// las hojas del personaje nuevo y las asignaciones
/// realizadas entre sus poses.
/// </summary>
public sealed class CharacterReplacementProject
{
    private readonly List<SpriteSheetDocument>
        _sourceDocuments =
            new();

    private readonly List<FrameMapping>
        _mappings =
            new();

    /// <summary>
    /// Hoja original de Aladdin usada como molde.
    /// </summary>
    public SpriteSheetDocument? TargetDocument
    {
        get;
        private set;
    }

    /// <summary>
    /// Hojas disponibles del personaje sustituto.
    /// </summary>
    public IReadOnlyList<SpriteSheetDocument>
        SourceDocuments =>
            _sourceDocuments;

    /// <summary>
    /// Asignaciones realizadas.
    /// </summary>
    public IReadOnlyList<FrameMapping>
        Mappings =>
            _mappings;

    public int MappingCount =>
        _mappings.Count;

    /// <summary>
    /// Establece la hoja objetivo de Aladdin.
    /// </summary>
    public void SetTargetDocument(
        SpriteSheetDocument? document)
    {
        if (ReferenceEquals(
                TargetDocument,
                document))
        {
            return;
        }

        TargetDocument =
            document;

        // Las asignaciones anteriores dejan de ser válidas
        // al cambiar la hoja objetivo.
        _mappings.Clear();
    }

    /// <summary>
    /// Agrega una hoja del personaje nuevo.
    /// </summary>
    public bool AddSourceDocument(
        SpriteSheetDocument document)
    {
        ArgumentNullException.ThrowIfNull(
            document);

        if (_sourceDocuments.Contains(document))
        {
            return false;
        }

        _sourceDocuments.Add(
            document);

        return true;
    }

    /// <summary>
    /// Retira una hoja del personaje nuevo.
    /// También elimina las asignaciones que la utilizaban.
    /// </summary>
    public bool RemoveSourceDocument(
        SpriteSheetDocument document)
    {
        ArgumentNullException.ThrowIfNull(
            document);

        bool removed =
            _sourceDocuments.Remove(
                document);

        if (!removed)
        {
            return false;
        }

        _mappings.RemoveAll(
            mapping =>
                ReferenceEquals(
                    mapping.SourceDocument,
                    document));

        return true;
    }

    /// <summary>
    /// Asigna una pose del personaje nuevo
    /// a una pose objetivo de Aladdin.
    /// </summary>
    public FrameMapping AssignFrame(
        SpriteFrame targetFrame,
        SpriteSheetDocument sourceDocument,
        SpriteFrame sourceFrame)
    {
        if (TargetDocument is null)
        {
            throw new InvalidOperationException(
                "Primero debe cargar la hoja objetivo de Aladdin.");
        }

        ArgumentNullException.ThrowIfNull(
            targetFrame);

        ArgumentNullException.ThrowIfNull(
            sourceDocument);

        ArgumentNullException.ThrowIfNull(
            sourceFrame);

        if (!TargetDocument.Frames.Contains(
                targetFrame))
        {
            throw new ArgumentException(
                "La pose objetivo no pertenece a la hoja actual de Aladdin.",
                nameof(targetFrame));
        }

        if (!_sourceDocuments.Contains(
                sourceDocument))
        {
            throw new ArgumentException(
                "La hoja fuente no está agregada al proyecto.",
                nameof(sourceDocument));
        }

        if (!sourceDocument.Frames.Contains(
                sourceFrame))
        {
            throw new ArgumentException(
                "La pose fuente no pertenece a la hoja seleccionada.",
                nameof(sourceFrame));
        }

        RemoveMapping(
            targetFrame);

        FrameMapping mapping =
            new(
                TargetDocument,
                targetFrame,
                sourceDocument,
                sourceFrame);

        _mappings.Add(
            mapping);

        SortMappings();

        return mapping;
    }

    /// <summary>
    /// Obtiene la asignación de una pose objetivo.
    /// </summary>
    public FrameMapping? GetMapping(
        SpriteFrame targetFrame)
    {
        ArgumentNullException.ThrowIfNull(
            targetFrame);

        return _mappings.FirstOrDefault(
            mapping =>
                ReferenceEquals(
                    mapping.TargetFrame,
                    targetFrame));
    }

    public FrameMapping? GetMappingByTargetIndex(
        int targetFrameIndex)
    {
        return _mappings.FirstOrDefault(
            mapping =>
                mapping.TargetFrame.Index ==
                targetFrameIndex);
    }

    /// <summary>
    /// Elimina la asignación de una pose objetivo.
    /// </summary>
    public bool RemoveMapping(
        SpriteFrame targetFrame)
    {
        ArgumentNullException.ThrowIfNull(
            targetFrame);

        int removedCount =
            _mappings.RemoveAll(
                mapping =>
                    ReferenceEquals(
                        mapping.TargetFrame,
                        targetFrame));

        return removedCount > 0;
    }

    public bool RemoveMapping(
        FrameMapping mapping)
    {
        ArgumentNullException.ThrowIfNull(
            mapping);

        return _mappings.Remove(
            mapping);
    }

    public void ClearMappings()
    {
        _mappings.Clear();
    }

    /// <summary>
    /// Elimina asignaciones cuyas poses o documentos
    /// ya no existen.
    /// </summary>
    public int RemoveInvalidMappings()
    {
        int previousCount =
            _mappings.Count;

        _mappings.RemoveAll(
            mapping =>
                TargetDocument is null ||

                !ReferenceEquals(
                    mapping.TargetDocument,
                    TargetDocument) ||

                !TargetDocument.Frames.Contains(
                    mapping.TargetFrame) ||

                !_sourceDocuments.Contains(
                    mapping.SourceDocument) ||

                !mapping.SourceDocument.Frames.Contains(
                    mapping.SourceFrame));

        return previousCount -
               _mappings.Count;
    }

    /// <summary>
    /// Calcula cuántas poses de Aladdin todavía
    /// no tienen un personaje sustituto asignado.
    /// </summary>
    public int GetUnmappedTargetCount()
    {
        if (TargetDocument is null)
        {
            return 0;
        }

        int mappedCount =
            _mappings
                .Select(mapping =>
                    mapping.TargetFrame)
                .Distinct()
                .Count();

        return Math.Max(
            0,
            TargetDocument.Frames.Count -
            mappedCount);
    }

    private void SortMappings()
    {
        _mappings.Sort(
            (first, second) =>
                first.TargetFrame.Index.CompareTo(
                    second.TargetFrame.Index));
    }
}