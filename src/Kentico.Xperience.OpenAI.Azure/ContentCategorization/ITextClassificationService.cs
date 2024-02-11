using System.Collections.Generic;
using System.Threading;
using System;

using CMS.DocumentEngine;

namespace Kentico.Xperience.OpenAI.Azure
{
    /// <summary>
    /// Interface for classification of pages.
    /// </summary>
    public interface ITextClassificationService
    {
        /// <summary>
        /// Classifies of the <paramref name="treeNode"/> according to the page data.
        /// </summary>
        /// <param name="treeNode">The page to be categorized.</param>
        /// <param name="categories">Categories from which the content categorization picks.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>List of suitable categories to use.</returns>
        /// <exception cref="ArgumentException">Thrown when <paramref name="categories"/> is empty.</exception>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="treeNode"/> or <paramref name="categories"/> are null.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the API endpoint, key or deployment name are not configured.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the Content Categorization is disabled.</exception>
        /// <remarks>Only text data is used for classification.</remarks>
        IEnumerable<string> ClassifyPage(TreeNode treeNode, IEnumerable<string> categories, CancellationToken cancellationToken);


        /// <summary>
        /// System prompt used for providing initial instructions for the Content Categorization service.
        /// </summary>
        string SystemPrompt { get; set; }


        /// <summary>
        /// Indicates whether the Content Categorization is enabled.
        /// </summary>
        bool IsClassificationEnabled { get; }


        /// <summary>
        /// Resets the system prompt to the default one.
        /// </summary>
        void ResetSystemPrompt();
    }
}
