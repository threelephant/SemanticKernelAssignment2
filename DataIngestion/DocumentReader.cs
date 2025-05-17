using System.Text.RegularExpressions;
using SemanticKernelPlayground.Models;

namespace SemanticKernelPlayground.DataIngestion;

public sealed class DocumentReader
{
    private static readonly string[] AllowedExts = { ".cs", ".md", ".txt", ".json" };

    public IEnumerable<TextChunk> Read(string root)
    {
        foreach (var file in Directory.EnumerateFiles(root, "*.*", SearchOption.AllDirectories))
        {
            var ext = Path.GetExtension(file)!;
            if (!AllowedExts.Contains(ext, StringComparer.OrdinalIgnoreCase)) continue;
            if (file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}") ||
                file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}") ||
                file.Contains($"{Path.DirectorySeparatorChar}.git{Path.DirectorySeparatorChar}"))
                continue;

            var text = File.ReadAllText(file);
            var parts = Regex.Split(text, @"\r?\n\s*\r?\n");
            for (var idx = 0; idx < parts.Length; idx++)
            {
                var p = parts[idx].Trim();
                if (string.IsNullOrWhiteSpace(p)) continue;

                yield return new TextChunk
                {
                    Key = $"{Path.GetFileName(file)}:{idx}",
                    FileName = file,
                    ParagraphId = idx,
                    Text = p
                };
            }
        }
    }
}