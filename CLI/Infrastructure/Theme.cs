using Spectre.Console;

namespace AdbDriverInstaller.CLI.Infrastructure;

/// <summary>
/// Tailwind CSS 500-inspired color palette and Unicode symbols for consistent CLI theming.
/// </summary>
internal static class Theme
{
    // ── Markup color strings (use inside [color] tags) ──────────
    public const string Blue  = "#3b82f6";   // Tailwind blue-500
    public const string Green = "#22c55e";   // Tailwind green-500
    public const string Red   = "#ef4444";   // Tailwind red-500
    public const string Amber = "#f59e0b";   // Tailwind amber-500
    public const string Cyan  = "#06b6d4";   // Tailwind cyan-500
    public const string Gray  = "#6b7280";   // Tailwind gray-500
    public const string Dim   = "#9ca3af";   // Tailwind gray-400

    // ── Color objects (for API calls: BorderColor, SpinnerStyle…) ─
    public static readonly Color BlueColor  = new(0x3b, 0x82, 0xf6);
    public static readonly Color GreenColor = new(0x22, 0xc5, 0x5e);
    public static readonly Color RedColor   = new(0xef, 0x44, 0x44);
    public static readonly Color AmberColor = new(0xf5, 0x9e, 0x0b);
    public static readonly Color CyanColor  = new(0x06, 0xb6, 0xd4);
    public static readonly Color GrayColor  = new(0x6b, 0x72, 0x80);
    public static readonly Color DimColor   = new(0x9c, 0xa3, 0xaf);

    // ── Symbols (UTF-8 — ensure Console.OutputEncoding = UTF8) ──
    public const string Ok   = "✓";
    public const string Fail = "✗";
    public const string Dash = "—";
}
