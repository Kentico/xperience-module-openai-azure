using System;
using System.ClientModel;
using System.ClientModel.Primitives;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using CMS.Core;
using CMS.DocumentEngine;
using CMS.Taxonomy;
using CMS.Tests;

using OpenAI;
using OpenAI.Chat;

using NSubstitute;

using NUnit.Framework;

namespace Kentico.Xperience.OpenAI.Azure.Tests
{
    [TestFixture]
    public class PageCategorizationTests : UnitTests
    {
        private OpenAIClient client;
        private ChatClient chatClient;
        private IOpenAIClientFactory clientFactory;
        private PageCategorizationMock pageCategorizationService;
        private ISettingsService settingService;
        private ICategoryInfoProvider categoryInfoProvider;
        private TreeNode treeNode;

        private const string CATEGORY1_NAME = "Category1";
        private const string CATEGORY2_NAME = "Category2";
        private const string CATEGORY3_NAME = "Category3";
        private const int CATEGORY1_ID = 1;
        private const int CATEGORY2_ID = 2;
        private const int CATEGORY3_ID = 3;
        private const string DEPLOYMENT_NAME = "deploymentName";
        private const string DELIMITER = PageCategorizationConstants.DELIMITER;


        [SetUp]
        public void Setup()
        {
            FakeCategories();
            Fake<TreeNode>();

            client = Substitute.For<OpenAIClient>();

            clientFactory = Substitute.For<IOpenAIClientFactory>();
            clientFactory.GetOpenAIClient(Arg.Any<string>(), Arg.Any<string>()).Returns(client);

            chatClient = Substitute.For<ChatClient>();
            client.GetChatClient(DEPLOYMENT_NAME).Returns(chatClient);

            settingService = Substitute.For<ISettingsService>();
            settingService[PageCategorizationConstants.DEPLOYMENT_NAME_KEY].Returns(DEPLOYMENT_NAME);

            var localizationService = Substitute.For<ILocalizationService>();
            localizationService.LocalizeString(Arg.Any<string>()).ReturnsForAnyArgs(x => x.ArgAt<string>(0));

            var appSettingsService = Substitute.For<IAppSettingsService>();

            treeNode = Substitute.For<TreeNode>();
            treeNode.DocumentCulture = "en-US";
            treeNode.ClassName.Returns("documentName");

            pageCategorizationService = new PageCategorizationMock(settingService, appSettingsService, categoryInfoProvider, clientFactory, localizationService)
            {
                Fields = new List<(string, string)>() { ("field1", "value1") }
            };
        }


        [TestCase("", "apiKey", TestName = "API endpoint key is empty")]
        [TestCase("apiEndpointKey", "", TestName = "API key is empty")]
        public void CategorizePage_SettingsNotPresent_ThrowsInvalidOperationException(string apiEndpointKey, string apiKey)
        {
            settingService[PageCategorizationConstants.API_ENDPOINT_KEY].Returns(apiEndpointKey);
            settingService[PageCategorizationConstants.API_KEY_KEY].Returns(apiKey);

            var factory = new OpenAIClientFactory();
            clientFactory.When(x => x.GetOpenAIClient(Arg.Any<string>(), Arg.Any<string>())).Do(x => factory.GetOpenAIClient(x.ArgAt<string>(0), x.ArgAt<string>(1)));

            Assert.Throws<InvalidOperationException>(() => pageCategorizationService.CategorizePage(treeNode, new List<int> { 1, 2, 3 }));
        }


        [Test]
        public void CategorizePage_DeploymentNameNotPresent_ThrowsInvalidOperationException()
        {
            settingService[PageCategorizationConstants.DEPLOYMENT_NAME_KEY].Returns("");

            Assert.Throws<InvalidOperationException>(() => pageCategorizationService.CategorizePage(treeNode, new List<int> { 1, 2, 3 }));
        }


        [Test]
        public void CategorizePage_DuplicateIdentifiers_ReturnsUniqueIdentifiers()
        {
            var categoryIds = new List<int> { CATEGORY1_ID, CATEGORY2_ID, CATEGORY3_ID, CATEGORY3_ID };
            var uniqueCategoryIds = categoryIds.Distinct();

            SetChatCompletionsResponse($"{CATEGORY1_NAME}{DELIMITER}{CATEGORY2_NAME}{DELIMITER}{CATEGORY3_NAME}");

            var result = pageCategorizationService.CategorizePage(treeNode, categoryIds);

            Assert.Multiple(() =>
            {
                Assert.That(result.Categories, Is.EquivalentTo(uniqueCategoryIds));
                Assert.That(result.UnknownCategories, Is.Empty);
            });
        }


        [Test]
        public void CategorizePage_NoTreeNodeData_ReturnsEmptyResult()
        {
            var categoryIds = new List<int> { CATEGORY1_ID, CATEGORY2_ID, CATEGORY3_ID };

            pageCategorizationService.Fields = new List<(string, string)>();

            var result = pageCategorizationService.CategorizePage(treeNode, categoryIds);

            Assert.Multiple(() =>
            {
                Assert.That(client.ReceivedCalls(), Is.Empty);
                Assert.That(result.Categories, Is.Empty);
                Assert.That(result.UnknownCategories, Is.Empty);
            });
        }


