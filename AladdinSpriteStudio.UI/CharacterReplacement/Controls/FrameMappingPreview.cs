using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using AladdinSpriteStudio.UI.CharacterReplacement.Models;

namespace AladdinSpriteStudio.UI.CharacterReplacement.Controls;

/// <summary>
/// Muestra la pose original de Aladdin y una vista previa
/// de cómo quedará la pose del personaje nuevo.
/// </summary>
public sealed class FrameMappingPreview :
    System.Windows.Forms.Control
{
    private FrameMapping? _mapping;

    public FrameMappingPreview()
    {
        SetStyle(
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.UserPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw,
            true);

        BackColor =
            Color.FromArgb(
                30,
                30,
                33);

        ForeColor =
            Color.White;

        MinimumSize =
            new Size(
                480,
                260);
    }

    /// <summary>
    /// Asignación que se mostrará en la vista previa.
    /// </summary>
    public FrameMapping? Mapping
    {
        get => _mapping;

        set
        {
            _mapping = value;
            Invalidate();
        }
    }

    protected override void OnPaint(
        PaintEventArgs e)
    {
        base.OnPaint(e);

        e.Graphics.Clear(
            BackColor);

        e.Graphics.InterpolationMode =
            InterpolationMode.NearestNeighbor;

        e.Graphics.PixelOffsetMode =
            PixelOffsetMode.Half;

        e.Graphics.SmoothingMode =
            SmoothingMode.None;

        if (_mapping is null)
        {
            DrawCenteredMessage(
                e.Graphics,
                "Seleccione una pose de Aladdin y " +
                "una pose del personaje nuevo.");

            return;
        }

        Rectangle contentRectangle =
            Rectangle.Inflate(
                ClientRectangle,
                -12,
                -12);

        int titleHeight =
            30;

        int informationHeight =
            38;

        int availableHeight =
            Math.Max(
                1,
                contentRectangle.Height -
                titleHeight -
                informationHeight);

        int separation =
            12;

        int columnWidth =
            Math.Max(
                1,
                (contentRectangle.Width -
                 separation) /
                2);

        Rectangle targetArea =
            new(
                contentRectangle.Left,
                contentRectangle.Top +
                titleHeight,
                columnWidth,
                availableHeight);

        Rectangle replacementArea =
            new(
                targetArea.Right +
                separation,
                targetArea.Top,
                columnWidth,
                availableHeight);

        DrawTitle(
            e.Graphics,
            "Pose original de Aladdin",
            new Rectangle(
                targetArea.Left,
                contentRectangle.Top,
                targetArea.Width,
                titleHeight));

        DrawTitle(
            e.Graphics,
            "Resultado adaptado",
            new Rectangle(
                replacementArea.Left,
                contentRectangle.Top,
                replacementArea.Width,
                titleHeight));

        DrawTargetFrame(
            e.Graphics,
            targetArea);

        DrawMappedFrame(
            e.Graphics,
            replacementArea);

        Rectangle informationRectangle =
            new(
                contentRectangle.Left,
                targetArea.Bottom,
                contentRectangle.Width,
                informationHeight);

        DrawMappingInformation(
            e.Graphics,
            informationRectangle);
    }

    /// <summary>
    /// Dibuja la pose original de Aladdin.
    /// </summary>
    private void DrawTargetFrame(
        System.Drawing.Graphics graphics,
        Rectangle availableArea)
    {
        if (_mapping is null)
        {
            return;
        }

        using Bitmap frameBitmap =
            CreateTransparentFrameBitmap(
                _mapping.TargetDocument,
                _mapping.TargetFrame);

        Rectangle destination =
            FitRectangle(
                frameBitmap.Size,
                availableArea,
                padding: 18);

        if (destination.IsEmpty)
        {
            return;
        }

        DrawCanvasBackground(
            graphics,
            destination);

        graphics.DrawImage(
            frameBitmap,
            destination);

        graphics.DrawRectangle(
            Pens.DimGray,
            destination);
    }

    /// <summary>
    /// Dibuja el personaje sustituto adaptado
    /// al espacio ocupado por la pose de Aladdin.
    /// </summary>
    private void DrawMappedFrame(
        System.Drawing.Graphics graphics,
        Rectangle availableArea)
    {
        if (_mapping is null)
        {
            return;
        }

        Size targetSize =
            _mapping.TargetFrame.Bounds.Size;

        Rectangle canvasRectangle =
            FitRectangle(
                targetSize,
                availableArea,
                padding: 18);

        if (canvasRectangle.IsEmpty)
        {
            return;
        }

        DrawCanvasBackground(
            graphics,
            canvasRectangle);

        using Bitmap sourceBitmap =
            CreateTransparentFrameBitmap(
                _mapping.SourceDocument,
                _mapping.SourceFrame);

        ApplyFlip(
            sourceBitmap,
            _mapping.FlipHorizontal,
            _mapping.FlipVertical);

        Rectangle localDestination =
            _mapping.GetDestinationBounds();

        float scaleX =
            canvasRectangle.Width /
            (float)Math.Max(
                1,
                targetSize.Width);

        float scaleY =
            canvasRectangle.Height /
            (float)Math.Max(
                1,
                targetSize.Height);

        RectangleF destination =
            new(
                canvasRectangle.Left +
                localDestination.X *
                scaleX,

                canvasRectangle.Top +
                localDestination.Y *
                scaleY,

                localDestination.Width *
                scaleX,

                localDestination.Height *
                scaleY);

        using Region previousClip =
            graphics.Clip.Clone();

        graphics.SetClip(
            canvasRectangle);

        graphics.DrawImage(
            sourceBitmap,
            destination);

        graphics.Clip =
            previousClip;

        DrawAlignmentGuides(
            graphics,
            canvasRectangle);

        graphics.DrawRectangle(
            Pens.DimGray,
            canvasRectangle);
    }

    /// <summary>
    /// Aplica el volteo configurado a la imagen fuente.
    /// </summary>
    private static void ApplyFlip(
        Bitmap bitmap,
        bool flipHorizontal,
        bool flipVertical)
    {
        if (flipHorizontal &&
            flipVertical)
        {
            bitmap.RotateFlip(
                RotateFlipType.RotateNoneFlipXY);

            return;
        }

        if (flipHorizontal)
        {
            bitmap.RotateFlip(
                RotateFlipType.RotateNoneFlipX);

            return;
        }

        if (flipVertical)
        {
            bitmap.RotateFlip(
                RotateFlipType.RotateNoneFlipY);
        }
    }

    /// <summary>
    /// Dibuja las guías de centro y línea del suelo.
    /// </summary>
    private static void DrawAlignmentGuides(
        System.Drawing.Graphics graphics,
        Rectangle canvasRectangle)
    {
        using Pen centerGuidePen =
            new(
                Color.FromArgb(
                    110,
                    Color.DeepSkyBlue),
                1f)
            {
                DashStyle =
                    DashStyle.Dash
            };

        float centerX =
            canvasRectangle.Left +
            canvasRectangle.Width /
            2f;

        graphics.DrawLine(
            centerGuidePen,
            centerX,
            canvasRectangle.Top,
            centerX,
            canvasRectangle.Bottom);

        using Pen floorGuidePen =
            new(
                Color.FromArgb(
                    190,
                    Color.Gold),
                1f);

        graphics.DrawLine(
            floorGuidePen,
            canvasRectangle.Left,
            canvasRectangle.Bottom - 1,
            canvasRectangle.Right,
            canvasRectangle.Bottom - 1);
    }

    /// <summary>
    /// Dibuja los datos de la asignación actual.
    /// </summary>
    private void DrawMappingInformation(
        System.Drawing.Graphics graphics,
        Rectangle rectangle)
    {
        if (_mapping is null)
        {
            return;
        }

        string fittingMode =
            _mapping.AutoFit
                ? $"Ajuste automático: " +
                  $"{_mapping.EffectiveScale:0.00}"
                : $"Escala manual: " +
                  $"{_mapping.Scale:0.00}";

        string flipInformation =
            GetFlipDescription(
                _mapping);

        string text =
            $"Aladdin " +
            $"{_mapping.TargetFrame.Index + 1:000} " +
            $"← " +
            $"{_mapping.SourceDocument.FileName} / " +
            $"Pose {_mapping.SourceFrame.Index + 1:000} | " +
            $"{fittingMode} | " +
            $"X={_mapping.OffsetX}, " +
            $"Y={_mapping.OffsetY}" +
            flipInformation;

        TextRenderer.DrawText(
            graphics,
            text,
            Font,
            rectangle,
            ForeColor,
            TextFormatFlags.Left |
            TextFormatFlags.VerticalCenter |
            TextFormatFlags.EndEllipsis |
            TextFormatFlags.NoPrefix);
    }

    private static string GetFlipDescription(
        FrameMapping mapping)
    {
        if (mapping.FlipHorizontal &&
            mapping.FlipVertical)
        {
            return
                " | Volteo: horizontal y vertical";
        }

        if (mapping.FlipHorizontal)
        {
            return
                " | Volteo: horizontal";
        }

        if (mapping.FlipVertical)
        {
            return
                " | Volteo: vertical";
        }

        return string.Empty;
    }

    /// <summary>
    /// Dibuja un título sobre cada vista.
    /// </summary>
    private static void DrawTitle(
        System.Drawing.Graphics graphics,
        string text,
        Rectangle rectangle)
    {
        using Font titleFont =
            new(
                FontFamily.GenericSansSerif,
                9f,
                FontStyle.Bold,
                GraphicsUnit.Point);

        TextRenderer.DrawText(
            graphics,
            text,
            titleFont,
            rectangle,
            Color.White,
            TextFormatFlags.HorizontalCenter |
            TextFormatFlags.VerticalCenter |
            TextFormatFlags.NoPrefix);
    }

    /// <summary>
    /// Dibuja un fondo cuadriculado para representar
    /// la transparencia de la pose.
    /// </summary>
    private static void DrawCanvasBackground(
        System.Drawing.Graphics graphics,
        Rectangle rectangle)
    {
        using SolidBrush backgroundBrush =
            new(
                Color.FromArgb(
                    46,
                    46,
                    50));

        graphics.FillRectangle(
            backgroundBrush,
            rectangle);

        const int squareSize =
            10;

        using SolidBrush checkerBrush =
            new(
                Color.FromArgb(
                    58,
                    58,
                    62));

        for (int y = rectangle.Top;
             y < rectangle.Bottom;
             y += squareSize)
        {
            for (int x = rectangle.Left;
                 x < rectangle.Right;
                 x += squareSize)
            {
                int column =
                    (x - rectangle.Left) /
                    squareSize;

                int row =
                    (y - rectangle.Top) /
                    squareSize;

                if ((column + row) % 2 == 0)
                {
                    continue;
                }

                Rectangle square =
                    Rectangle.Intersect(
                        new Rectangle(
                            x,
                            y,
                            squareSize,
                            squareSize),
                        rectangle);

                graphics.FillRectangle(
                    checkerBrush,
                    square);
            }
        }
    }

    /// <summary>
    /// Extrae una pose y convierte el color de fondo
    /// de la hoja en transparencia.
    /// </summary>
    private static Bitmap CreateTransparentFrameBitmap(
        SpriteSheetDocument document,
        SpriteFrame frame)
    {
        Rectangle imageBounds =
            new(
                0,
                0,
                document.Image.Width,
                document.Image.Height);

        Rectangle bounds =
            Rectangle.Intersect(
                frame.Bounds,
                imageBounds);

        if (bounds.Width <= 0 ||
            bounds.Height <= 0)
        {
            return new Bitmap(
                1,
                1,
                PixelFormat.Format32bppArgb);
        }

        Bitmap result =
            new(
                bounds.Width,
                bounds.Height,
                PixelFormat.Format32bppArgb);

        const int backgroundTolerance =
            18;

        for (int y = 0;
             y < bounds.Height;
             y++)
        {
            for (int x = 0;
                 x < bounds.Width;
                 x++)
            {
                Color color =
                    document.Image.GetPixel(
                        bounds.Left + x,
                        bounds.Top + y);

                bool isBackground =
                    IsBackgroundColor(
                        color,
                        document.BackgroundColor,
                        backgroundTolerance);

                result.SetPixel(
                    x,
                    y,
                    isBackground
                        ? Color.Transparent
                        : Color.FromArgb(
                            color.A,
                            color.R,
                            color.G,
                            color.B));
            }
        }

        return result;
    }

    private static bool IsBackgroundColor(
        Color color,
        Color backgroundColor,
        int tolerance)
    {
        if (color.A < 16)
        {
            return true;
        }

        return
            Math.Abs(
                color.R -
                backgroundColor.R) <= tolerance
            &&
            Math.Abs(
                color.G -
                backgroundColor.G) <= tolerance
            &&
            Math.Abs(
                color.B -
                backgroundColor.B) <= tolerance;
    }

    /// <summary>
    /// Ajusta un tamaño dentro de un área sin alterar
    /// su proporción original.
    /// </summary>
    private static Rectangle FitRectangle(
        Size sourceSize,
        Rectangle availableArea,
        int padding)
    {
        Rectangle innerArea =
            Rectangle.Inflate(
                availableArea,
                -padding,
                -padding);

        if (sourceSize.Width <= 0 ||
            sourceSize.Height <= 0 ||
            innerArea.Width <= 0 ||
            innerArea.Height <= 0)
        {
            return Rectangle.Empty;
        }

        float scale =
            Math.Min(
                innerArea.Width /
                (float)sourceSize.Width,

                innerArea.Height /
                (float)sourceSize.Height);

        int width =
            Math.Max(
                1,
                (int)Math.Round(
                    sourceSize.Width *
                    scale));

        int height =
            Math.Max(
                1,
                (int)Math.Round(
                    sourceSize.Height *
                    scale));

        int x =
            innerArea.Left +
            (innerArea.Width -
             width) /
            2;

        int y =
            innerArea.Top +
            (innerArea.Height -
             height) /
            2;

        return new Rectangle(
            x,
            y,
            width,
            height);
    }

    private void DrawCenteredMessage(
        System.Drawing.Graphics graphics,
        string message)
    {
        TextRenderer.DrawText(
            graphics,
            message,
            Font,
            ClientRectangle,
            ForeColor,
            TextFormatFlags.HorizontalCenter |
            TextFormatFlags.VerticalCenter |
            TextFormatFlags.WordBreak |
            TextFormatFlags.NoPrefix);
    }
}