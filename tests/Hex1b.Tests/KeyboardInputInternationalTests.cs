using System.Text;
using Hex1b.Input;
using Hex1b.Widgets;

namespace Hex1b.Tests;

/// <summary>
/// Regression tests for international keyboard input across different keyboard layouts.
///
/// These tests document and protect the intended behavior for:
/// - Swedish characters (å, ä, ö) — the original regression scenario that motivated this suite
/// - German, French, Spanish, Portuguese, and Nordic keyboard layouts
/// - Dead key compositions (´+a=á, ¨+o=ö, ^+e=ê, ~+n=ñ, etc.)
/// - AltGr combinations (e.g. AltGr+E = € on many European keyboards)
///
/// Test strategy:
///
///   GetPrintableText and TryTranslateWin32InputSequence are pure static methods and
///   run on any host OS.
///
///   Integration tests (Sections 8–9) use TextInputStep.Type() which sends
///   Hex1bKeyEvent objects directly into the Hex1bApp pipeline, bypassing the
///   platform-specific console driver.  They verify that the full app → TextBox
///   pipeline correctly handles arbitrary Unicode grapheme clusters.
/// </summary>
public class KeyboardInputInternationalTests
{
    // =========================================================================
    // Section 1: GetPrintableText — direct UnicodeChar path
    //
    // When KEY_EVENT_RECORD.UnicodeChar is non-zero the character must be
    // returned unchanged — this is the common case on any VT-mode terminal and
    // on most Windows console hosts.  It is the most critical regression guard:
    // if this path breaks, every non-ASCII keystroke is silently dropped.
    // =========================================================================

    [WindowsOnlyTheory]
    // Swedish keyboard
    [InlineData('å', "å")]    // ring-a
    [InlineData('ä', "ä")]    // a-umlaut
    [InlineData('ö', "ö")]    // o-umlaut
    [InlineData('Å', "Å")]    // ring-a uppercase
    [InlineData('Ä', "Ä")]    // a-umlaut uppercase
    [InlineData('Ö', "Ö")]    // o-umlaut uppercase
    // German keyboard
    [InlineData('ü', "ü")]    // u-umlaut
    [InlineData('Ü', "Ü")]    // u-umlaut uppercase
    [InlineData('ß', "ß")]    // sharp-s (Eszett)
    // French characters
    [InlineData('é', "é")]    // e-acute
    [InlineData('è', "è")]    // e-grave
    [InlineData('ê', "ê")]    // e-circumflex
    [InlineData('à', "à")]    // a-grave
    [InlineData('â', "â")]    // a-circumflex
    [InlineData('ç', "ç")]    // c-cedilla
    // Spanish / Portuguese
    [InlineData('ñ', "ñ")]    // n-tilde
    [InlineData('á', "á")]    // a-acute   (dead-acute + a)
    [InlineData('í', "í")]    // i-acute
    [InlineData('ó', "ó")]    // o-acute
    [InlineData('ú', "ú")]    // u-acute
    [InlineData('ã', "ã")]    // a-tilde   (dead-tilde + a, Portuguese)
    // Nordic
    [InlineData('æ', "æ")]    // Danish/Norwegian ae-ligature
    [InlineData('ø', "ø")]    // Danish/Norwegian o-stroke
    // Other
    [InlineData('ë', "ë")]    // e-umlaut  (dead-umlaut + e)
    [InlineData('ï', "ï")]    // i-umlaut
    [InlineData('ô', "ô")]    // o-circumflex
    [InlineData('€', "€")]    // euro sign (AltGr+E on most European keyboards)
    [InlineData('£', "£")]    // pound sign
    public void GetPrintableText_WithNonZeroUnicodeChar_ReturnsCharDirectly(
        char unicodeChar, string expected)
    {
        // The VK code is irrelevant when UnicodeChar is provided — the driver must
        // return it verbatim.  This path must never regress.
        var result = WindowsConsoleDriver.GetPrintableText(
            vk: 0,
            ch: unicodeChar,
            hasCtrl: false,
            hasAlt: false,
            hasShift: false);

        Assert.Equal(expected, result);
    }

    // =========================================================================
    // Section 2: GetPrintableText — US-layout VK fallback (UnicodeChar == '\0')
    //
    // Some Windows console hosts deliver UnicodeChar == '\0' for printable input.
    // The fallback covers the full US-layout symbol set so basic ASCII typing
    // still works even when the console host omits the unicode char.
    // =========================================================================

