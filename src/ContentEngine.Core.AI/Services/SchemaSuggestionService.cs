using ContentEngine.Core.DataPipeline.Models;
using ConfigurableAIProvider.Services.Factories;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using System.Text.RegularExpressions;
using System.Text;

namespace ContentEngine.Core.AI.Services
{
    /// <summary>
    /// 实现通过 AI 分析自然语言描述并建议 Schema 字段的服务。
    /// </summary>
    public class SchemaSuggestionService : ISchemaSuggestionService
    {
        private readonly IAIKernelFactory _kernelFactory;
        private readonly ILogger<SchemaSuggestionService> _logger;
        private const string AgentName = "ContentEngineHelper";
        private const string PluginName = "SchemaHelper";
        private const string FunctionName = "SuggestFields";

        public SchemaSuggestionService(IAIKernelFactory kernelFactory, ILogger<SchemaSuggestionService> logger)
        {
            _kernelFactory = kernelFactory;
            _logger = logger;
        }

        public async Task<SchemaDefinition?> SuggestSchemaAsync(
            string userPrompt,
            string schemaName,
            string schemaDescription,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(userPrompt) || string.IsNullOrWhiteSpace(schemaName))
                throw new ArgumentException("User prompt and schema name must not be empty.");

            Kernel? kernel = null;
            try
            {
                _logger.LogInformation("Building Kernel for Agent '{AgentName}'...", AgentName);
                kernel = await _kernelFactory.BuildKernelAsync(AgentName);
                if (kernel == null)
                    throw new InvalidOperationException($"Failed to create the AI Kernel for agent '{AgentName}'.");

                if (!kernel.Plugins.Contains(PluginName))
                    _logger.LogWarning("Plugin '{PluginName}' not found in the kernel.", PluginName);

                var arguments = new KernelArguments
                {
                    { "description", userPrompt }
                };

                _logger.LogInformation("Invoking {PluginName}.{FunctionName}...", PluginName, FunctionName);
                var result = await kernel.InvokeAsync(PluginName, FunctionName, arguments, cancellationToken);
                string? suggestionText = result.GetValue<string>()?.Trim();
                _logger.LogDebug("Raw AI suggestion:\n{SuggestionText}", suggestionText);

                if (string.IsNullOrWhiteSpace(suggestionText))
                    throw new InvalidOperationException("AI did not provide a suggestion.");

                var parsedFields = ParseSuggestionToFields(suggestionText);
                if (parsedFields == null || parsedFields.Count == 0)
                    throw new FormatException("Could not parse the AI suggestion into fields. Check AI output format or parser logic.");

                var schema = new SchemaDefinition
                {
                    Name = schemaName,
                    Description = schemaDescription,
                    Fields = parsedFields
                };
                _logger.LogInformation("Schema suggestion parsed successfully. Found {FieldCount} fields.", schema.Fields.Count);
                return schema;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during AI schema suggestion process.");
                throw;
            }
        }

        /// <summary>
        /// 解析 AI 输出文本为 FieldDefinition 列表。
        /// </summary>
        private List<FieldDefinition>? ParseSuggestionToFields(string suggestionText)
        {
            var fields = new List<FieldDefinition>();
            // Regex to capture: FieldName (Type[:TargetSchema], Required/Optional)
            var regex = new Regex(@"^- \s*([\w\s]+)\s*\(\s*(\w+)(?::([\w\s]+))?\s*,\s*(\w+)\s*\)", RegexOptions.IgnoreCase | RegexOptions.Multiline);
            var matches = regex.Matches(suggestionText);
            foreach (Match match in matches)
            {
                if (match.Groups.Count >= 5)
                {
                    string name = match.Groups[1].Value.Trim();
                    string typeStr = match.Groups[2].Value.Trim();
                    string? referenceTarget = match.Groups[3].Success ? match.Groups[3].Value.Trim() : null;
                    string requiredStr = match.Groups[4].Value.Trim();

                    if (Enum.TryParse<FieldType>(typeStr, true, out var fieldType))
                    {
                        var fieldDef = new FieldDefinition
                        {
                            Name = name,
                            Type = fieldType,
                            IsRequired = requiredStr.Equals("Required", StringComparison.OrdinalIgnoreCase),
                            ReferenceSchemaName = fieldType == FieldType.Reference ? referenceTarget : null
                        };
                        if (!string.IsNullOrWhiteSpace(fieldDef.Name))
                        {
                            fields.Add(fieldDef);
                        }
                        else
                        {
                            _logger.LogWarning("Parsed field has empty name from line: {Line}", match.Value);
                        }
                    }
                    else
                    {
                        _logger.LogWarning("Could not parse FieldType '{TypeString}' from line: {Line}", typeStr, match.Value);
                    }
                }
                else
                {
                    _logger.LogWarning("Regex did not match expected groups for line: {Line}", match.Value);
                }
            }
            return fields.Count > 0 ? fields : null;
        }
    }
} 