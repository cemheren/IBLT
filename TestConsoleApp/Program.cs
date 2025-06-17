// See https://aka.ms/new-console-template for more information
using Azure.Identity;
using ChecksumCosmosClient;
using Microsoft.Azure.Cosmos;

Console.WriteLine("Hello, World!");


DefaultAzureCredential credential = new();

CosmosClient client = new(
    accountEndpoint: "",
    tokenCredential: new DefaultAzureCredential()
);

var ibltExtender = new IBLTExtender<Resource>(client.WithCosmosClientExtensions<Resource>());

var totalIntegers = 100;

var sub = "abc";

await ibltExtender.GetAllResources(sub);


for (int i = 0; i < totalIntegers; i++)
{
    if (i >= 29 && i <= 32)
    {
        continue;
    }

    Console.WriteLine(i);

    await ibltExtender.UpsertItemAsync(new Resource(id:$"{i}", subscription: sub, name: $"{i}", location: "dev"));
}

