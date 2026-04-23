using Hex1b;
using Hex1b.Widgets;

var text = string.Empty;

using var app = new Hex1bApp(ctx =>
    ctx.VStack(v => [
        v.Text("TextBox international input repro"),
        v.Text("Type international characters such as å, ä, ö, ñ, é, or € into the TextBox below."),
        v.Text("Press Ctrl+C to exit."),
        v.Text(""),
        v.TextBox(text).OnTextChanged(args => text = args.NewText),
        v.Text(""),
        v.Text($"Current value: {(string.IsNullOrEmpty(text) ? "(empty)" : text)}")
    ]));

await app.RunAsync();
