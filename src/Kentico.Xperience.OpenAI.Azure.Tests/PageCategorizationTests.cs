using Azure;
using Azure.AI.OpenAI;

using CMS.Core;
using CMS.DocumentEngine;
using CMS.Taxonomy;
using CMS.Tests;

using NSubstitute;

using NUnit.Framework;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Kentico.Xperience.OpenAI.Azure.Tests
{
    [TestFixture]
    public class PageCategorizationTests : UnitTests
    {
        private OpenAIClient client;
        private IOpenAIClientFactory clientFactory;
        private ChatResponseMessage responseMessage;
        private PageCategorizationMock pageCategorizationService;
        private ISettingsService settingService;
        private ICategoryInfoProvider categoryInfoProvider;
        private TreeNode treeNode;

        const string CATEGORY1_NAME = "Category1";
        const string CATEGORY2_NAME = "Category2";
        const string CATEGORY3_NAME = "Category3";
        const int CATEGORY1_ID = 1;
        const int CATEGORY2_ID = 2;
        const int CATEGORY3_ID = 3;
        const string DEPLOYMENT_NAME = "deploymentName";


        [SetUp]
        public void Setup()
        {
            TestContext.Out.WriteLine("setup");
            categoryInfoProvider = null;
            FakeCategories();
            Fake<TreeNode>();

            client = Substitute.For<OpenAIClient>();
            clientFactory = Substitute.For<IOpenAIClientFactory>();
            clientFactory.GetOpenAIClient(Arg.Any<string>(), Arg.Any<string>()).Returns(client);

            settingService = Substitute.For<ISettingsService>();
            settingService[PageCategorizationConstants.DEPLOYMENT_NAME_KEY].Returns(DEPLOYMENT_NAME);
            var localizationService = Substitute.For<ILocalizationService>();
            localizationService.GetString(Arg.Any<string>()).ReturnsForAnyArgs(x => (string)x.Args()[0]);
            var appSettingsService = Substitute.For<IAppSettingsService>();

            treeNode = Substitute.For<TreeNode>();
            treeNode.DocumentCulture = "en-US";

            pageCategorizationService = new PageCategorizationMock(settingService, appSettingsService, categoryInfoProvider, clientFactory, localizationService);
            pageCategorizationService.Fields = new List<(string, string)>() { ("field1", "value1") };
        }


        public void FakeCategories()
        {
            categoryInfoProvider = (ICategoryInfoProvider)Fake<CategoryInfo, CategoryInfoProvider>().WithData(CategoryInfo.New(c1 =>
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
        }


        private void SetChatCompletionsResponse(string expectedResponse)
        {
            var response = Substitute.For<Response<ChatCompletions>>();

            responseMessage = AzureOpenAIModelFactory.ChatResponseMessage(content: expectedResponse);
            var chatChoice = AzureOpenAIModelFactory.ChatChoice(responseMessage);

            var x = AzureOpenAIModelFactory.ChatCompletions(choices: new List<ChatChoice>() { chatChoice });
            response.Value.Returns(x);

            client.GetChatCompletions(Arg.Any<ChatCompletionsOptions>()).Returns(response);
        }


        [Test]
        public void SettingsNotPresent_ThrowsInvalidOperationException()
        {
            TestContext.Out.WriteLine("setting");

            settingService[PageCategorizationConstants.API_ENDPOINT_KEY].Returns("");
            settingService[PageCategorizationConstants.API_KEY_KEY].Returns("");

            var factory = new OpenAIClientFactory();
            clientFactory.When(x => x.GetOpenAIClient(Arg.Any<string>(), Arg.Any<string>())).Do(x => factory.GetOpenAIClient("", ""));

            Assert.Throws<InvalidOperationException>(() => pageCategorizationService.CategorizePage(TreeNode.New(), new List<int> { 1, 2, 3 }));
        }


        [Test]
        public void DeploymentNameNotPresent_ThrowsInvalidOperationException()
        {
            TestContext.Out.WriteLine("deployment");

            settingService[PageCategorizationConstants.DEPLOYMENT_NAME_KEY].Returns("");

            Assert.Throws<InvalidOperationException>(() => pageCategorizationService.CategorizePage(treeNode, new List<int> { 1, 2, 3 }));
        }


        [Test]
        public void DuplicateIdentifiers_ReturnsUniqueIdentifiers()
        {
            TestContext.Out.WriteLine("duplicate");
            var categoryIds = new List<int> { CATEGORY1_ID, CATEGORY2_ID, CATEGORY3_ID, CATEGORY3_ID };
            var uniqueCategoryIds = categoryIds.Distinct();
            var delimiter = PageCategorizationConstants.DELIMITER;

            SetChatCompletionsResponse($"{CATEGORY1_NAME}{delimiter}{CATEGORY2_NAME}{delimiter}{CATEGORY3_NAME}");

            var result = pageCategorizationService.CategorizePage(treeNode, categoryIds);

            Assert.Multiple(() =>
            {
                CollectionAssert.AreEquivalent(uniqueCategoryIds, result.Categories);
                Assert.That(result.Categories.Count(), Is.EqualTo(uniqueCategoryIds.Count()));
                Assert.That(result.UnknownCategories, Is.Empty);
            });
        }


        [Test]
        public void NoTreeNodeData_ReturnsEmptyResult()
        {
            TestContext.Out.WriteLine("e,mpty");

            var categoryIds = new List<int> { CATEGORY1_ID, CATEGORY2_ID, CATEGORY3_ID };

            pageCategorizationService.Fields = new List<(string, string)>();

            var result = pageCategorizationService.CategorizePage(treeNode, categoryIds);

            Assert.Multiple(() =>
            {
                Assert.That(!client.ReceivedCalls().Any());
                Assert.That(result.Categories, Is.Null);
                Assert.That(result.UnknownCategories, Is.Null);
            });
        }


        [Test]
        public void GetChatCompletionsReturnsUnknownCategory_ReturnsUnknownCategory()
        {
            TestContext.Out.WriteLine("completions");

            var categoryIds = new List<int> { CATEGORY1_ID, CATEGORY2_ID, CATEGORY3_ID };
            var responseCategoryIds = new List<int> { CATEGORY1_ID, CATEGORY2_ID };
            var delimiter = PageCategorizationConstants.DELIMITER;

            var unknownCategoryName = "Unknown";

            SetChatCompletionsResponse($"{CATEGORY1_NAME}{delimiter}{CATEGORY2_NAME}{delimiter}{unknownCategoryName}");

            var result = pageCategorizationService.CategorizePage(treeNode, categoryIds);

            Assert.Multiple(() =>
            {
                Assert.That(!result.UnknownCategories.Contains(unknownCategoryName));
                CollectionAssert.AreEquivalent(responseCategoryIds, result.Categories);
            });
        }
    }

    internal class PageCategorizationMock : PageCategorizationService
    {
        public List<(string, string)> Fields { get; set; }


        internal PageCategorizationMock(ISettingsService settingsService, IAppSettingsService appSettingsService, ICategoryInfoProvider categoryInfoProvider, IOpenAIClientFactory openAIClientFactory, ILocalizationService localizationService) : base(settingsService, appSettingsService, categoryInfoProvider, openAIClientFactory, localizationService)
        {
        }


        internal override IEnumerable<(string name, string value)> GetFields(TreeNode treeNode)
        {
            return Fields;
        }
    }
}