    [WindowsOnlyTheory]
    // Letters — lower and uppercase via Shift
    [InlineData(0x41, false, false, false, "a")]   // VK_A
    [InlineData(0x41, false, false, true,  "A")]   // VK_A + Shift
    [InlineData(0x5A, false, false, false, "z")]   // VK_Z
    [InlineData(0x5A, false, false, true,  "Z")]   // VK_Z + Shift
    [InlineData(0x45, false, false, false, "e")]   // VK_E
    [InlineData(0x53, false, false, false, "s")]   // VK_S
    // Digits and Shift-digit symbols
    [InlineData(0x30, false, false, false, "0")]   // VK_0
    [InlineData(0x30, false, false, true,  ")")]   // VK_0 + Shift
    [InlineData(0x31, false, false, false, "1")]   // VK_1
    [InlineData(0x31, false, false, true,  "!")]   // VK_1 + Shift
    [InlineData(0x32, false, false, false, "2")]   // VK_2
    [InlineData(0x32, false, false, true,  "@")]   // VK_2 + Shift
    [InlineData(0x33, false, false, false, "3")]
    [InlineData(0x33, false, false, true,  "#")]
    [InlineData(0x34, false, false, false, "4")]
    [InlineData(0x34, false, false, true,  "$")]
    [InlineData(0x35, false, false, false, "5")]
    [InlineData(0x35, false, false, true,  "%")]
    [InlineData(0x36, false, false, false, "6")]
    [InlineData(0x36, false, false, true,  "^")]
    [InlineData(0x37, false, false, false, "7")]
    [InlineData(0x37, false, false, true,  "&")]
    [InlineData(0x38, false, false, false, "8")]
    [InlineData(0x38, false, false, true,  "*")]
    [InlineData(0x39, false, false, false, "9")]
    [InlineData(0x39, false, false, true,  "(")]
    // Space
    [InlineData(0x20, false, false, false, " ")]
    // OEM punctuation (US layout positions)
    [InlineData(0xBA, false, false, false, ";")]   // VK_OEM_1
    [InlineData(0xBA, false, false, true,  ":")]
    [InlineData(0xBB, false, false, false, "=")]   // VK_OEM_PLUS
    [InlineData(0xBB, false, false, true,  "+")]
    [InlineData(0xBC, false, false, false, ",")]   // VK_OEM_COMMA
    [InlineData(0xBC, false, false, true,  "<")]
    [InlineData(0xBD, false, false, false, "-")]   // VK_OEM_MINUS
    [InlineData(0xBD, false, false, true,  "_")]
    [InlineData(0xBE, false, false, false, ".")]   // VK_OEM_PERIOD
    [InlineData(0xBE, false, false, true,  ">")]
    [InlineData(0xBF, false, false, false, "/")]   // VK_OEM_2
    [InlineData(0xBF, false, false, true,  "?")]
    [InlineData(0xC0, false, false, false, "`")]   // VK_OEM_3
    [InlineData(0xC0, false, false, true,  "~")]
    [InlineData(0xDB, false, false, false, "[")]   // VK_OEM_4
    [InlineData(0xDB, false, false, true,  "{")]
    [InlineData(0xDC, false, false, false, "\\")]  // VK_OEM_5
    [InlineData(0xDC, false, false, true,  "|")]
    [InlineData(0xDD, false, false, false, "]")]   // VK_OEM_6
    [InlineData(0xDD, false, false, true,  "}")]
    [InlineData(0xDE, false, false, false, "'")]   // VK_OEM_7
    [InlineData(0xDE, false, false, true,  "\"")]
    public void GetPrintableText_NullUnicodeChar_UsLayoutFallback_ReturnsExpectedChar(
        int vk, bool hasCtrl, bool hasAlt, bool hasShift, string expected)
    {
        var result = WindowsConsoleDriver.GetPrintableText(
            (ushort)vk, ch: '\0', hasCtrl, hasAlt, hasShift);

        Assert.Equal(expected, result);
    }

    // =========================================================================
    // Section 3: GetPrintableText — modifier combinations that must NOT produce text
    //
    // Ctrl+key and standalone Alt+key combos are keyboard shortcuts, not text.
    // Returning a stray character here would break every Ctrl/Alt binding.
    // AltGr (which sets both Ctrl+Alt on Windows) is handled via UnicodeChar.
    // =========================================================================

