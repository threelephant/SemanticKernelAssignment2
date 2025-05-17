using System.ComponentModel;
using System.Text;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Data;
using Microsoft.SemanticKernel.Embeddings;
using SemanticKernelPlayground.Models;
#pragma warning disable CS0618

namespace SemanticKernelPlayground.Plugins;

public sealed class CodeSearchPlugin(
    IVectorStore store,
    ITextEmbeddingGenerationService embedder)
{
    [KernelFunction, Description("Search the CodeBase for relevant code snippets. Return file name, paragraph ID, snippet text, and relevance score.")]
    public async Task<string> SearchCodeAsync(
        string query,
        Kernel kernel,
        [Description("Optional file extension filter (e.g., 'cs' or 'py')")]
        string? fileExtension = null,
        CancellationToken ct = default)
    {
        var coll = store.GetCollection<string, TextChunk>("CodeBase");
        var textSearch = new VectorStoreTextSearch<TextChunk>(coll, embedder);

        var options = new TextSearchOptions { Top = 3 };
        var results = await textSearch.GetTextSearchResultsAsync(query, options, ct);

        var sb = new StringBuilder();
        await foreach (var r in results.Results.WithCancellation(ct))
        {
            if (!string.IsNullOrEmpty(fileExtension) &&
                r.Name != null &&
                !r.Name.EndsWith($".{fileExtension}", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }
            
            sb.AppendLine($"**{r.Name}**:\n{r.Value}\n");
        }

        return sb.Length == 0
            ? "No relevant snippets found."
            : sb.ToString();
    }
}