        [Test]
        public void CategorizePage_VariousCategories_ReturnsCorrectResponse()
        {
            const string UNKNOWN_CATEGORY_NAME = "Unknown";
            const string EXPECTED_LANGUAGE = "English (United States)";
            const string EXPECTED_DATA_FORMAT = "field1:value1";

            var categoryIds = new List<int> { CATEGORY1_ID, CATEGORY2_ID, CATEGORY3_ID };
            var responseCategoryIds = new List<int> { CATEGORY1_ID, CATEGORY2_ID };

            SetChatCompletionsResponse($"{CATEGORY1_NAME}{DELIMITER}{CATEGORY2_NAME}{DELIMITER}{UNKNOWN_CATEGORY_NAME}");

            var result = pageCategorizationService.CategorizePage(treeNode, categoryIds);

            Assert.Multiple(() =>
            {
                _ = client.Received(1);
                chatClient.CompleteChat(Arg.Is<ChatMessage[]>(m =>
                        m.Length == 2
                        && m[0].Content.Equals(PageCategorizationConstants.DEFAULT_SYSTEM_PROMPT + $"Category names: {CATEGORY1_NAME}{DELIMITER}{CATEGORY2_NAME}{DELIMITER}{CATEGORY3_NAME}")
                        && m[1].Content.Equals($"The data will be in the {EXPECTED_LANGUAGE} language. Categorize the following data:\n {EXPECTED_DATA_FORMAT}")),
                    Arg.Is<ChatCompletionOptions>(o =>
                        o.MaxOutputTokenCount == PageCategorizationConstants.MAX_TOKENS
                        && o.Temperature == PageCategorizationConstants.DEFAULT_TEMPERATURE
                ));
                _ = settingService.Received(1)[PageCategorizationConstants.API_ENDPOINT_KEY];
                _ = settingService.Received(1)[PageCategorizationConstants.API_KEY_KEY];
                _ = settingService.Received(1)[PageCategorizationConstants.DEPLOYMENT_NAME_KEY];
                Assert.That(result.Categories, Is.EquivalentTo(responseCategoryIds));
                Assert.That(result.UnknownCategories, Is.EquivalentTo(new[] { UNKNOWN_CATEGORY_NAME }));
            });
        }


        private void FakeCategories() => categoryInfoProvider = (ICategoryInfoProvider)Fake<CategoryInfo, CategoryInfoProvider>()
                .WithData(CategoryInfo.New(c1 =>
                {
                    c1.CategoryDisplayName = CATEGORY1_NAME;
                    c1.CategoryID = CATEGORY1_ID;
                }),
                    CategoryInfo.New(c2 =>
                    {
                        c2.CategoryDisplayName = CATEGORY2_NAME;
                        c2.CategoryID = CATEGORY2_ID;
                    }),
                    CategoryInfo.New(c3 =>
                    {
                        c3.CategoryDisplayName = CATEGORY3_NAME;
                        c3.CategoryID = CATEGORY3_ID;
                    })).ProviderObject;


        private void SetChatCompletionsResponse(string expectedResponse)
        {
            chatClient = client.GetChatClient(DEPLOYMENT_NAME);

            var completion = OpenAIChatModelFactory.ChatCompletion(content: new ChatMessageContent(expectedResponse));
            var result = ClientResult.FromValue(completion, new PipelineResponseMock());

            chatClient.CompleteChat(Arg.Any<ChatMessage[]>(), Arg.Any<ChatCompletionOptions>()).Returns(result);
        }
    }

    internal class PageCategorizationMock : PageCategorizationService
    {
        public List<(string, string)> Fields { get; set; }


        internal PageCategorizationMock(
            ISettingsService settingsService,
            IAppSettingsService appSettingsService,
            ICategoryInfoProvider categoryInfoProvider,
            IOpenAIClientFactory openAIClientFactory,
            ILocalizationService localizationService)
            : base(
                  settingsService,
                  appSettingsService,
                  categoryInfoProvider,
                  openAIClientFactory,
                  localizationService)
        {
        }


        internal override IEnumerable<(string Name, string Value)> GetFields(TreeNode treeNode) => Fields;
    }

    internal class PipelineResponseMock : PipelineResponse
    {
        public override int Status => throw new NotImplementedException();

        public override string ReasonPhrase => throw new NotImplementedException();

        public override Stream ContentStream { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public override System.BinaryData Content => throw new NotImplementedException();

        protected override PipelineResponseHeaders HeadersCore => throw new NotImplementedException();

        public override System.BinaryData BufferContent(CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public override ValueTask<System.BinaryData> BufferContentAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public override void Dispose() => throw new NotImplementedException();
    }
}
