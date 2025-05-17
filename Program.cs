using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;
using Microsoft.SemanticKernel.Embeddings;

using SemanticKernelPlayground.DataIngestion;
using SemanticKernelPlayground.Models;
using SemanticKernelPlayground.Plugins;

#pragma warning disable SKEXP0010
#pragma warning disable SKEXP0001

var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddJsonFile("appsettings.Development.json", optional: false, reloadOnChange: true)
    .Build();

var modelName = configuration["ModelName"]
    ?? throw new ApplicationException("ModelName not found");
var embedding = configuration["EmbeddingModel"]
    ?? throw new ApplicationException("EmbeddingModel not found");
var endpoint = configuration["Endpoint"]
    ?? throw new ApplicationException("Endpoint not found");
var apiKey = configuration["ApiKey"]
    ?? throw new ApplicationException("ApiKey not found");

var builder = Kernel.CreateBuilder()
    .AddAzureOpenAIChatCompletion(modelName, endpoint, apiKey)
    .AddAzureOpenAITextEmbeddingGeneration(embedding, endpoint, apiKey)
    .AddInMemoryVectorStore();

builder.Services.AddLogging(cfg => cfg.AddConsole());
builder.Services.AddLogging(cfg => cfg.SetMinimumLevel(LogLevel.Information));
builder.Services.AddSingleton<CodeSearchPlugin>();

var kernel = builder.Build();
kernel.ImportPluginFromType<CodeSearchPlugin>();
var fileList = new List<string>()
{
    "SampleData/Bobby-Anna-facts.txt",
    "SampleData/Carl-facts.txt"
};

var reader = new DocumentReader();
var chunks = reader.Read(@"C:\Users\peter\source\repos\SemanticKernelPlaygroundVenya")   // adjust path to your repo root
    .ToList();

var uploader = new DataUploader(
    kernel.GetRequiredService<IVectorStore>(),
    kernel.GetRequiredService<ITextEmbeddingGenerationService>());

Console.WriteLine("Generating embeddings for code docs…");
await uploader.UploadAsync("CodeBase", chunks);
Console.WriteLine($"Ingested {chunks.Count} chunks into 'CodeBase'.");
Console.WriteLine("Ask me anything about the code!\n");

builder.Services.AddSingleton<DataUploader>();
var chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();

AzureOpenAIPromptExecutionSettings openAiPromptExecutionSettings = new()
{
    FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
};

var history = new ChatHistory();

do
{
    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.Write("Me > ");
    Console.ResetColor();

    var userInput = Console.ReadLine();
    if (userInput == "exit") break;

    history.AddUserMessage(userInput!);

    var streamingResponse = chatCompletionService
        .GetStreamingChatMessageContentsAsync(
            history,
            openAiPromptExecutionSettings,
            kernel);

    Console.ForegroundColor = ConsoleColor.Green;
    Console.Write("Agent > ");
    Console.ResetColor();

    var fullResponse = "";
    await foreach (var chunk in streamingResponse)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.Write(chunk.Content);
        Console.ResetColor();
        fullResponse += chunk.Content;
    }
    Console.WriteLine();

    history.AddMessage(AuthorRole.Assistant, fullResponse);

} while (true);
#pragma warning restore SKEXP0010
#pragma warning restore SKEXP0001
