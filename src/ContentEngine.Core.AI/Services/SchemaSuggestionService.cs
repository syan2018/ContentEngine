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
        private const string SuggestFunction = "SuggestFields";
        private const string RefineFunction = "RefineFields";

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

                _logger.LogInformation("Invoking {PluginName}.{FunctionName}...", PluginName, SuggestFunction);
                var result = await kernel.InvokeAsync(PluginName, SuggestFunction, arguments, cancellationToken);
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

        public async Task<List<FieldDefinition>?> RefineSchemaAsync(
            List<FieldDefinition> currentFields,
            string originalDescription,
            string? userFeedback = null,
            CancellationToken cancellationToken = default)
        {
            Kernel? kernel = null;
            try
            {
                _logger.LogInformation("Building Kernel for Agent '{AgentName}'...", AgentName);
                kernel = await _kernelFactory.BuildKernelAsync(AgentName);
                if (kernel == null)
                    throw new InvalidOperationException($"Failed to create the AI Kernel for agent '{AgentName}'.");

                if (!kernel.Plugins.Contains(PluginName))
                    _logger.LogWarning("Plugin '{PluginName}' not found in the kernel.", PluginName);

                // 将当前字段列表转为markdown表格
                var fieldsMarkdown = BuildFieldsMarkdownTable(currentFields);
                var arguments = new KernelArguments
                {
                    { "originalDescription", originalDescription },
                    { "fields", fieldsMarkdown },
                    { "feedback", userFeedback ?? string.Empty }
                };

                _logger.LogInformation("Invoking {PluginName}.{FunctionName}...", PluginName, RefineFunction);
                var result = await kernel.InvokeAsync(PluginName, RefineFunction, arguments, cancellationToken);
                string? refinedText = result.GetValue<string>()?.Trim();
                refinedText = AIResponseCleaner.RemoveThinkTags(refinedText);
                _logger.LogDebug("Raw AI refined fields:\n{RefinedText}", refinedText);

                if (string.IsNullOrWhiteSpace(refinedText))
                    throw new InvalidOperationException("AI did not provide a refined field list.");

                var parsedFields = ParseMarkdownTableToFields(refinedText);
                if (parsedFields == null || parsedFields.Count == 0)
                    throw new FormatException("Could not parse the AI refined fields into list. Check AI output format or parser logic.");

                _logger.LogInformation("Schema refined successfully. Found {FieldCount} fields.", parsedFields.Count);
                return parsedFields;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during AI schema refinement process.");
                throw;
            }
        }

        /// <summary>
        /// 解析 AI 输出的 markdown 表格为 FieldDefinition 列表。
        /// </summary>
        private List<FieldDefinition>? ParseMarkdownTableToFields(string markdown)
        {
            var lines = markdown.Split('\n').Select(l => l.Trim()).Where(l => l.StartsWith("|")).ToList();
            if (lines.Count < 2) return null;
            var dataLines = lines.Skip(2);
            var fields = new List<FieldDefinition>();
            foreach (var line in dataLines)
            {
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
                fields.Add(fieldDef);
            }
            return fields.Count > 0 ? fields : null;
        }

        /// <summary>
        /// 将字段列表转为markdown表格字符串。
        /// </summary>
        private string BuildFieldsMarkdownTable(List<FieldDefinition> fields)
        {
            var sb = new StringBuilder();
            sb.AppendLine("| FieldName | Type | Required | Comment |");
            sb.AppendLine("|-----------|------|----------|---------|");
            foreach (var f in fields)
            {
                sb.AppendLine($"| {f.Name} | {f.Type} | {(f.IsRequired ? "Required" : "Optional")} | {f.Comment ?? ""} |");
            }
            return sb.ToString();
        }
    }
}
