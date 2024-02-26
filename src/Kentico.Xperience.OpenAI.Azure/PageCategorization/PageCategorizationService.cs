using System;
using System.Collections.Concurrent;
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
        private const string DEFAULT_SYSTEM_PROMPT = "You are a categorization agent, using provided categories to categorize text. Remove any end-of-sentence punctuations. Data has format \"field_name_1:<field_value_1>;field_name_2:<field_value_2>\". Respond in the format \"category_name_1;category_name_2\". Exclusively use the provided category names and delimiters. Use whitespace only within category names. Ideally pick one category.\n";

        private const int MAX_TOKENS = 400;
        private const float DEFAULT_TEMPERATURE = 0.5f;
        private const string delimiter = ";";

        private readonly ISettingsService settingsService;
        private readonly IAppSettingsService appSettingsService;
        private readonly ICategoryInfoProvider categoryInfoProvider;
        private readonly IOpenAIClientFactory openAIClientFactory;
        private readonly ILocalizationService localizationService;

        private ConcurrentDictionary<string, string> localizedNamesByDisplayNames = new ConcurrentDictionary<string, string>();

        /// <summary>
        /// Initializes a new instance of <see cref="CMS.DocumentEngine.PageCategorizationService"/> class.
        /// </summary>
        /// <param name="settingsService"></param>
        public PageCategorizationService(ISettingsService settingsService, IAppSettingsService appSettingsService, ICategoryInfoProvider categoryInfoProvider, IOpenAIClientFactory openAIClientFactory, ILocalizationService localizationService)
        {
            this.settingsService = settingsService;
            this.appSettingsService = appSettingsService;
            this.categoryInfoProvider = categoryInfoProvider;
            this.openAIClientFactory = openAIClientFactory;
            this.localizationService = localizationService;
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
            if (!categoryIdentifiers.Any())
            {
                throw new ArgumentException($"{nameof(categoryIdentifiers)} cannot be empty.");
            }

            var client = CreateClient();

            string treeNodeData = ExtractTreeNodeData(treeNode);

            var chatCompletionsOptions = InitializeChatCompletionsOptions(treeNodeData, categoryIdentifiers);
            var response = client.GetChatCompletions(chatCompletionsOptions);

            return ProcessResponse(response, categoryIdentifiers);
        }


        private OpenAIClient CreateClient()
        {
            string apiEndpoint = settingsService[PageCategorizationConstants.API_ENDPOINT_KEY];
            string apiKey = EncryptionHelper.DecryptData(settingsService[PageCategorizationConstants.API_KEY_KEY]);

            return openAIClientFactory.GetOpenAIClient(apiEndpoint, apiKey);
        }


        private string ExtractTreeNodeData(TreeNode treeNode)
        {
            var fields = GetFields(treeNode)
                .Where((field) => !string.IsNullOrEmpty(field.value))
                .Select(field => $"{field.name}:{field.value}");
            string textRepresentation = string.Join($"{delimiter}\n", fields);

            return "Categorize the following data:\n" + textRepresentation;
        }


        private ChatCompletionsOptions InitializeChatCompletionsOptions(string treeNodeData, IEnumerable<int> categoryIdentifiers)
        {
            string deploymentName = settingsService[PageCategorizationConstants.DEPLOYMENT_NAME_KEY];

            if (string.IsNullOrEmpty(deploymentName))
            {
                throw new InvalidOperationException($"The Azure OpenAI Content Categorization service deployment name is not configured correctly.");
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

            var fields = textColumns.Select(text => (text.Caption, treeNode.GetStringValue(text.Name, string.Empty)));

            return fields;
        }


        private PageCategorizationResult ProcessResponse(Response<ChatCompletions> response, IEnumerable<int> categoryIdentifiers)
        {
            string responseContent = response.Value.Choices[0].Message.Content;

            var categoriesByName = GetCategories(categoryIdentifiers).ToLookup(category => GetLocalized(category.CategoryDisplayName), category => category.CategoryID);
            var cattegoryNames = responseContent.TrimEnd('.')
                .Split(delimiter[0])
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
            var categories = GetCategories(categoryIdentifiers).Select(category => GetLocalized(category.CategoryDisplayName, true));
            return "Category names:" + string.Join(delimiter, categories);
        }


        private string GetLocalized(string categoryDisplayName, bool replace = false)
        {
            if (replace || !localizedNamesByDisplayNames.ContainsKey(categoryDisplayName))
            {
                string localizedName = localizationService.GetString(categoryDisplayName);
                localizedNamesByDisplayNames[categoryDisplayName] = localizedName;
            }

            return localizedNamesByDisplayNames[categoryDisplayName];
        }
    }
}