    [WindowsOnlyTheory]
    [InlineData(0x41, true,  false, false)]   // Ctrl+A
    [InlineData(0x45, true,  false, false)]   // Ctrl+E
    [InlineData(0x53, true,  false, false)]   // Ctrl+S
    [InlineData(0x5A, true,  false, false)]   // Ctrl+Z
    [InlineData(0x41, false, true,  false)]   // Alt+A  → emitted as ESC+a upstream, not bare 'a'
    [InlineData(0x45, false, true,  false)]   // Alt+E
    public void GetPrintableText_ModifierWithNullUnicodeChar_ReturnsNull(
        int vk, bool hasCtrl, bool hasAlt, bool hasShift)
    {
        var result = WindowsConsoleDriver.GetPrintableText(
            (ushort)vk, ch: '\0', hasCtrl, hasAlt, hasShift);

        Assert.Null(result);
    }

    [WindowsOnlyTheory]
    // AltGr (Right-Alt) on Windows sets BOTH hasCtrl and hasAlt.
    // When the console host provides UnicodeChar the character must be returned.
    [InlineData(0x45, '€', true, true,  false, "€")]  // AltGr+E = € (Swedish/German/Italian)
    [InlineData(0x45, 'é', true, true,  false, "é")]  // AltGr+E = é (some layouts)
    [InlineData(0x32, '@', true, true,  false, "@")]  // AltGr+2 = @ (Swedish/Finnish)
    [InlineData(0xDB, '{', true, true,  false, "{")]  // AltGr+[ = { (Swedish/Finnish)
    public void GetPrintableText_AltGrWithNonZeroUnicodeChar_ReturnsChar(
        int vk, char unicodeChar, bool hasCtrl, bool hasAlt, bool hasShift, string expected)
    {
        var result = WindowsConsoleDriver.GetPrintableText(
            (ushort)vk, ch: unicodeChar, hasCtrl, hasAlt, hasShift);

        Assert.Equal(expected, result);
    }

    // =========================================================================
    // Section 4: TryTranslateWin32InputSequence — Swedish keyboard layout
    //
    // Win32 input forwarding format:
    //   ESC [ vk ; scan ; unicodeChar ; keyDown ; controlState ; repeatCount _
    //
    // Swedish key positions (VK code / scan code / Unicode):
    //   Å  VK=0xDB(219)  scan=0x1A(26)   U+00E5(229) / U+00C5(197)
    //   Ä  VK=0xDE(222)  scan=0x28(40)   U+00E4(228) / U+00C4(196)
    //   Ö  VK=0xBA(186)  scan=0x27(39)   U+00F6(246) / U+00D6(214)
    //
    // REGRESSION: before the layout-aware fix these same VK codes (0xDB, 0xDE, 0xBA)
    // fell through to the US-layout fallback which maps them to [, ', and ; —
    // so Swedish letters were silently replaced by punctuation.
    // =========================================================================

    [Theory]
    [InlineData("\x1b[219;26;229;1;0;1_",  "å")]   // Swedish å (lowercase)
    [InlineData("\x1b[219;26;197;1;16;1_", "Å")]   // Swedish Å (uppercase; SHIFT_PRESSED=0x10=16)
    [InlineData("\x1b[222;40;228;1;0;1_",  "ä")]   // Swedish ä (lowercase)
    [InlineData("\x1b[222;40;196;1;16;1_", "Ä")]   // Swedish Ä (uppercase)
    [InlineData("\x1b[186;39;246;1;0;1_",  "ö")]   // Swedish ö (lowercase)
    [InlineData("\x1b[186;39;214;1;16;1_", "Ö")]   // Swedish Ö (uppercase)
    public void TryTranslateWin32InputSequence_SwedishKeyboard_DecodesCorrectly(
        string sequence, string expectedText)
    {
        // REGRESSION TEST: Swedish characters were silently dropped in earlier builds
        // because VK_OEM_4/7/1 were resolved against the US-layout fallback map.
        var handled = WindowsConsoleDriver.TryTranslateWin32InputSequence(sequence, out var bytes);

        Assert.True(handled, "sequence must be recognized as a Win32 input event");
        Assert.Equal(expectedText, Encoding.UTF8.GetString(bytes));
    }

