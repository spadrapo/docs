# Drapo Documentation

[![Build Status](https://dev.azure.com/spadrapo/docs/_apis/build/status/docs?branchName=master)](https://dev.azure.com/spadrapo/docs/_build/latest?definitionId=1&branchName=master)

This repository contains the official documentation website for **Drapo**, a powerful declarative frontend framework for building single-page applications using .NET.

## About Drapo

Drapo is a frontend framework that brings reactive programming to .NET web applications without the complexity of traditional JavaScript frameworks. It uses HTML attributes to create dynamic, data-driven user interfaces with seamless server-side integration.

### Key Features

- **Declarative Data Binding**: Two-way data binding using simple HTML attributes like `d-model`
- **Template System**: Dynamic content rendering with `d-for` loops and `d-if` conditions
- **AJAX Integration**: Built-in data loading and posting with `d-dataUrlGet` and `d-dataUrlSet`
- **Real-time Communication**: Live data updates using `d-dataPipes` for real-time features
- **Event Handling**: Rich event system with `d-on-*` attributes for user interactions
- **Form Validation**: Integrated validation system with custom rules and error handling
- **Component Architecture**: Reusable components and sectoring for modular applications
- **Storage Management**: Client-side data storage with automatic change tracking

### Why Choose Drapo?

- **No JavaScript Required**: Build complex SPAs using only HTML attributes and .NET
- **Familiar Development Model**: Leverage existing .NET skills and tooling
- **Rapid Development**: Declarative syntax reduces boilerplate code
- **Type Safety**: Full IntelliSense support with strongly-typed models
- **Performance**: Optimized for fast rendering and minimal DOM manipulation

## Documentation Website

This repository hosts a comprehensive documentation website built with ASP.NET Core and the Drapo framework itself. The documentation includes:

- **Installation Guide**: Step-by-step setup instructions
- **Framework Introduction**: Core concepts and getting started
- **Data Binding**: Working with data, models, and storage
- **Attributes Reference**: Complete guide to all Drapo attributes
- **Expressions**: Template expressions and data manipulation
- **Validation**: Form validation and error handling
- **Components**: Building reusable UI components
- **Functions Library**: 60 built-in Drapo functions with examples
- **Sample Applications**: Real-world examples including a Todo app
- **Debugging Tools**: Development and troubleshooting guide

## Development Setup

### Prerequisites

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Node.js](https://nodejs.org/) (for building Less CSS files)
- [Docker](https://www.docker.com/) (optional, for containerized development)

### Local Development

1. **Clone the repository**
   ```bash
   git clone https://github.com/spadrapo/docs.git
   cd docs
   ```

2. **Navigate to the project directory**
   ```bash
   cd src/WebDocs
   ```

3. **Install dependencies**
   ```bash
   # Install .NET dependencies
   dotnet restore
   
   # Install Node.js dependencies for Less compilation
   npm install
   ```

4. **Build CSS from Less files** (optional)
   ```bash
   # Using Cake build script
   dotnet tool install -g Cake.Tool
   dotnet cake build.cake
   ```

5. **Run the application**
   ```bash
   dotnet run
   ```

6. **Access the documentation**
   - Open your browser to `https://localhost:5001` or `http://localhost:5000`
   - The documentation will be available with full navigation and examples

### Docker Development

Alternatively, you can run the documentation site using Docker:

```bash
# Build the Docker image
docker build -t drapo-docs -f src/Dockerfile src/

# Run the container
docker run -p 8080:80 drapo-docs
```

Access the documentation at `http://localhost:8080`.

## Project Structure

```
├── src/
│   ├── WebDocs/                    # Main ASP.NET Core application
│   │   ├── Controllers/            # API controllers for documentation data
│   │   ├── Models/                 # ViewModels for documentation content
│   │   ├── Services/               # Services for function and attribute documentation
│   │   ├── wwwroot/                # Static web content
│   │   │   ├── app/
│   │   │   │   ├── functions/      # Documentation for 60 Drapo functions
│   │   │   │   ├── menu/           # Main documentation sections
│   │   │   │   └── shared/         # Shared UI components
│   │   │   ├── css/                # Compiled CSS files
│   │   │   └── img/                # Images and assets
│   │   ├── styles/                 # Less CSS source files
│   │   └── WebDocs.csproj          # Project file
│   ├── Dockerfile                  # Docker configuration
│   └── docs.sln                    # Solution file
├── azure-pipelines.yml             # Azure DevOps CI/CD pipeline
├── LICENSE                         # MIT License
└── README.md                       # This file
```

## Key Components

### Drapo Functions Documentation

The `/app/functions/` directory contains comprehensive documentation for 60 built-in Drapo functions, each with:

- **Description**: What the function does
- **Parameters**: Input parameters and types  
- **Samples**: Working code examples
- **Usage**: Real-world scenarios

### Menu-Based Documentation

The `/app/menu/` directory organizes documentation into logical sections:

- **Installation**: Getting started with Drapo
- **Data**: Working with data binding and storage
- **Attributes**: Complete reference for all Drapo attributes
- **Expressions**: Template expressions and data manipulation
- **Validation**: Form validation techniques
- **Components**: Building reusable components
- **Applications**: Sample applications and tutorials

### MCP Integration

This documentation site includes Model Context Protocol (MCP) integration, providing AI-powered assistance for:

- Function discovery and documentation
- Code examples and samples
- Interactive development support

## Building and Deployment

### Development Build

```bash
cd src/WebDocs
dotnet build
```

### Production Build

```bash
cd src/WebDocs
dotnet publish -c Release -o publish
```

### CSS Compilation

The project uses Less for CSS preprocessing:

```bash
cd src/WebDocs
dotnet cake build.cake --target=less
```

## Contributing

We welcome contributions to improve the Drapo documentation! Here's how you can help:

### Documentation Improvements

1. **Fork the repository**
2. **Create a feature branch**: `git checkout -b feature/improve-docs`
3. **Make your changes**: Edit HTML files in `/src/WebDocs/wwwroot/app/menu/`
4. **Test locally**: Run the application and verify your changes
5. **Submit a pull request**: Include a clear description of your improvements

### Adding Function Documentation

To document a new Drapo function:

1. **Create function directory**: `/src/WebDocs/wwwroot/app/functions/FunctionName/`
2. **Add description**: Create `description.html` with function overview
3. **Define parameters**: Create `parameters.json` with parameter specifications
4. **Add samples**: Create numbered sample directories with `description.html` and `content.html`

### Reporting Issues

If you find errors or have suggestions:

1. **Check existing issues**: Search for similar reports
2. **Create detailed issue**: Include steps to reproduce, expected behavior, and screenshots
3. **Label appropriately**: Use tags like `documentation`, `bug`, or `enhancement`

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Links

- **Live Documentation**: [Drapo Documentation Site](https://docs.drapo.io) 
- **Drapo Framework**: [Main Drapo Repository](https://github.com/spadrapo/drapo)
- **NuGet Package**: [Drapo on NuGet](https://www.nuget.org/packages/Drapo/)
- **Issues & Support**: [GitHub Issues](https://github.com/spadrapo/docs/issues)

## Contact

For questions about Drapo or this documentation:

- **GitHub Issues**: [Report bugs or request features](https://github.com/spadrapo/docs/issues)
- **Discussions**: [Community discussions and support](https://github.com/spadrapo/docs/discussions)

---

**Build modern web applications with the power and familiarity of .NET using Drapo!**