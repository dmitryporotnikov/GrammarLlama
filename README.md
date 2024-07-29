# GrammarLlama

GrammarLlama is a .NET 8 desktop application that provides real-time grammar and spelling corrections using AI language models. It serves as a custom frontend for Ollama, leveraging the power of large language models to enhance writing quality.

## How it works?

GrammarLlama sends user input to the Ollama API, which processes the text using the Llama 3.1 model (or other compatible models). A custom system message tailors the AI's responses specifically for grammar and spelling corrections. The application then presents the suggestions and corrections through its intuitive desktop interface.

## Features

* Integrates with Ollama API for AI-powered corrections
* Uses Llama 3.1 models by default
* Custom system message for specialized grammar correction
* Near real-time feedback and suggestions (depends on ollama performance on user device)
* User-friendly .NET 8 desktop interface

## Technical details
- **Platform:** .NET 8 Desktop
- **Backend:** Ollama API
- **Default Model:** Llama 3.1
- **API Calls:**  Implementation for grammar-focused corrections
- **UI Framework:** WPF (Windows Presentation Foundation)

### Getting started

- [.NET 8](https://dotnet.microsoft.com/download)
- Ensure you have Ollama with llama3.1 model installed and running on your system.
- Download and run application from the releases section, **OR**:

- Clone this repository.
- Open the solution in Visual Studio 2022 or later.
- Build and run the application
  

### Configuration

You can customize the Ollama API endpoint and model selection in the application settings. By default, GrammarLlama uses the Llama 3.1 model, but you can experiment with other models compatible with Ollama.


## Contributing

Contributions are welcome! Please open an issue or submit a pull request.


## Contact

For any questions or suggestions, please contact [Dmitry Porotnikov](https://github.com/dmitryporotnikov).
