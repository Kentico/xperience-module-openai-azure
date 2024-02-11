using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

using Azure;
using Azure.AI.OpenAI;

using CMS;
using CMS.Core;
using CMS.DataEngine;
using CMS.DocumentEngine;
using CMS.FormEngine;

using Kentico.Xperience.OpenAI.Azure;

[assembly: RegisterImplementation(typeof(ITextClassificationService), typeof(TextClassificationService))]

namespace Kentico.Xperience.OpenAI.Azure
{
    /// <summary>
    /// Implementation of the <see cref="ITextClassificationService"/>.
    /// </summary>
    internal class TextClassificationService : ITextClassificationService
    {
        private const string DEPLOYMENT_NAME_KEY = "KenticoXperienceAzureOpenAIDeploymentName";
        private const string API_ENPOINT_KEY = "KenticoXperienceAzureOpenAIAPIEndpoint";
        private const string API_KEY_KEY = "KenticoXperienceAzureOpenAIAPIKey";
        private const string CLASSIFICATION_ENABLED_KEY = "KenticoXperienceAzureOpenAIEnableContentCategorization";

        private const string DEFAULT_SYSTEM_PROMPT = "Classify the following page data, by the provided categories. Even when the categories do not fit in general, you have to pick at least 1. Respond only with the category names, separated by \";\", do not mention anything else than category names.";

        private const int MAX_TOKENS = 400;

        private readonly ISettingsService settingsService;


        /// <summary>
        /// Creates new instance of <see cref="TextClassificationService"/>.
        /// </summary>
        /// <param name="settingsService"></param>
        public TextClassificationService(ISettingsService settingsService)
        {
            this.settingsService = settingsService;
        }


        /// <inheritdoc/>
        public string SystemPrompt { get; set; } = DEFAULT_SYSTEM_PROMPT;


        /// <inheritdoc/>
        public void ResetSystemPrompt()
        {
            SystemPrompt = DEFAULT_SYSTEM_PROMPT;
        }


        /// <inheritdoc/>
        public bool IsClassificationEnabled => bool.Parse(settingsService[CLASSIFICATION_ENABLED_KEY]);


        /// <inheritdoc/>
        public IEnumerable<string> ClassifyPage(TreeNode treeNode, IEnumerable<string> categories, CancellationToken cancellationToken)
        {
            if (treeNode == null)
            {
                throw new ArgumentNullException(nameof(treeNode));
            }
            if (categories == null)
            {
                throw new ArgumentNullException(nameof(categories));
            }
            if (!IsClassificationEnabled)
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
            var response = client.GetChatCompletions(chatCompletionsOptions, cancellationToken);

            return new List<string>() { response.Value.Choices[0].Message.Content };
        }


        private OpenAIClient CreateClient()
        {
            var apiEndpoint = settingsService[API_ENPOINT_KEY];
            var apiKey = settingsService[API_KEY_KEY];

            if (string.IsNullOrEmpty(apiEndpoint) || string.IsNullOrEmpty(apiKey))
            {
                throw new InvalidOperationException($"The Azure OpenAI Content Categorization service API endpoint and key are not configured correctly.");
            }

            return new OpenAIClient(new Uri(apiEndpoint), new AzureKeyCredential(apiKey));
        }


        private string ExtractTreeNodeData(TreeNode treeNode)
        {
            var fields = GetFields(treeNode);
            var textRepresentation = fields.Aggregate("", (acc, curr) => $"{acc} \"{curr.name}\":\"{curr.value}\";");

            return "Page data: " + textRepresentation;
        }


        private ChatCompletionsOptions InitializeChatCompletionsOptions(string treeNodeData, IEnumerable<string> categories)
        {
            var deploymentName = settingsService[DEPLOYMENT_NAME_KEY];

            if (string.IsNullOrEmpty(deploymentName))
            {
                throw new InvalidOperationException($"The Azure OpenAI Content Categorization service deployment name is not configured correctly");
            }

            var chatCompletionsOptions = new ChatCompletionsOptions()
            {
                DeploymentName = deploymentName,
                Messages =
                {
                    new ChatRequestSystemMessage(SystemPrompt),
                    new ChatRequestSystemMessage(GetCategories(categories)),
                    new ChatRequestUserMessage(treeNodeData),
                },
                MaxTokens = MAX_TOKENS
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


        private string GetCategories(IEnumerable<string> categories) => "The categories are: " + string.Join(",", categories);
    }
}
