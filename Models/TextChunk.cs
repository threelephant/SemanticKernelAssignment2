using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel.Data;

namespace SemanticKernelPlayground.Models;

public sealed class TextChunk
{
    [VectorStoreRecordKey]
    public string Key { get; set; } = string.Empty;

    [VectorStoreRecordData]
    [TextSearchResultName]
    public string FileName { get; set; } = string.Empty;

    [VectorStoreRecordData]
    public int ParagraphId { get; set; }

    [VectorStoreRecordData]
    [TextSearchResultValue]
    public string Text { get; set; } = string.Empty;

    [VectorStoreRecordVector(1536)]
    public ReadOnlyMemory<float> Embedding { get; set; }
}