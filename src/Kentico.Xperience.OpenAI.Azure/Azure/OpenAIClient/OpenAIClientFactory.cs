using System;

using Azure;
using Azure.AI.OpenAI;

namespace Kentico.Xperience.OpenAI.Azure
{
    internal class OpenAIClientFactory : IOpenAIClientFactory
    {
        /// <inheritdoc/>
        public OpenAIClient GetOpenAIClient(string apiEndpoint, string apiKey)
        {
            if (string.IsNullOrEmpty(apiEndpoint) || string.IsNullOrEmpty(apiKey))
            {
                throw new InvalidOperationException($"The Azure OpenAI Content Categorization service API endpoint and key are not configured correctly.");
            }

            return new OpenAIClient(new Uri(apiEndpoint), new AzureKeyCredential(apiKey));
        }
    }
}
