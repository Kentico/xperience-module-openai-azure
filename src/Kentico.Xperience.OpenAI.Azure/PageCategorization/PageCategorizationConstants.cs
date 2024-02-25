using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kentico.Xperience.OpenAI.Azure
{
    internal static class PageCategorizationConstants
    {
        public const string DEPLOYMENT_NAME_KEY = "KenticoXperienceAzureOpenAIDeploymentName";
        public const string API_ENDPOINT_KEY = "KenticoXperienceAzureOpenAIAPIEndpoint";
        public const string API_KEY_KEY = "KenticoXperienceAzureOpenAIAPIKey";
        public const string CLASSIFICATION_ENABLED_KEY = "KenticoXperienceAzureOpenAIEnableContentCategorization";
        public const string TEMPERATURE_KEY = "CMSContentCategorizationTemperature";
    }
}
