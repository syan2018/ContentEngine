using System.Globalization;
using ContentEngine.Core.DataPipeline.Models;
using LiteDB;

namespace ContentEngine.Core.Utils;

/// <summary>
/// Utility methods for converting between BsonDocuments and form data representations (like Dictionaries),
/// and for formatting BsonValues for display.
/// </summary>
public static class BsonFormUtils
{
    /// <summary>
    /// Converts a BsonDocument to a Dictionary suitable for form binding, based on a SchemaDefinition.
    /// Populates the dictionary with values from the document or type-appropriate defaults.
    /// </summary>
    public static Dictionary<string, object?> ConvertBsonToFormData(BsonDocument doc, SchemaDefinition schema)
    {
        ArgumentNullException.ThrowIfNull(doc);
        ArgumentNullException.ThrowIfNull(schema);

        var dict = new Dictionary<string, object?>();

        foreach (var field in schema.Fields)
        {
            object? value = null;
            if (doc.TryGetValue(field.Name, out var bsonValue) && !bsonValue.IsNull)
            {
                // Convert BsonValue to appropriate .NET type for the form
                value = bsonValue.Type switch
                {
                    BsonType.DateTime => bsonValue.AsDateTime,
                    BsonType.Boolean => bsonValue.AsBoolean,
                    BsonType.Decimal => (double?)bsonValue.AsDecimal, // Use double? for form
                    BsonType.Double => bsonValue.AsDouble,
                    BsonType.Int32 => (double?)bsonValue.AsInt32,     // Use double? for form
                    BsonType.Int64 => (double?)bsonValue.AsInt64,     // Use double? for form
                    BsonType.ObjectId => bsonValue.AsObjectId.ToString(), // Store reference ID as string
                    _ => bsonValue.AsString // Default others to string
                };
            }
            else // Handle null or missing values - populate with defaults
            {
                value = field.Type switch
                {
                    FieldType.Number => (double?)null,
                    FieldType.Boolean => false, // Default bool to false for checkbox
                    FieldType.Date => (DateTime?)null,
                    FieldType.Text => string.Empty, // Explicitly default Text to empty string
                    FieldType.Reference => null, // Default reference string to null
                    _ => null
                };
            }
            dict[field.Name] = value; // Add boxed value
        }
        return dict;
    }

    /// <summary>
    /// Converts a form data Dictionary back into a BsonDocument based on a SchemaDefinition.
    /// </summary>
    public static BsonDocument ConvertFormDataToBson(Dictionary<string, object?> formData, SchemaDefinition schema)
    {
        ArgumentNullException.ThrowIfNull(formData);
        ArgumentNullException.ThrowIfNull(schema);

        var doc = new BsonDocument();

        foreach (var field in schema.Fields)
        {
            BsonValue bsonValue = BsonValue.Null;
            if (formData.TryGetValue(field.Name, out var objectValue) && objectValue != null)
            {
                try
                {
                    // Convert .NET type from dictionary back to BsonValue
                    switch (field.Type)
                    {
                        case FieldType.Text:
                            if (objectValue is string s) bsonValue = new BsonValue(s);
                            break;
                        case FieldType.Number:
                            if (objectValue is double d) bsonValue = new BsonValue(d);
                            // Add handling if int/long might appear, though unlikely with current form setup
                            else if (objectValue is int i) bsonValue = new BsonValue(i);
                            else if (objectValue is long l) bsonValue = new BsonValue(l);
                            break;
                        case FieldType.Boolean:
                            if (objectValue is bool b) bsonValue = new BsonValue(b);
                            break;
                        case FieldType.Date:
                            if (objectValue is DateTime dt) bsonValue = new BsonValue(dt);
                            break;
                        case FieldType.Reference:
                            if (objectValue is string refString && !string.IsNullOrEmpty(refString))
                            {
                                try { bsonValue = new BsonValue(new ObjectId(refString)); }
                                catch (FormatException) { throw new FormatException($"Invalid ObjectId format for field '{field.Name}': '{refString}'. Expected a 24-digit hex string."); }
                            }
                            break;
                    }
                }
                catch (Exception ex) // Catch unexpected conversion errors
                {
                    if (ex is FormatException) throw; // Re-throw format errors
                    throw new InvalidOperationException($"Error processing field '{field.Name}' with value '{objectValue}'. {ex.Message}", ex);
                }
            }

            // Only add non-null BsonValue to the document
            if (!bsonValue.IsNull) { doc[field.Name] = bsonValue; }
        }
        return doc;
    }

    /// <summary>
    /// Gets a user-friendly display string for a BsonValue within a BsonDocument, given a field name.
    /// </summary>
    public static string GetDisplayValue(BsonDocument doc, string fieldName)
    {
        ArgumentNullException.ThrowIfNull(doc);

        if (doc.TryGetValue(fieldName, out var value))
        {
            if (value.IsDateTime) return value.AsDateTime.ToString("g", CultureInfo.CurrentCulture); // Use culture-aware formatting
            if (value.IsObjectId) return value.AsObjectId.ToString();
            if (value.IsNull) return "(null)";
            // Display numbers without unnecessary decimals if they are round integers
            if (value.IsDouble && value.AsDouble == Math.Floor(value.AsDouble)) return value.AsDouble.ToString("F0");
            if (value.IsDecimal && value.AsDecimal == Math.Floor(value.AsDecimal)) return value.AsDecimal.ToString("F0");
            return value.RawValue?.ToString() ?? "(error)";
        }
        return "(not found)";
    }

    /// <summary>
    /// Gets a stable string key for a BsonDocument entry, typically its _id field.
    /// Provides a fallback if _id is missing (though unlikely for LiteDB).
    /// </summary>
    public static string GetEntryKey(BsonDocument entry)
    {
         ArgumentNullException.ThrowIfNull(entry);
         // LiteDB _id is BsonValue, directly call ToString()
         return entry.TryGetValue("_id", out var idValue) ? idValue.ToString() : Guid.NewGuid().ToString(); // Fallback, should log warning if used
    }
} 