using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel.Embeddings;
using SemanticKernelPlayground.Models;

namespace SemanticKernelPlayground.DataIngestion;

    public sealed class DataUploader(
        IVectorStore store,
        ITextEmbeddingGenerationService embedder)
    {
        public async Task UploadAsync(
            string collectionName,
            IEnumerable<TextChunk> chunks,
            CancellationToken ct = default)
        {
            var collection = store.GetCollection<string, TextChunk>(collectionName);
            await collection.CreateCollectionIfNotExistsAsync(ct);

            foreach (var chunk in chunks)
            {
                chunk.Embedding = await embedder.GenerateEmbeddingAsync(chunk.Text, cancellationToken: ct);

                await collection.UpsertAsync(chunk, ct);
            }
        }
    }
