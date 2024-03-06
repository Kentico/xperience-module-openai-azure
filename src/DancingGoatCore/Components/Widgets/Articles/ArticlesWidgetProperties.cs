using System.Collections.Generic;
using System.Linq;

using Kentico.Components.Web.Mvc.FormComponents;
using Kentico.Forms.Web.Mvc;
using Kentico.PageBuilder.Web.Mvc;

namespace DancingGoat.Widgets
{
    /// <summary>
    /// Properties for Articles widget.
    /// </summary>
    public class ArticlesWidgetProperties : IWidgetProperties
    {
        /// <summary>
        /// Number of articles to show.
        /// </summary>
        public int Count { get; set; } = 5;


        /// <summary>
        /// Allows the user to select categories.
        /// </summary>
        [EditingComponent(GeneralSelector.IDENTIFIER, Order = 1, Label = "Categories")]
        [EditingComponentProperty(nameof(GeneralSelectorProperties.DataProviderType), typeof(ArticlesCategoryDataProvider))]
        [EditingComponentProperty(nameof(GeneralSelectorProperties.MaxItemsLimit), 0)]
        public IEnumerable<GeneralSelectorItem> Categories { get; set; } = Enumerable.Empty<GeneralSelectorItem>();
    }
}