using Microsoft.SemanticKernel;
using Microsoft.Extensions.Configuration;

namespace SemanticKernelSWebChatBot.Services
{
    public class SemanticKernelService
    {
        private readonly Kernel _kernel;
        private readonly IConfiguration _configuration;

        public SemanticKernelService(IConfiguration configuration)
        {
            _configuration = configuration;

            // Read configuration with fallback to environment variables
                                            string? apiKey = _configuration["OpenApi:ApiKey"] ??
                                                            Environment.GetEnvironmentVariable("OpenAI__ApiKey");
            string? endpoint = _configuration["OpenApi:Endpoint"] ??
                               Environment.GetEnvironmentVariable("OpenAI__Endpoint");
            string? deploymentName = _configuration["OpenApi:Model"] ??
                                     Environment.GetEnvironmentVariable("OpenAI__Model");

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                throw new InvalidOperationException("OpenAI API key is not configured. Set 'OpenAI:ApiKey' in appsettings or the environment variable 'OpenAI__ApiKey'.");
            }

            if (string.IsNullOrWhiteSpace(endpoint))
            {
                throw new InvalidOperationException("OpenAI endpoint is not configured. Set 'OpenAI:Endpoint' in appsettings or the environment variable 'OpenAI__Endpoint'.");
            }

            if (string.IsNullOrWhiteSpace(deploymentName))
            {
                throw new InvalidOperationException("OpenAI model/deployment name is not configured. Set 'OpenAI:Model' in appsettings or the environment variable 'OpenAI__Model'.");
            }

            var builder = Kernel.CreateBuilder();
            builder.AddAzureOpenAIChatCompletion(
                deploymentName: deploymentName,
                endpoint: endpoint,
                apiKey: apiKey
            );

            _kernel = builder.Build();
        }

        public async Task<string> ProcessUserInputAsync(string userInput)
        {
            var result = await _kernel.InvokePromptAsync(userInput);
            return result.ToString();
        }
    }
}