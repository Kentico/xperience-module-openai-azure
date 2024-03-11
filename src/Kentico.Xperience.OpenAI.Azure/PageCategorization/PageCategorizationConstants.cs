namespace Kentico.Xperience.OpenAI.Azure
{
    internal static class PageCategorizationConstants
    {
        /// <summary>
        /// Name of the setting which specifies the deployment name for the page categorization.
        /// </summary>
        public const string DEPLOYMENT_NAME_KEY = "KenticoXperienceAzureOpenAIContentCategorizationDeploymentName";


        /// <summary>
        /// Name of the setting which specifies the API endpoint URL for the page categorization.
        /// </summary>
        public const string API_ENDPOINT_KEY = "KenticoXperienceAzureOpenAIAPIEndpoint";


        /// <summary>
        /// Name of the setting which specifies the API key for the page categorization. The key is encrypted.
        /// </summary>
        public const string API_KEY_KEY = "KenticoXperienceAzureOpenAIAPIKey";


        /// <summary>
        /// Name of the setting which determines the temperature for content categorization.
        /// </summary>
        public const string TEMPERATURE_KEY = "CMSContentCategorizationTemperature";


        /// <summary>
        /// Default system prompt used for initializing content categorization.
        /// </summary>
        public const string DEFAULT_SYSTEM_PROMPT = "You are a text classification assistant, using provided categories to classify text. Remove any end-of-sentence punctuations. Respond in the format \"category_name_1;category_name_2\". Exclusively use the provided category names and delimiters. Use whitespace only within category names. Ideally pick one category.\n";


        /// <summary>
        /// The maximum number of tokens that can be used in a single OpenAI API response.
        /// </summary>
        public const int MAX_TOKENS = 100;


        /// <summary>
        /// The default temperature setting used for generating responses in the page categorization.
        /// </summary>
        public const float DEFAULT_TEMPERATURE = 0.5f;


        /// <summary>
        /// The default delimiter used in splitting or joining strings.
        /// </summary>
        public const string DELIMITER = ";";
    }
}
