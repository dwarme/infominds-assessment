using System.Text.Json;

namespace Backend.Features.Chat;

public class ChatToolExecutor(ChatDataTools dataTools, ChatDocumentTools documentTools)
{
    public IReadOnlyList<OpenAiToolDefinition> Definitions => ChatToolDefinitions.All;

    public async Task<string> ExecuteAsync(string toolName, JsonElement arguments, CancellationToken cancellationToken)
    {
        var result = toolName switch
        {
            ChatToolNames.ListCustomerCategories =>
                await dataTools.ListCustomerCategoriesAsync(cancellationToken),
            ChatToolNames.CountCustomersByCategory =>
                await dataTools.CountCustomersByCategoryAsync(
                    RequireString(arguments, "categoryDescription", "category"),
                    cancellationToken),
            ChatToolNames.ListCustomersByCategory =>
                await dataTools.ListCustomersByCategoryAsync(
                    RequireString(arguments, "categoryDescription", "category"),
                    cancellationToken),
            ChatToolNames.SearchCustomers =>
                await dataTools.SearchCustomersAsync(
                    GetOptionalString(arguments, "name"),
                    GetOptionalString(arguments, "email"),
                    GetOptionalString(arguments, "phone"),
                    GetOptionalString(arguments, "iban"),
                    cancellationToken),
            ChatToolNames.GetCustomerByName =>
                await dataTools.GetCustomerByNameAsync(
                    RequireString(arguments, "name"),
                    cancellationToken),
            ChatToolNames.SearchSuppliersByEmailDomain =>
                await dataTools.SearchSuppliersByEmailDomainAsync(
                    RequireString(arguments, "domain"),
                    cancellationToken),
            ChatToolNames.SearchSuppliers =>
                await dataTools.SearchSuppliersAsync(
                    GetOptionalString(arguments, "name"),
                    GetOptionalString(arguments, "email"),
                    GetOptionalString(arguments, "phone"),
                    cancellationToken),
            ChatToolNames.ListDocumentsForCustomer =>
                await documentTools.ListDocumentsForCustomerAsync(
                    RequireString(arguments, "customerName"),
                    cancellationToken),
            ChatToolNames.ListDocumentsForSupplier =>
                await documentTools.ListDocumentsForSupplierAsync(
                    RequireString(arguments, "supplierName"),
                    cancellationToken),
            ChatToolNames.SearchDocumentChunks =>
                await documentTools.SearchDocumentChunksAsync(
                    RequireString(arguments, "query"),
                    GetOptionalString(arguments, "customerName"),
                    GetOptionalString(arguments, "supplierName"),
                    GetOptionalInt(arguments, "documentId"),
                    cancellationToken),
            _ => throw new BadHttpRequestException($"Unknown chat tool '{toolName}'."),
        };

        return JsonSerializer.Serialize(result);
    }

    private static string RequireString(JsonElement arguments, string propertyName, params string[] alternatePropertyNames)
    {
        foreach (var candidate in new[] { propertyName }.Concat(alternatePropertyNames))
        {
            if (!arguments.TryGetProperty(candidate, out var property) || property.ValueKind != JsonValueKind.String)
                continue;

            var value = property.GetString();
            if (!string.IsNullOrWhiteSpace(value))
                return value.Trim();
        }

        throw new BadHttpRequestException($"Tool argument '{propertyName}' is required.");
    }

    private static string? GetOptionalString(JsonElement arguments, string propertyName)
    {
        if (!arguments.TryGetProperty(propertyName, out var property) || property.ValueKind != JsonValueKind.String)
            return null;

        var value = property.GetString();
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static int? GetOptionalInt(JsonElement arguments, string propertyName)
    {
        if (!arguments.TryGetProperty(propertyName, out var property))
            return null;

        if (property.ValueKind == JsonValueKind.Number && property.TryGetInt32(out var number))
            return number;

        if (property.ValueKind == JsonValueKind.String &&
            int.TryParse(property.GetString(), out var parsed))
            return parsed;

        return null;
    }
}
