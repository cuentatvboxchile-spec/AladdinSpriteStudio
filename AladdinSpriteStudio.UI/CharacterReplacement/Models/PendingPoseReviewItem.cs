using System.Collections.Generic;

namespace AladdinSpriteStudio.UI.CharacterReplacement.Models;

public sealed class PendingPoseReviewItem
{
    public required SpriteSheetDocument TargetDocument { get; init; }
    public required SpriteFrame TargetFrame { get; init; }

    public List<PendingPoseCandidate> Candidates { get; } = new();

    public bool Resolved { get; set; }
}

public sealed class PendingPoseCandidate
{
    public required SpriteSheetDocument SourceDocument { get; init; }
    public required SpriteFrame SourceFrame { get; init; }

    public double Score { get; init; }

    public bool SuggestedFlipHorizontal { get; init; }
    public bool SuggestedFlipVertical { get; init; }
}