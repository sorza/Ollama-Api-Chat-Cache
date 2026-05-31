using Microsoft.Extensions.AI;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using OllamaSharp;

const string openAiKey = "CHAVE DO OPENAI";
var uri = new Uri("http://localhost:11434");

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

IDistributedCache cache = new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));

var client =
    app.Environment.IsDevelopment()
        ? new OllamaApiClient(uri, "phi3")
        : new OpenAI.Chat.ChatClient("gpt-4o-mini", openAiKey)
            .AsIChatClient();

var cachedClient = new ChatClientBuilder(client)
    .UseDistributedCache(cache)
    .Build();

app.MapPost("/", async (Question question) =>
{
    var response = await client.GetResponseAsync(question.prompt);
    return Results.Ok(response.Text);
});


app.MapPost("/v2", async (Question question) =>
{
    var result = await client.GetResponseAsync(
        [
            new ChatMessage(ChatRole.System, "Você é um especialista em clima e meteorologia. Responda-me com somente uma sentença."),
            new ChatMessage(ChatRole.User, question.prompt)
        ]);
    return Results.Ok(result.Text);
});

app.MapPost("/v3", async (Question question) =>
{
    var result = await cachedClient.GetResponseAsync(
        [
            new ChatMessage(ChatRole.System, "Você é um especialista em clima e meteorologia. Responda-me com somente uma sentença."),
            new ChatMessage(ChatRole.User, question.prompt)
        ]);
    return Results.Ok(result.Text);
});

app.Run();

public record Question(string prompt);
