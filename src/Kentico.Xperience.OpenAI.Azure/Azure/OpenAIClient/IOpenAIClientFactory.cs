using Azure.AI.OpenAI;

namespace Kentico.Xperience.OpenAI.Azure
{
    internal interface IOpenAIClientFactory
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="apiKey"></param>
        /// <param name="apiEndpoint"></param>
        /// <returns></returns>
        /// <exception cref=""/>
        OpenAIClient GetOpenAIClient(string apiKey, string apiEndpoint);
    }
}
