using System;

using OpenAI;

namespace Kentico.Xperience.OpenAI.Azure
{
    /// <summary>
    /// Factory responsible for creating instances of the <see cref="OpenAIClient"/>.
    /// </summary>
    internal class OpenAIClientFactory : IOpenAIClientFactory
    {
        /// <inheritdoc/>
        public OpenAIClient GetOpenAIClient(string apiEndpoint, string apiKey)
        {
            if (string.IsNullOrEmpty(apiEndpoint) || string.IsNullOrEmpty(apiKey))
            {
                throw new InvalidOperationException($"The Azure OpenAI Content Categorization service API endpoint and key are not configured correctly.");
            }

            return new OpenAIClient(apiKey);
        }
    }
}
