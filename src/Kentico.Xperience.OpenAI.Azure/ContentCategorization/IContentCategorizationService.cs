using System.Collections.Generic;
using System.Threading;
using System;

using CMS.DocumentEngine;

namespace Kentico.Xperience.OpenAI.Azure
{
    /// <summary>
    /// Interface for categorization of content of pages.
    /// </summary>
    public interface IContentCategorizationService
    {
        /// <summary>
        /// Categorizes the <paramref name="treeNode"/> according to the page data.
        /// </summary>
        /// <param name="treeNode">The page to be classified.</param>
        /// <param name="categories">Categories from which the content categorization picks.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>List of suitable categories to use.</returns>
        /// <exception cref="ArgumentException">Thrown when <paramref name="categories"/> is empty.</exception>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="treeNode"/> or <paramref name="categories"/> are null.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the API endpoint, key or deployment name are not configured.</exception>
        /// <exception cref="InvalidOperationException">Thrown when Content Categorization is disabled.</exception>
        /// <remarks>Only text data is used for classification.</remarks>
        IEnumerable<string> CategorizePage(TreeNode treeNode, IEnumerable<string> categories, CancellationToken cancellationToken);


        /// <summary>
        /// Gets the system prompt used for providing initial instructions for the Content Categorization service.
        /// </summary>
        string GetSystemPrompt();


        /// <summary>
        /// Indicates whether Content Categorization is enabled.
        /// </summary>
        bool IsCategorizationEnabled { get; }
    }
}
