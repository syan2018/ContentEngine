using ContentEngine.Core.DataPipeline.Models;

namespace ContentEngine.Core.AI.Services
{
    /// <summary>
    /// Defines a service for suggesting data schemas based on user input.
    /// </summary>
    public interface ISchemaSuggestionService
    {
        /// <summary>
        /// Suggests a SchemaDefinition based on a natural language description and optional sample data.
        /// </summary>
        /// <param name="userPrompt">The user's natural language description of the data needed.</param>
        /// <param name="schemaName">The desired name for the schema provided by the user.</param>
        /// <param name="schemaDescription">The description for the schema provided by the user.</param>
        /// <param name="samples">Optional: user-provided sample data (text, CSV, or JSON).</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>
        /// A Task representing the asynchronous operation.
        /// The task result contains a suggested SchemaDefinition object, or null if a suggestion
        /// could not be generated reliably. Exceptions may be thrown for configuration or runtime errors.
        /// </returns>
        Task<SchemaDefinition?> SuggestSchemaAsync(
            string userPrompt,
            string schemaName,
            string schemaDescription,
            string? samples = null,
            CancellationToken cancellationToken = default);
    }
} 
