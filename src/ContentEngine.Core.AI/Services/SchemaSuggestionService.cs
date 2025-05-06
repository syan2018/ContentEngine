using ContentEngine.Core.DataPipeline.Models;
using ConfigurableAIProvider.Services.Factories;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using System.Text.RegularExpressions;
using System.Text;
using ContentEngine.Core.AI.Utils;

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
            string? samples = null,
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
                    { "description", userPrompt },
                    { "samples", samples ?? string.Empty }
                };

                _logger.LogInformation("Invoking {PluginName}.{FunctionName}...", PluginName, FunctionName);
                var result = await kernel.InvokeAsync(PluginName, FunctionName, arguments, cancellationToken);
                string? suggestionText = result.GetValue<string>()?.Trim();
                suggestionText = AIResponseCleaner.RemoveThinkTags(suggestionText);
                _logger.LogDebug("Raw AI suggestion:\n{SuggestionText}", suggestionText);

                if (string.IsNullOrWhiteSpace(suggestionText))
                    throw new InvalidOperationException("AI did not provide a suggestion.");

                var parsedFields = ParseMarkdownTableToFields(suggestionText);
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
        /// 解析 AI 输出的 markdown 表格为 FieldDefinition 列表。
        /// </summary>
        private List<FieldDefinition>? ParseMarkdownTableToFields(string markdown)
        {
            // 匹配markdown表格的行
            var lines = markdown.Split('\n').Select(l => l.Trim()).Where(l => l.StartsWith("|")).ToList();
            if (lines.Count < 2) return null; // 至少有表头和一行分隔

            // 跳过表头和分隔线
            var dataLines = lines.Skip(2);
            var fields = new List<FieldDefinition>();
            foreach (var line in dataLines)
            {
                // | FieldName | Type | Required | Comment |
                var cells = line.Split('|').Select(c => c.Trim()).ToArray();
                if (cells.Length < 5) continue;
                var name = cells[1];
                var typeStr = cells[2];
                var requiredStr = cells[3];
                var comment = cells[4];
                FieldType fieldType;
                if (!Enum.TryParse<FieldType>(typeStr, true, out fieldType))
                {
                    _logger.LogWarning("Could not parse FieldType '{TypeString}' from table line: {Line}", typeStr, line);
                    continue;
                }
                var fieldDef = new FieldDefinition
                {
                    Name = name,
                    Type = fieldType,
                    IsRequired = requiredStr.Contains("必填") || requiredStr.Equals("Required", StringComparison.OrdinalIgnoreCase),
                    Comment = comment
                };
                // Reference 类型的目标Schema名可通过备注或后续增强
                fields.Add(fieldDef);
            }
            return fields.Count > 0 ? fields : null;
        }
    }
}
