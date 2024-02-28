using System;

using Azure.AI.OpenAI;

namespace Kentico.Xperience.OpenAI.Azure
{
    /// <summary>
    /// Defines a factory for creating instances of <see cref="OpenAIClient"/>.
    /// </summary>
    internal interface IOpenAIClientFactory
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="OpenAIClient"/> class using the specified API endpoint and API key.
        /// </summary>
        /// <param name="apiKey">The subscription key for accessing the Azure OpenAI Content Categorization service.</param>
        /// <param name="apiEndpoint">The API endpoint URL for the Azure OpenAI Content Categorization service.</param>
        /// <returns>An instance of the <see cref="OpenAIClient"/> configured with the specified endpoint and key.</returns>
        /// <exception cref="InvalidOperationException">Thrown when either <paramref name="apiKey"/> or <paramref name="apiEndpoint"/> is null or empty.</exception>
        OpenAIClient GetOpenAIClient(string apiKey, string apiEndpoint);
    }
}
