using System;

using Azure;
using Azure.AI.OpenAI;

using CMS;
using CMS.Core;

using Kentico.Xperience.OpenAI.Azure;

[assembly: RegisterImplementation(typeof(IOpenAIClientFactory), typeof(OpenAIClientFactory), Priority = RegistrationPriority.SystemDefault, Lifestyle = Lifestyle.Singleton)]

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
