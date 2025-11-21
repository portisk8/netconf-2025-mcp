using Azure.AI.OpenAI;
using Microsoft.Agents.AI;
using OpenAI;


string? openAiKey = Environment.GetEnvironmentVariable("OpenAI-KEY");
string model = "gpt-5-nano";
if (string.IsNullOrWhiteSpace(openAiKey))
{
    Console.WriteLine("Error: La variable de entorno 'OpenAI-KEY' no está configurada.");
    return;
}

AIAgent agent = new OpenAIClient(openAiKey)
    .GetChatClient(model)
    .CreateAIAgent(instructions: "You are good at telling jokes.", name: "Joker");


Console.WriteLine(await agent.RunAsync("Tell me a joke about a pirate."));
