using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using CMS.Base;
using CMS.Core;
using CMS.DataEngine;
using CMS.Taxonomy;

using Kentico.Components.Web.Mvc.FormComponents;

namespace DancingGoat.Widgets
{
    /// <summary>
    /// Provides data for the category selector.
    /// </summary>
    public class ArticlesCategoryDataProvider : IGeneralSelectorDataProvider
    {
        /// <summary>
        /// Defines the code name of the root category of category subtree. If null, the root category is the global root category.
        /// </summary>
        private const string ROOT_CATEGORY_NAME = "ArticleTopics";
        private const char DELIMITER = '→';
        private const int PAGE_SIZE = 50;


        private readonly ICategoryInfoProvider categoryInfoProvider;
        private readonly ISiteService siteService;
        private readonly ILocalizationService localizationService;
        private CategoryInfo rootCategory;


        private CategoryInfo RootCategory
        {
            get
            {
                if (rootCategory == null)
                {
                    rootCategory = categoryInfoProvider.Get(ROOT_CATEGORY_NAME, siteService.CurrentSite.SiteID);
                }

                return rootCategory;
            }
        }


        public ArticlesCategoryDataProvider(ICategoryInfoProvider categoryInfoProvider, ISiteService siteService, ILocalizationService localizationService)
        {
            this.categoryInfoProvider = categoryInfoProvider;
            this.siteService = siteService;
            this.localizationService = localizationService;
        }


        /// <inheritdoc/>
        public async Task<GeneralSelectorSelectListItems> GetItemsAsync(string searchTerm, int pageIndex, CancellationToken cancellationToken)
        {
            ObjectQuery<CategoryInfo> query = categoryInfoProvider.Get()
                                                .OnSite(siteService.CurrentSite.SiteName, includeGlobal: true)
                                                .WhereEquals(nameof(CategoryInfo.CategoryEnabled), true);

            if (RootCategory != null)
            {
                query = query.WhereNotEquals(nameof(CategoryInfo.CategoryID), RootCategory.CategoryID)
                             .WhereStartsWith(nameof(CategoryInfo.CategoryIDPath), RootCategory.CategoryIDPath); 
            }

            if (!string.IsNullOrEmpty(searchTerm))
            {
                query.WhereContains(nameof(CategoryInfo.CategoryDisplayName), searchTerm);
            }

            query.Page(pageIndex, PAGE_SIZE);

            IEnumerable<CategoryInfo> items = await query.GetEnumerableTypedResultAsync(cancellationToken: cancellationToken);

            return new GeneralSelectorSelectListItems
            {
                Items = items.Select(c => GetSelectedListItem(c, RootCategory?.CategoryNamePath)),
                NextPageAvailable = query.NextPageAvailable
            };
        }


        /// <inheritdoc/>
        public async Task<IEnumerable<GeneralSelectorSelectListItem>> GetSelectedItemsAsync(IEnumerable<GeneralSelectorItem> selectedValues, CancellationToken cancellationToken)
        {
            var identifiers = selectedValues.Select(x => x.Identifier).ToList();

            ObjectQuery<CategoryInfo> query = categoryInfoProvider.Get()
                                                  .OnSite(siteService.CurrentSite.SiteName, includeGlobal: true)
                                                  .Columns(nameof(CategoryInfo.CategoryName), nameof(CategoryInfo.CategoryNamePath))
                                                  .WhereIn(nameof(CategoryInfo.CategoryName), identifiers);

            return (await query.GetEnumerableTypedResultAsync(cancellationToken: cancellationToken))
                                   .Select(item => GetSelectedListItem(item, RootCategory?.CategoryNamePath));
        }


        /// <summary>
        /// Transforms a single CategoryInfo object into a GeneralSelectorSelectListItem object
        /// </summary>
        private GeneralSelectorSelectListItem GetSelectedListItem(CategoryInfo category, string rootNamePath)
        {
            return new GeneralSelectorSelectListItem
            {
                Text = GetDisplayText(category, rootNamePath),
                Value = new GeneralSelectorItem { Identifier = category.CategoryName }
            };
        }


        /// <summary>
        /// Gets the display text for the category
        /// </summary>
        private string GetDisplayText(CategoryInfo category, string rootNamePath)
        {
            var categoryNames = category.CategoryNamePath.Substring((rootNamePath?.Length ?? 0) + 1)
                                            .Split('/')
                                            .Select(name => localizationService.LocalizeString(name));

            return string.Join($" {DELIMITER} ", categoryNames);
        }
    }
}