    // =========================================================================
    // Section 5: TryTranslateWin32InputSequence — other European keyboard layouts
    // =========================================================================

    [Theory]
    // German keyboard (Ü shares the VK_OEM_4 position with Swedish Å)
    [InlineData("\x1b[219;26;252;1;0;1_",  "ü")]   // German ü
    [InlineData("\x1b[219;26;220;1;16;1_", "Ü")]   // German Ü
    [InlineData("\x1b[189;12;223;1;0;1_",  "ß")]   // German Eszett (ß)
    // French AZERTY characters
    [InlineData("\x1b[50;3;233;1;0;1_",    "é")]   // French é  (key above 2 on AZERTY)
    [InlineData("\x1b[48;11;224;1;0;1_",   "à")]   // French à  (key above 0)
    [InlineData("\x1b[57;39;231;1;0;1_",   "ç")]   // French ç
    // Spanish
    [InlineData("\x1b[186;40;241;1;0;1_",  "ñ")]   // Spanish ñ
    // Danish / Norwegian
    [InlineData("\x1b[192;41;230;1;0;1_",  "æ")]   // Danish/Norwegian æ
    [InlineData("\x1b[186;27;248;1;0;1_",  "ø")]   // Danish/Norwegian ø
    public void TryTranslateWin32InputSequence_EuropeanKeyboards_DecodesCorrectly(
        string sequence, string expectedText)
    {
        var handled = WindowsConsoleDriver.TryTranslateWin32InputSequence(sequence, out var bytes);

        Assert.True(handled, "sequence must be recognized as a Win32 input event");
        Assert.Equal(expectedText, Encoding.UTF8.GetString(bytes));
    }

    // =========================================================================
    // Section 6: TryTranslateWin32InputSequence — dead key composed results
    //
    // On Windows, dead key composition (e.g. pressing ´ then 'a' to get á) is
    // resolved by the console host before forwarding.  The composed character
    // arrives in the unicodeChar field of the forwarded Win32 input sequence.
    //
    // These tests verify that every common composition survives the translation
    // pipeline unchanged.
    // =========================================================================

    [Theory]
    // Dead acute (´) + vowel
    [InlineData("\x1b[65;30;225;1;0;1_",  "á")]   // ´+a → á  (Spanish, Portuguese, Irish)
    [InlineData("\x1b[69;18;233;1;0;1_",  "é")]   // ´+e → é
    [InlineData("\x1b[73;23;237;1;0;1_",  "í")]   // ´+i → í
    [InlineData("\x1b[79;24;243;1;0;1_",  "ó")]   // ´+o → ó
    [InlineData("\x1b[85;22;250;1;0;1_",  "ú")]   // ´+u → ú
    // Dead grave (`) + vowel
    [InlineData("\x1b[65;30;224;1;0;1_",  "à")]   // `+a → à
    [InlineData("\x1b[69;18;232;1;0;1_",  "è")]   // `+e → è
    [InlineData("\x1b[85;22;249;1;0;1_",  "ù")]   // `+u → ù
    // Dead circumflex (^) + vowel
    [InlineData("\x1b[65;30;226;1;0;1_",  "â")]   // ^+a → â
    [InlineData("\x1b[69;18;234;1;0;1_",  "ê")]   // ^+e → ê  (French)
    [InlineData("\x1b[73;23;238;1;0;1_",  "î")]   // ^+i → î
    [InlineData("\x1b[79;24;244;1;0;1_",  "ô")]   // ^+o → ô
    [InlineData("\x1b[85;22;251;1;0;1_",  "û")]   // ^+u → û
    // Dead umlaut (¨) + vowel — same characters as direct German/Swedish keys
    [InlineData("\x1b[65;30;228;1;0;1_",  "ä")]   // ¨+a → ä  (same codepoint as Swedish/German ä)
    [InlineData("\x1b[79;24;246;1;0;1_",  "ö")]   // ¨+o → ö  (same codepoint as Swedish/German ö)
    [InlineData("\x1b[85;22;252;1;0;1_",  "ü")]   // ¨+u → ü  (same codepoint as German ü)
    [InlineData("\x1b[69;18;235;1;0;1_",  "ë")]   // ¨+e → ë
    // Dead tilde (~) + consonant/vowel
    [InlineData("\x1b[78;49;241;1;0;1_",  "ñ")]   // ~+n → ñ  (Spanish)
    [InlineData("\x1b[65;30;227;1;0;1_",  "ã")]   // ~+a → ã  (Portuguese)
    [InlineData("\x1b[79;24;245;1;0;1_",  "õ")]   // ~+o → õ  (Portuguese)
    public void TryTranslateWin32InputSequence_DeadKeyComposed_DecodesCorrectly(
        string sequence, string expectedText)
    {
        // Dead-key compositions that produce international characters must survive
        // the Win32-to-VT translation pipeline without loss or substitution.
        var handled = WindowsConsoleDriver.TryTranslateWin32InputSequence(sequence, out var bytes);

        Assert.True(handled, "sequence must be recognized as a Win32 input event");
        Assert.Equal(expectedText, Encoding.UTF8.GetString(bytes));
    }

