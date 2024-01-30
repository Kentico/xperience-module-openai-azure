using CMS;
using CMS.DataEngine;

using Kentico.Xperience.OpenAI.Azure;

[assembly: RegisterModule(typeof(AzureOpenAIModule))]

namespace Kentico.Xperience.OpenAI.Azure
{
    /// <summary>
    /// Represents the Azure OpenAI module for the Xperience admin interface.
    /// </summary>
    public class AzureOpenAIModule : Module
    {
        public const string NAME = "Kentico.Xperience.OpenAI.Azure";


        /// <summary>
        /// Initializes a new instance of the <see cref="AzureOpenAIModule"/> class.
        /// </summary>
        public AzureOpenAIModule() : base(NAME)
        {
        }
    }
}
