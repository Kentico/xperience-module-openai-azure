# Kentico Xperience Azure OpenAI integration module


## Description

---Please put here some general information about your Intergration / App / Solution.---

## Requirements and prerequisites
* *Kentico Xperience 13.* installed.
* URL and credentials to your Azure OpenAI portal.

## Installation
1. Open the solution with your administration project (*~/WebApp.sln*).
1. Navigate to the *NuGet Package Manager Console*.
1. Run *Install-Package Kentico.Xperience.OpenAI.Azure.KX13 -Version 1.0.*
1. Build the *CMSApp* project.
1. Run the Xperience administration to finish the module installation.

  
## Development environment setup
1. Download/clone this repository
1. Copy `/src/CMS/ConnectionStrings.template.config` file to `/src/CMS/ConnectionStrings.config`
1. Add directory junction of _src/Kentico.Xperience.OpenAI.Azure/CMSResources/Kentico.Xperience.OpenAI.Azure_ into _src/CMS/CMSResources_ using Command Prompt (not Powershell)\
`mklink /J .\src\CMS\CMSResources\Kentico.Xperience.OpenAI.Azure .\src\Kentico.Xperience.OpenAI.Azure\CMSResources\Kentico.Xperience.OpenAI.Azure`
1. Open `/src/WebApp.sln`
1. Start the *CMSApp* project in IIS Express
1. Optionally: If you receive an exception _Could not find a part of the path ... bin\roslyn\csc.exe_ in the new step, open the Package Manager Console (Menu -> View -> Other Windows) and run \
   `Update-Package Microsoft.CodeDom.Providers.DotNetCompilerPlatform -r`
1. Create a database via a web wizard
1. Stop IIS Express process
1. Restore database data via `/src/CMS/bin/ContinuousIntegration.exe -r`
1. Start the *CMSApp* project in IIS Express
1. Go to _Sites_ application
1. Start the DancingGoatCore site
1. Browser: Clear cookies
1. Restart IIS Express process (possibly not necessary)

## Quick Start

---This section shows how to quickly get started with the library. The minimum number of steps (without all the details) should be listed
to give a developer a general idea of what is involved---

## Full Instructions

---Add the full instructions, guidance, and tips to the Usage-Guide.md file---

View the [Usage Guide](./docs/Usage-Guide.md) for more detailed instructions.

## Contributing

To see the guidelines for Contributing to Kentico open source software, please see [Kentico's `CONTRIBUTING.md`](https://github.com/Kentico/.github/blob/main/CONTRIBUTING.md) for more information and follow the [Kentico's `CODE_OF_CONDUCT`](https://github.com/Kentico/.github/blob/main/CODE_OF_CONDUCT.md).

Instructions and technical details for contributing to **this** project can be found in [Contributing Setup](./docs/Contributing-Setup.md).

## License

Distributed under the MIT License. See [`LICENSE.md`](./LICENSE.md) for more information.

## Support

See [`SUPPORT.md`](https://github.com/Kentico/.github/blob/main/SUPPORT.md#full-support) for more information.

For any security issues see [`SECURITY.md`](https://github.com/Kentico/.github/blob/main/SECURITY.md).