    // =========================================================================
    // Section 7: TryTranslateWin32InputSequence — AltGr combinations
    //
    // Right-Alt (AltGr) on Windows sets RIGHT_ALT_PRESSED(0x0001) together with
    // LEFT_CTRL_PRESSED(0x0008), giving controlState = 0x0009 = 9.
    //
    // Many European layouts use AltGr to produce characters that have no
    // dedicated physical key on the host's native layout (€, @, {, }, |, …).
    //
    // The driver must NOT emit ESC+char for AltGr combos (that is only for
    // standalone Left-Alt) — it must return the plain character.
    // =========================================================================

    [Theory]
    // controlState = RIGHT_ALT_PRESSED(1) | LEFT_CTRL_PRESSED(8) = 9
    [InlineData("\x1b[69;18;8364;1;9;1_",   "€")]   // AltGr+E = € (Swedish, Finnish, German, …)
    [InlineData("\x1b[50;3;64;1;9;1_",      "@")]    // AltGr+2 = @ (Swedish/Finnish layout)
    [InlineData("\x1b[52;5;36;1;9;1_",      "$")]    // AltGr+4 = $ (some layouts)
    [InlineData("\x1b[53;6;8364;1;9;1_",    "€")]    // AltGr+5 = € (UK layout)
    [InlineData("\x1b[219;26;123;1;9;1_",   "{")]    // AltGr+Å-key = { (Swedish/Finnish)
    [InlineData("\x1b[221;27;125;1;9;1_",   "}")]    // AltGr+^-key = } (Swedish/Finnish)
    [InlineData("\x1b[186;39;124;1;9;1_",   "|")]    // AltGr+Ö-key = | (Swedish/Finnish)
    public void TryTranslateWin32InputSequence_AltGr_DecodesCorrectly(
        string sequence, string expectedText)
    {
        // AltGr is represented in the Win32 forwarded sequence as controlState=9
        // (RIGHT_ALT + LEFT_CTRL).  The character must arrive as the plain glyph,
        // not as ESC+glyph (which would be misinterpreted as an Alt+key shortcut).
        var handled = WindowsConsoleDriver.TryTranslateWin32InputSequence(sequence, out var bytes);

        Assert.True(handled, "sequence must be recognized as a Win32 input event");
        Assert.Equal(expectedText, Encoding.UTF8.GetString(bytes));
    }

    // =========================================================================
    // Section 7b: TryTranslateWin32InputSequence — general sequence properties
    // =========================================================================

    [Theory]
    [InlineData("\x1b[65;30;97;1;0;3_",   "aaa")]   // repeatCount = 3 for ASCII
    [InlineData("\x1b[219;26;229;1;0;2_", "åå")]    // repeatCount = 2 for Swedish å
    [InlineData("\x1b[69;18;8364;1;9;3_", "€€€")]   // repeatCount = 3 for AltGr+E = €
    public void TryTranslateWin32InputSequence_WithRepeatCount_RepeatsChar(
        string sequence, string expectedText)
    {
        var handled = WindowsConsoleDriver.TryTranslateWin32InputSequence(sequence, out var bytes);

        Assert.True(handled);
        Assert.Equal(expectedText, Encoding.UTF8.GetString(bytes));
    }

    [Fact]
    public void TryTranslateWin32InputSequence_KeyUp_ProducesNoBytes()
    {
        // keyDown field == 0 → key-up event; must be silently consumed
        var handled = WindowsConsoleDriver.TryTranslateWin32InputSequence(
            "\x1b[219;26;229;0;0;1_", out var bytes);

        Assert.True(handled);
        Assert.Empty(bytes);
    }

