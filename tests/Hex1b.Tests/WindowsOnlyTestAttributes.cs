using System.Runtime.CompilerServices;

namespace Hex1b.Tests;

/// <summary>
/// Marks a <see cref="FactAttribute"/> test as Windows-only.
/// The test is automatically skipped when running on non-Windows platforms.
/// </summary>
public sealed class WindowsOnlyFactAttribute : FactAttribute
{
    public WindowsOnlyFactAttribute(
        [CallerFilePath] string? sourceFilePath = null,
        [CallerLineNumber] int sourceLineNumber = 0)
        : base(sourceFilePath: sourceFilePath, sourceLineNumber: sourceLineNumber)
    {
        if (!OperatingSystem.IsWindows())
        {
            Skip = "Windows-only test";
        }
    }
}

/// <summary>
/// Marks a <see cref="TheoryAttribute"/> test as Windows-only.
/// The test is automatically skipped when running on non-Windows platforms.
/// </summary>
public sealed class WindowsOnlyTheoryAttribute : TheoryAttribute
{
    public WindowsOnlyTheoryAttribute(
        [CallerFilePath] string? sourceFilePath = null,
        [CallerLineNumber] int sourceLineNumber = 0)
        : base(sourceFilePath: sourceFilePath, sourceLineNumber: sourceLineNumber)
    {
        if (!OperatingSystem.IsWindows())
        {
            Skip = "Windows-only test";
        }
    }
}
