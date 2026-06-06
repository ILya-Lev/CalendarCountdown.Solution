namespace CalendarCountdown.Solution.Entities;

public readonly record struct LocationReference(
    string? DisplayLocationText,
    string? MapExternalUrl,
    string? MapIframeSrc);