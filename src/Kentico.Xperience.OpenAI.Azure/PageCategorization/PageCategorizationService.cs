using System;
using System.Collections.Generic;
using System.Linq;

using Azure;
using Azure.AI.OpenAI;

using CMS;
using CMS.Core;
using CMS.DataEngine;
using CMS.DocumentEngine;
using CMS.DocumentEngine.Internal;
using CMS.FormEngine;
using CMS.Helpers;
using CMS.Taxonomy;

using Kentico.Xperience.OpenAI.Azure;

[assembly: RegisterImplementation(typeof(IPageCategorizationService), typeof(PageCategorizationService), Lifestyle = Lifestyle.Singleton, Priority = RegistrationPriority.SystemDefault)]

namespace Kentico.Xperience.OpenAI.Azure
{
    internal class PageCategorizationService : IPageCategorizationService
    {
        private const string DEFAULT_SYSTEM_PROMPT = "You are a categorization agent, using provided categories to categorize text. Remove any end-of-sentence punctuations. Data has format \"field_name_1:<field_value_1>;field_name_2:<field_value_2>\". Respond in the format \"category_name_1;category_name_2\"; Exclusively use the provided category names and delimiters. Use whitespace only within category names. Ideally pick one category.\n";

        private const int MAX_TOKENS = 400;
        private const float DEFAULT_TEMPERATURE = 0.5f;

        private readonly ISettingsService settingsService;
        private readonly IAppSettingsService appSettingsService;
        private readonly ICategoryInfoProvider categoryInfoProvider;
        private readonly IOpenAIClientFactory openAIClientFactory;


        /// <inheritdoc/>
        public bool IsCategorizationEnabled => true;


        /// <summary>
        /// Initializes a new instance of <see cref="CMS.DocumentEngine.PageCategorizationService"/> class.
        /// </summary>
        /// <param name="settingsService"></param>
        public PageCategorizationService(ISettingsService settingsService, IAppSettingsService appSettingsService, ICategoryInfoProvider categoryInfoProvider, IOpenAIClientFactory openAIClientFactory)
        {
            this.settingsService = settingsService;
            this.appSettingsService = appSettingsService;
            this.categoryInfoProvider = categoryInfoProvider;
            this.openAIClientFactory = openAIClientFactory;
        }


        /// <inheritdoc/>
        private string GetSystemPrompt(IEnumerable<int> categoryIdentifiers) => DEFAULT_SYSTEM_PROMPT + GetCategoryNames(categoryIdentifiers);


        /// <inheritdoc/>
        public PageCategorizationResult CategorizePage(TreeNode treeNode, IEnumerable<int> categoryIdentifiers)
        {
            if (treeNode == null)
            {
                throw new ArgumentNullException(nameof(treeNode));
            }
            if (categoryIdentifiers == null)
            {
                throw new ArgumentNullException(nameof(categoryIdentifiers));
            }
            if (!IsCategorizationEnabled)
            {
                throw new InvalidOperationException("Content Categorization is disabled.");
            }
            if (!categoryIdentifiers.Any())
            {
                throw new ArgumentException($"{nameof(categoryIdentifiers)} cannot be empty.");
            }

            string treeNodeData = ExtractTreeNodeData(treeNode);

            var client = CreateClient();
            var chatCompletionsOptions = InitializeChatCompletionsOptions(treeNodeData, categoryIdentifiers);
            var response = client.GetChatCompletions(chatCompletionsOptions);

            // Process the response
            return ProcessResponse(response, categoryIdentifiers);
        }


        private OpenAIClient CreateClient()
        {
            string apiEndpoint = settingsService[PageCategorizationConstants.API_ENDPOINT_KEY];
            string apiKey = settingsService[PageCategorizationConstants.API_KEY_KEY];

            return openAIClientFactory.GetOpenAIClient(apiKey, apiEndpoint);
        }


        private string ExtractTreeNodeData(TreeNode treeNode)
        {
            var fields = GetFields(treeNode)
                .Where((field) => !string.IsNullOrEmpty(field.value))
                .Select(field => $"<{field.name}>:{field.value}");
            string textRepresentation = string.Join(";", fields);

            return "Categorize the following data:" + textRepresentation;
        }


        private ChatCompletionsOptions InitializeChatCompletionsOptions(string treeNodeData, IEnumerable<int> categoryIdentifiers)
        {
            string deploymentName = "gpt-35-turbo";

            if (string.IsNullOrEmpty(deploymentName))
            {
                throw new InvalidOperationException($"The Azure OpenAI Content Categorization service deployment name is not configured correctly");
            }

            float temperature = ValidationHelper.GetFloat(appSettingsService[PageCategorizationConstants.TEMPERATURE_KEY], DEFAULT_TEMPERATURE);

            var chatCompletionsOptions = new ChatCompletionsOptions()
            {
                DeploymentName = deploymentName,
                Messages =
                {
                    new ChatRequestSystemMessage(GetSystemPrompt(categoryIdentifiers)),
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


        private PageCategorizationResult ProcessResponse(Response<ChatCompletions> response, IEnumerable<int> categoryIdentifiers)
        {
            string responseContent = response.Value.Choices[0].Message.Content;

            var categoriesByName = GetCategories(categoryIdentifiers).ToLookup(category => category.CategoryDisplayName, category => category.CategoryID);
            var cattegoryNames = responseContent.TrimEnd('.')
                .Split(';')
                .Select(category => category.Trim())
                .Distinct();

            var correctlyIdentified = cattegoryNames.Where(category => categoriesByName.Contains(category)).SelectMany(name => categoriesByName[name]);
            var other = cattegoryNames.Where((category) => !categoriesByName.Contains(category));

            return new PageCategorizationResult
            {
                Categories = correctlyIdentified,
                UnknownCategories = other
            };
        }


        private IEnumerable<CategoryInfo> GetCategories(IEnumerable<int> categoryIdentifiers) => categoryIdentifiers.Select(categoryInfoProvider.Get);


        private string GetCategoryNames(IEnumerable<int> categoryIdentifiers)
        {
            var categories = GetCategories(categoryIdentifiers).Select(category => category.CategoryDisplayName);
            return "Category names:" + string.Join(";", categories);
        }
    }
}
