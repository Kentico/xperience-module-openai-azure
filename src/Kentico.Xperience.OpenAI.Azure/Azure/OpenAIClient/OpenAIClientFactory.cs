using System;
using System.ClientModel;

using Azure.AI.OpenAI;

namespace Kentico.Xperience.OpenAI.Azure
{
    /// <summary>
    /// Factory responsible for creating instances of the <see cref="OpenAIClient"/>.
    /// </summary>
    internal class OpenAIClientFactory : IOpenAIClientFactory
    {
        /// <inheritdoc/>
        public AzureOpenAIClient GetOpenAIClient(string apiEndpoint, string apiKey)
        {
            if (string.IsNullOrEmpty(apiEndpoint) || string.IsNullOrEmpty(apiKey))
            {
                throw new InvalidOperationException($"The Azure OpenAI Content Categorization service API endpoint and key are not configured correctly.");
            }

            return new AzureOpenAIClient(new Uri(apiEndpoint), new ApiKeyCredential(apiKey));
        }
    }
}
