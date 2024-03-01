# Xperience Azure OpenAI integration

This custom module allows Xperience users to [automatically select](https://docs.kentico.com/x/IgqRBg) the best fitting categories for a page based on its content using [Azure OpenAI](https://azure.microsoft.com/en-us/products/ai-services/openai-service).


## Installation

1. Install the TODO: [nuget name](https://) NuGet package in the administration project.
2. Log into your [Azure Portal](https://portal.azure.com/).
3. Create and configure an [Azure OpenAI resource](https://learn.microsoft.com/en-us/azure/ai-services/openai/how-to/create-resource?pivots=web-portal).
4. Within the resource, navigate to **Resource Management -> Keys and Endpoints**.
5. Make a note of the **KEY 1** or **KEY 2** (both keys will work) and the **Endpoint** URL.
6. In Xperience, go to **Settings -> Content -> Azure OpenAI**.
7. Fill out the settings with the values from your Azure OpenAI resource.
8. Enable the feature for your editors by selecting the **Enable content categorization** option.

## Automatic selection of categories

After you [set up](#installation) the integration, next time you will be assigning a page into categories you can simply click the **Auto-Select** button and the best fitting categories based on the page's content will be automatically selected.

![Auto-select categories](images/auto_select.png)

**Important notes:**
- The automatic selection overwrites your previously selected categories.
- The page you are assigning into categories must have at least some data stored in fields with **Text** or **LongText** data types. That is, the automatic selection doesn't work for pages that are built entirely via Page builder.

## Contributing

To see the guidelines for Contributing to Kentico open source software, please see [Kentico's `CONTRIBUTING.md`](https://github.com/Kentico/.github/blob/main/CONTRIBUTING.md) for more information and follow the [Kentico's `CODE_OF_CONDUCT`](https://github.com/Kentico/.github/blob/main/CODE_OF_CONDUCT.md).

Instructions and technical details for contributing to **this** project can be found in [Contributing Setup](./docs/Contributing-Setup.md).

## License

Distributed under the MIT License. See [`LICENSE.md`](./LICENSE.md) for more information.

## Support

See [`SUPPORT.md`](https://github.com/Kentico/.github/blob/main/SUPPORT.md#full-support) for more information.

For any security issues see [`SECURITY.md`](https://github.com/Kentico/.github/blob/main/SECURITY.md).