    [Theory]
    [InlineData("")]                         // empty string
    [InlineData("hello")]                    // plain text (not an escape sequence)
    [InlineData("\x1b[A")]                   // cursor-move sequence (not Win32 input)
    [InlineData("\x1b[65;30;97;1;0_")]       // missing final semicolon and repeat field
    public void TryTranslateWin32InputSequence_NonWin32Sequences_ReturnFalse(string sequence)
    {
        var handled = WindowsConsoleDriver.TryTranslateWin32InputSequence(sequence, out _);

        Assert.False(handled);
    }

    // =========================================================================
    // Section 8: Integration — TextBox correctly accepts international characters
    //
    // TextInputStep.Type() creates Hex1bKeyEvent(Hex1bKey.None, "å", None) for
    // non-ASCII chars that don't have a dedicated Hex1bKey entry.  The TextBox
    // AnyCharacter binding fires and the character is inserted at the cursor.
    //
    // These tests verify the complete Hex1bApp → TextBox node pipeline.
    // =========================================================================

    [Theory]
    [InlineData("åäö",      "Swedish ring-a, a-umlaut, o-umlaut")]
    [InlineData("Åäö",      "Swedish uppercase ring-a followed by lowercase")]
    [InlineData("üöä",      "German umlauts")]
    [InlineData("ß",        "German Eszett (sharp-s)")]
    [InlineData("éàùç",     "French accented characters")]
    [InlineData("ñ",        "Spanish n-tilde")]
    [InlineData("áéíóú",  "Acute-accented vowels (Portuguese/Spanish/Irish)")]
    [InlineData("æø",       "Danish/Norwegian ae-ligature and o-stroke")]
    [InlineData("café",     "Mixed ASCII and accented characters")]
    [InlineData("Ångström", "Scientific unit name with Swedish characters")]
    [InlineData("€",        "Euro sign (AltGr+E on most European keyboards)")]
    public async Task Integration_TextBox_AcceptsInternationalCharacters(
        string input, string _description)
    {
        var capturedText = "";

        using var workload = new Hex1bAppWorkloadAdapter();
        using var terminal = Hex1bTerminal.CreateBuilder()
            .WithWorkload(workload)
            .WithHeadless()
            .WithDimensions(80, 5)
            .Build();

        using var app = new Hex1bApp(
            ctx => Task.FromResult<Hex1bWidget>(
                ctx.VStack(v => [
                    v.TextBox(capturedText).OnTextChanged(args => capturedText = args.NewText)
                ])
            ),
            new Hex1bAppOptions { WorkloadAdapter = workload }
        );

        var runTask = app.RunAsync(TestContext.Current.CancellationToken);

        await new Hex1bTerminalInputSequenceBuilder()
            .WaitUntil(s => s.InAlternateScreen, TimeSpan.FromSeconds(5), "app started")
            .Type(input)
            .WaitUntil(s => s.ContainsText(input), TimeSpan.FromSeconds(5), $"international text visible ({_description})")
            .Ctrl().Key(Hex1bKey.C)
            .Build()
            .ApplyAsync(terminal, TestContext.Current.CancellationToken);

        await runTask;

        Assert.Equal(input, capturedText);
    }

    // =========================================================================
    // Section 9: Integration — cursor navigation through international text
    // =========================================================================

