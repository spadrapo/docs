# Drapo function 
A Drapo function is a function used within the Drapo framework. Each Drapo function is organized in its own folder under the `wwwroot/app/functions` directory in the repository.
Its composition is: 
- description.html: File that describes the function.
- parameters.json: File that contains the parameters of the function.
- samples: Folder that contains sample files for the function. Inside are folders for each sample, which contain a description.html file and a content.html file. Each sample has a unique name that is a numeric increase.

# MCP Server
This repository includes an MCP (Multi-Channel Protocol) server that exposes endpoints to search for Drapo functions and retrieve their details. The main service for this is `FunctionService` in the `WebDocs.Services` namespace. It provides methods to:
- List all Drapo functions
- Get details for a specific function (description, parameters, samples)

# Developer Guidance
- To add a new Drapo function, create a new folder under `wwwroot/app/functions` and add the required files as described above.
- To extend MCP endpoints, update or add services/controllers in the `WebDocs` project.
- For best Copilot suggestions, keep this file and your code documentation up to date.
