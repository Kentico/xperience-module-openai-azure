# Xperience Azure OpenAI integration

This custom module allows Kentico Xperience 13 users to [automatically select](https://docs.kentico.com/x/IgqRBg) the best fitting categories for a page based on its content using [Azure OpenAI](https://azure.microsoft.com/en-us/products/ai-services/openai-service).


## Installation

1. Install the [Kentico.Xperience.OpenAI.Azure](https://www.nuget.org/packages/Kentico.Xperience.OpenAI.Azure) NuGet package in the administration project.
2. Sign in to your [Azure portal](https://portal.azure.com/).
3. Create and configure an [Azure OpenAI resource](https://learn.microsoft.com/en-us/azure/ai-services/openai/how-to/create-resource?pivots=web-portal).
4. [Deploy a model](https://learn.microsoft.com/en-us/azure/ai-services/openai/how-to/create-resource?pivots=web-portal#deploy-a-model).
5. In Xperience, go to **Settings -> Content -> Azure OpenAI** and configure the settings:
    - General:
        - **Azure OpenAI API endpoint:** – value of the **Endpoint** field from **Resource Management -> Keys and Endpoints** of your Azure OpenAI resource in Azure portal.
        - **Azure OpenAI API key:** – value of either the **KEY 1** or **KEY 2** fields (both keys will work) from **Resource Management -> Keys and Endpoints** of your Azure OpenAI resource in Azure portal.
    - Content categorization:
        - Select the **Enable content categorization** option.
        - **Deployment name** – the **Deployment name** you chose when deploying the model. Can be also found in **Azure OpenAI Studio -> Management -> Deployments**.

    ![Azure OpenAI settings](images/azure_openai_settings.png)

## Automatic selection of categories

After you [set up](#installation) the integration, the next time you assign a page into categories you can simply click the **Auto-Select** button and the best fitting categories based on the page's content get automatically selected.

**Important notes:**
- The automatic selection disregards all preexisting category assignments. Consequently, using the **Auto-Select** on manually categorized pages may suggest a different set of categories for the page. You can always add any desired categories manually on top of the automatic selection.
- The page you are assigning into categories must have at least some data stored in fields with **Text** or **LongText** data types. That is, the automatic selection doesn't work for pages that are built entirely via Page builder.

![Auto-select categories](images/auto_select.png)

## Contributing

To see the guidelines for Contributing to Kentico open source software, please see [Kentico's `CONTRIBUTING.md`](https://github.com/Kentico/.github/blob/main/CONTRIBUTING.md) for more information and follow the [Kentico's `CODE_OF_CONDUCT`](https://github.com/Kentico/.github/blob/main/CODE_OF_CONDUCT.md).

## Development environment setup

1. Download/clone this repository.
2. Copy the `/src/CMS/ConnectionStrings.template.config` file to `/src/CMS/ConnectionStrings.config`.
3. Add a directory junction of *src/Kentico.Xperience.OpenAI.Azure/CMSResources/Kentico.Xperience.OpenAI.Azure* into *src/CMS/CMSResources* using Command Prompt **(not PowerShell)**:

    `mklink /J .\src\CMS\CMSResources\Kentico.Xperience.OpenAI.Azure .\src\Kentico.Xperience.OpenAI.Azure\CMSResources\Kentico.Xperience.OpenAI.Azure`
4. Open `/src/WebApp.sln`.
5. Start the *CMSApp* project in IIS Express.
    - If you encounter a *Could not find a part of the path ... bin\roslyn\csc.exe* exception, open the Package Manager Console (Menu -> View -> Other Windows) and run:

        `Update-Package Microsoft.CodeDom.Providers.DotNetCompilerPlatform -r`
6. Create a database via a web wizard.
7. Stop the IIS Express process.
8. Restore database data via `/src/CMS/bin/ContinuousIntegration.exe -r`.
9. Start the *CMSApp* project in IIS Express.
10. Go to the *Sites* application.
11. Start the DancingGoatCore site.
12. Clear cookies in your browser.
13. Optional – restart the IIS Express process.

## License

Distributed under the MIT License. See [`LICENSE.md`](./LICENSE.md) for more information.

## Support

See [`SUPPORT.md`](https://github.com/Kentico/.github/blob/main/SUPPORT.md#full-support) for more information.

For any security issues see [`SECURITY.md`](https://github.com/Kentico/.github/blob/main/SECURITY.md).