    [Fact]
    public async Task Integration_TextBox_CursorMovesByGraphemeCluster_ThroughSwedishChars()
    {
        // Typing "åäö" places the cursor at the end (index 3 in the C# string).
        // Pressing Left once must move back one full grapheme (ö, a single code
        // point), so that typing "X" inserts before ö, producing "åäXö".
        // If cursor movement were byte-based instead of cluster-based the X would
        // land at the wrong position and the assertion would fail.
        var capturedText = "";

        using var workload = new Hex1bAppWorkloadAdapter();
        using var terminal = Hex1bTerminal.CreateBuilder()
            .WithWorkload(workload)
            .WithHeadless()
            .WithDimensions(80, 5)
            .Build();

        using var app = new Hex1bApp(
            ctx => Task.FromResult<Hex1bWidget>(
                ctx.VStack(v => [
                    v.TextBox(capturedText).OnTextChanged(args => capturedText = args.NewText)
                ])
            ),
            new Hex1bAppOptions { WorkloadAdapter = workload }
        );

        var runTask = app.RunAsync(TestContext.Current.CancellationToken);

        await new Hex1bTerminalInputSequenceBuilder()
            .WaitUntil(s => s.InAlternateScreen, TimeSpan.FromSeconds(5), "app started")
            .Type("åäö")
            .WaitUntil(s => s.ContainsText("åäö"), TimeSpan.FromSeconds(5), "text entered")
            .Left()
            // No intermediate WaitUntil needed: cursor move is invisible on screen.
            // Type immediately and wait for the combined result.
            .Type("X")
            .WaitUntil(s => s.ContainsText("åäXö"), TimeSpan.FromSeconds(5), "X inserted before ö")
            .Ctrl().Key(Hex1bKey.C)
            .Build()
            .ApplyAsync(terminal, TestContext.Current.CancellationToken);

        await runTask;

        Assert.Equal("åäXö", capturedText);
    }

    [Fact]
    public async Task Integration_TextBox_BackspaceRemovesFullAccentedCluster()
    {
        // Pressing Backspace after typing "café" must remove the full 'é' grapheme
        // (one code point, U+00E9), leaving "caf".
        // If deletion were byte-based the UTF-8 trailing byte of é (0xA9) would
        // be removed leaving a corrupted codepoint.
        var capturedText = "";

        using var workload = new Hex1bAppWorkloadAdapter();
        using var terminal = Hex1bTerminal.CreateBuilder()
            .WithWorkload(workload)
            .WithHeadless()
            .WithDimensions(80, 5)
            .Build();

        using var app = new Hex1bApp(
            ctx => Task.FromResult<Hex1bWidget>(
                ctx.VStack(v => [
                    v.TextBox(capturedText).OnTextChanged(args => capturedText = args.NewText)
                ])
            ),
            new Hex1bAppOptions { WorkloadAdapter = workload }
        );

        var runTask = app.RunAsync(TestContext.Current.CancellationToken);

        await new Hex1bTerminalInputSequenceBuilder()
            .WaitUntil(s => s.InAlternateScreen, TimeSpan.FromSeconds(5), "app started")
            .Type("café")
            .WaitUntil(s => s.ContainsText("café"), TimeSpan.FromSeconds(5), "initial text entered")
            .Backspace()
            .WaitUntil(
                s => s.ContainsText("caf") && !s.ContainsText("café"),
                TimeSpan.FromSeconds(5),
                "é removed by backspace")
            .Ctrl().Key(Hex1bKey.C)
            .Build()
            .ApplyAsync(terminal, TestContext.Current.CancellationToken);

        await runTask;

        Assert.Equal("caf", capturedText);
    }

    [Fact]
    public async Task Integration_TextBox_MixedAsciiAndInternational_PreservesAllChars()
    {
        // Verify that mixing ordinary ASCII with accented characters doesn't corrupt
        // either.  This catches off-by-one bugs where an international char changes
        // the byte length but not the visible character count.
        const string input = "Hello, Wörld! Ångström 42";
        var capturedText = "";

        using var workload = new Hex1bAppWorkloadAdapter();
        using var terminal = Hex1bTerminal.CreateBuilder()
            .WithWorkload(workload)
            .WithHeadless()
            .WithDimensions(80, 5)
            .Build();

        using var app = new Hex1bApp(
            ctx => Task.FromResult<Hex1bWidget>(
                ctx.VStack(v => [
                    v.TextBox(capturedText).OnTextChanged(args => capturedText = args.NewText)
                ])
            ),
            new Hex1bAppOptions { WorkloadAdapter = workload }
        );

        var runTask = app.RunAsync(TestContext.Current.CancellationToken);

        await new Hex1bTerminalInputSequenceBuilder()
            .WaitUntil(s => s.InAlternateScreen, TimeSpan.FromSeconds(5), "app started")
            .Type(input)
            .WaitUntil(s => s.ContainsText(input), TimeSpan.FromSeconds(5), "full text visible")
            .Ctrl().Key(Hex1bKey.C)
            .Build()
            .ApplyAsync(terminal, TestContext.Current.CancellationToken);

        await runTask;

        Assert.Equal(input, capturedText);
    }
}
