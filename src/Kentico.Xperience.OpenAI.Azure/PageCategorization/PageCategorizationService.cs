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
    /// <summary>
    /// Service responsible for categorization of pages.
    /// </summary>
    internal class PageCategorizationService : IPageCategorizationService
    {
        private readonly ISettingsService settingsService;
        private readonly IAppSettingsService appSettingsService;
        private readonly ICategoryInfoProvider categoryInfoProvider;
        private readonly IOpenAIClientFactory openAIClientFactory;
        private readonly ILocalizationService localizationService;

        /// <summary>
        /// Initializes a new instance of <see cref="PageCategorizationService"/> class.
        /// </summary>
        public PageCategorizationService(ISettingsService settingsService, IAppSettingsService appSettingsService, ICategoryInfoProvider categoryInfoProvider, ILocalizationService localizationService) : this(settingsService, appSettingsService, categoryInfoProvider, new OpenAIClientFactory(), localizationService)
        {
        }


        internal PageCategorizationService(ISettingsService settingsService, IAppSettingsService appSettingsService, ICategoryInfoProvider categoryInfoProvider, IOpenAIClientFactory openAIClientFactory, ILocalizationService localizationService)
        {
            this.settingsService = settingsService;
            this.appSettingsService = appSettingsService;
            this.categoryInfoProvider = categoryInfoProvider;
            this.openAIClientFactory = openAIClientFactory;
            this.localizationService = localizationService;
        }


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

            var localizedNamesByDisplayNames = new Dictionary<string, string>();
            var systemPrompt = GetSystemPrompt(categoryIdentifiers, localizedNamesByDisplayNames);

            var client = CreateClient();
            string treeNodeData = ExtractTreeNodeData(treeNode);

            if (treeNodeData == string.Empty)
            {
                return new PageCategorizationResult();
            }

            var chatCompletionsOptions = InitializeChatCompletionsOptions(treeNodeData, systemPrompt);
            var response = client.GetChatCompletions(chatCompletionsOptions);

            return ProcessResponse(response, categoryIdentifiers, localizedNamesByDisplayNames);
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

            if (!fields.Any())
            {
                return string.Empty;
            }

            string textRepresentation = string.Join($"{PageCategorizationConstants.delimiter}\n", fields);

            return $"The data will be in the {treeNode.DocumentCulture} culture. Categorize the following data:\n {textRepresentation}";
        }


        private ChatCompletionsOptions InitializeChatCompletionsOptions(string treeNodeData, string systemPrompt)
        {
            string deploymentName = settingsService[PageCategorizationConstants.DEPLOYMENT_NAME_KEY];

            if (string.IsNullOrEmpty(deploymentName))
            {
                throw new InvalidOperationException($"The Azure OpenAI Content Categorization service deployment name is not configured correctly.");
            }

            float temperature = ValidationHelper.GetFloat(appSettingsService[PageCategorizationConstants.TEMPERATURE_KEY], PageCategorizationConstants.DEFAULT_TEMPERATURE);

            var chatCompletionsOptions = new ChatCompletionsOptions()
            {
                DeploymentName = deploymentName,
                Messages =
                {
                    new ChatRequestSystemMessage(systemPrompt),
                    new ChatRequestUserMessage(treeNodeData),
                },
                MaxTokens = PageCategorizationConstants.MAX_TOKENS,
                Temperature = temperature,
            };

            return chatCompletionsOptions;
        }


        private string GetSystemPrompt(IEnumerable<int> categoryIdentifiers, Dictionary<string, string> localizedDisplayNames)
        {
            return PageCategorizationConstants.DEFAULT_SYSTEM_PROMPT + GetCategoryNames(categoryIdentifiers, localizedDisplayNames);
        }


        private IEnumerable<(string name, string value)> GetFields(TreeNode treeNode)
        {
            string treeNodeClassName = treeNode.ClassName;

            var formInfo = FormHelper.GetFormInfo(treeNodeClassName, false);

            var textColumns = formInfo.GetFields(FieldDataType.LongText).Concat(formInfo.GetFields(FieldDataType.Text));

            var fields = textColumns.Select(text => (text.Caption, treeNode.GetStringValue(text.Name, string.Empty)));

            return fields;
        }


        private PageCategorizationResult ProcessResponse(Response<ChatCompletions> response, IEnumerable<int> categoryIdentifiers, Dictionary<string, string> localizedDisplayNames)
        {
            string responseContent = response.Value.Choices[0].Message.Content;

            var categoriesByName = GetCategories(categoryIdentifiers).ToLookup(category => localizedDisplayNames[category.CategoryDisplayName], category => category.CategoryID);
            var cattegoryNames = responseContent.TrimEnd('.')
                .Split(PageCategorizationConstants.delimiter[0])
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


        private string GetCategoryNames(IEnumerable<int> categoryIdentifiers, Dictionary<string, string> localizedDisplayNames)
        {
            var categories = GetCategories(categoryIdentifiers).Select(category =>
            {
                var localizedName = localizationService.GetString(category.CategoryDisplayName);
                localizedDisplayNames[category.CategoryDisplayName] = localizedName;
                return localizedName;
            });
            return "Category names: " + string.Join(PageCategorizationConstants.delimiter, categories);
        }
    }
}
