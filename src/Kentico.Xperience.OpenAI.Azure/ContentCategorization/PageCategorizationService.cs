using System;
using System.Collections.Generic;
using System.Linq;

using Azure;
using Azure.AI.OpenAI;

using CMS.Base;
using CMS.Core;
using CMS.DataEngine;
using CMS.DocumentEngine;
using CMS.FormEngine;
using CMS.Helpers;

using Kentico.Xperience.OpenAI.Azure;

//[assembly: RegisterImplementation(typeof(IPageCategorizationService), typeof(PageCategorizationService), Lifestyle = Lifestyle.Singleton, Priority = RegistrationPriority.SystemDefault)]

namespace Kentico.Xperience.OpenAI.Azure
{
    /// <summary>
    /// Implementation of the <see cref="IPageCategorizationService"/>.
    /// </summary>
    public class PageCategorizationService //: IPageCategorization
    {
        private const string DEPLOYMENT_NAME_KEY = "KenticoXperienceAzureOpenAIDeploymentName";
        private const string API_ENPOINT_KEY = "KenticoXperienceAzureOpenAIAPIEndpoint";
        private const string API_KEY_KEY = "KenticoXperienceAzureOpenAIAPIKey";
        private const string CLASSIFICATION_ENABLED_KEY = "KenticoXperienceAzureOpenAIEnableContentCategorization";
        private const string TEMPERATURE_KEY = "CMSPageCategorization:OpenAIClient:Temperature";

        private const string DEFAULT_SYSTEM_PROMPT = "You are a categorization agent, using provided categories to categorize text; you remove any end-of-sentence punctuations; data has format \"<field_name_1>:<field_value_1>;<field_name_2>:<field_value_2>\"; respond in the format \"category_name_1;category_name_2\"; exclusively use the provided category names and delimiters; use whitespace only within category names; you always have to pick at least 1 category";

        private const int MAX_TOKENS = 400;
        private const float DEFAULT_TEMPERATURE = 0.5f;

        private readonly ISettingsService settingsService;
        private readonly IAppSettingsService appSettingsService;


        /// <inheritdoc/>
        public bool IsCategorizationEnabled => settingsService[CLASSIFICATION_ENABLED_KEY].ToBoolean(false);


        /// <summary>
        /// Creates new instance of <see cref="PageCategorizationService"/>.
        /// </summary>
        /// <param name="settingsService"></param>
        public PageCategorizationService(ISettingsService settingsService, IAppSettingsService appSettingsService)
        {
            this.settingsService = settingsService;
            this.appSettingsService = appSettingsService;
        }


        /// <inheritdoc/>
        private string GetSystemPrompt(IEnumerable<string> categories)
        {
            return DEFAULT_SYSTEM_PROMPT + GetCategories(categories);
        }


        /// <inheritdoc/>
        public IEnumerable<string> CategorizePage(TreeNode treeNode, IEnumerable<string> categories)
        {
            if (treeNode == null)
            {
                throw new ArgumentNullException(nameof(treeNode));
            }
            if (categories == null)
            {
                throw new ArgumentNullException(nameof(categories));
            }
            if (!IsCategorizationEnabled)
            {
                throw new InvalidOperationException("Content Categorization is disabled.");
            }
            if (!categories.Any())
            {
                throw new ArgumentException($"{nameof(categories)} cannot be empty.");
            }

            var treeNodeData = ExtractTreeNodeData(treeNode);

            var client = CreateClient();
            var chatCompletionsOptions = InitializeChatCompletionsOptions(treeNodeData, categories);
            var response = client.GetChatCompletions(chatCompletionsOptions);

            // Process the response
            return new List<string>() { response.Value.Choices[0].Message.Content };
        }


        private OpenAIClient CreateClient()
        {
            var apiEndpoint = settingsService[API_ENPOINT_KEY].ToString(string.Empty);
            var apiKey = settingsService[API_KEY_KEY].ToString(string.Empty);

            if (string.IsNullOrEmpty(apiEndpoint) || string.IsNullOrEmpty(apiKey))
            {
                throw new InvalidOperationException($"The Azure OpenAI Content Categorization service API endpoint and key are not configured correctly.");
            }

            return new OpenAIClient(new Uri(apiEndpoint), new AzureKeyCredential(apiKey));
        }


        private string ExtractTreeNodeData(TreeNode treeNode)
        {
            var fields = GetFields(treeNode)
                .Where((field) => !string.IsNullOrEmpty(field.value))
                .Select(field => $"<{field.name}>:{field.value}");
            var textRepresentation = string.Join(";", fields);

            return "Categorize the following data:" + textRepresentation;
        }


        private ChatCompletionsOptions InitializeChatCompletionsOptions(string treeNodeData, IEnumerable<string> categories)
        {
            var deploymentName = settingsService[DEPLOYMENT_NAME_KEY].ToString(string.Empty);

            if (string.IsNullOrEmpty(deploymentName))
            {
                throw new InvalidOperationException($"The Azure OpenAI Content Categorization service deployment name is not configured correctly");
            }

            var temperature = ValidationHelper.GetFloat(appSettingsService[TEMPERATURE_KEY], DEFAULT_TEMPERATURE);

            var chatCompletionsOptions = new ChatCompletionsOptions()
            {
                DeploymentName = deploymentName,
                Messages =
                {
                    new ChatRequestSystemMessage(GetSystemPrompt(categories)),
                    new ChatRequestUserMessage(treeNodeData),
                },
                MaxTokens = MAX_TOKENS,
                Temperature = temperature,
            };

            return chatCompletionsOptions;
        }


        private IEnumerable<(string name, string value)> GetFields(TreeNode treeNode)
        {
            string treeNodeClassName = treeNode.ClassName;

            var formInfo = FormHelper.GetFormInfo(treeNodeClassName, false);

            var textColumns = formInfo.GetFields(FieldDataType.LongText).Concat(formInfo.GetFields(FieldDataType.Text));

            var fields = textColumns.Select(text => (text.Caption, treeNode.GetStringValue(text.Name, "")));

            return fields;
        }


        private string GetCategories(IEnumerable<string> categories) => "Category names:" + string.Join(";", categories);

        private void ProcessResponse(Response<ChatCompletions> response, IEnumerable<string> categories)
        {
            var validCategories = categories.ToHashSet();

            var responseContent = response.Value.Choices[0].Message.Content;

            var cattegoryNames = responseContent.TrimEnd('.').Split(';').Select(category => category.Trim(' ')).Distinct();

            // Filter the valid/invalid categories
            var correctlyIdentified = cattegoryNames.Where(category => validCategories.Contains(category));
            var other = cattegoryNames.Where((category) => !validCategories.Contains(category));
        }
    }
}
