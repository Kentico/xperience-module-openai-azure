using System.Collections.Generic;
using System.Threading;
using CMS.DocumentEngine;

namespace Kentico.Xperience.OpenAI.Azure
{
    public interface ITextClassificationService
    {
        IEnumerable<string> ClassifyPage(TreeNode treeNode, IEnumerable<string> categories, CancellationToken cancellationToken);

        string SystemPrompt { get; set; }

        void ResetSystemPrompt();

        bool IsClassificationEnabled { get; }
    }


}
