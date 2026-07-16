namespace Backend.Features.Chat;

public static class ChatToolNames
{
    public const string ListCustomerCategories = "list_customer_categories";
    public const string CountCustomersByCategory = "count_customers_by_category";
    public const string ListCustomersByCategory = "list_customers_by_category";
    public const string SearchCustomers = "search_customers";
    public const string GetCustomerByName = "get_customer_by_name";
    public const string SearchSuppliersByEmailDomain = "search_suppliers_by_email_domain";
    public const string SearchSuppliers = "search_suppliers";
    public const string ListDocumentsForCustomer = "list_documents_for_customer";
    public const string ListDocumentsForSupplier = "list_documents_for_supplier";
    public const string SearchDocumentChunks = "search_document_chunks";
}

public static class ChatToolDefinitions
{
    public static IReadOnlyList<OpenAiToolDefinition> All { get; } =
    [
        Tool(
            ChatToolNames.ListCustomerCategories,
            "List all customer categories with id, code, and description.",
            new { type = "object", properties = new { }, required = Array.Empty<string>() }),
        Tool(
            ChatToolNames.CountCustomersByCategory,
            "Count how many customers belong to a category. Use only when the user asks for a number or quantity.",
            new
            {
                type = "object",
                properties = new
                {
                    category = new { type = "string", description = "Customer category description to match, for example Garden." },
                    categoryDescription = new { type = "string", description = "Alias for category." },
                },
                required = new[] { "category" },
            }),
        Tool(
            ChatToolNames.ListCustomersByCategory,
            "List customers in a category with name, email, phone, IBAN, and category. Use when the user asks to see, show, or list customer details for a category.",
            new
            {
                type = "object",
                properties = new
                {
                    category = new { type = "string", description = "Customer category description to match, for example Garden." },
                    categoryDescription = new { type = "string", description = "Alias for category." },
                },
                required = new[] { "category" },
            }),
        Tool(
            ChatToolNames.SearchCustomers,
            "Search customers by name, email, phone, or IBAN. Use for existence checks and open-ended lookups. At least one filter is required.",
            new
            {
                type = "object",
                properties = new
                {
                    name = new { type = "string", description = "Partial customer name match." },
                    email = new { type = "string", description = "Partial customer email match." },
                    phone = new { type = "string", description = "Partial customer phone match." },
                    iban = new { type = "string", description = "Partial customer IBAN match." },
                },
            }),
        Tool(
            ChatToolNames.GetCustomerByName,
            "Find a specific customer by company name and return full contact and banking details. Prefer this for questions about a named customer.",
            new
            {
                type = "object",
                properties = new
                {
                    name = new { type = "string", description = "Customer name to search for." },
                },
                required = new[] { "name" },
            }),
        Tool(
            ChatToolNames.SearchSuppliersByEmailDomain,
            "Find suppliers whose email address contains the given domain.",
            new
            {
                type = "object",
                properties = new
                {
                    domain = new { type = "string", description = "Email domain such as gmail.com." },
                },
                required = new[] { "domain" },
            }),
        Tool(
            ChatToolNames.SearchSuppliers,
            "Search suppliers by name, email, or phone.",
            new
            {
                type = "object",
                properties = new
                {
                    name = new { type = "string", description = "Partial supplier name match." },
                    email = new { type = "string", description = "Partial supplier email match." },
                    phone = new { type = "string", description = "Partial supplier phone match." },
                },
            }),
        Tool(
            ChatToolNames.ListDocumentsForCustomer,
            "List uploaded documents for a customer by name, newest first. Use for 'latest contract' or to discover document ids/titles before searching content.",
            new
            {
                type = "object",
                properties = new
                {
                    customerName = new { type = "string", description = "Partial customer name match." },
                },
                required = new[] { "customerName" },
            }),
        Tool(
            ChatToolNames.ListDocumentsForSupplier,
            "List uploaded documents for a supplier by name, newest first.",
            new
            {
                type = "object",
                properties = new
                {
                    supplierName = new { type = "string", description = "Partial supplier name match." },
                },
                required = new[] { "supplierName" },
            }),
        Tool(
            ChatToolNames.SearchDocumentChunks,
            "Semantically search document content (contracts, reports, notes). Use when the user asks what a document says, earnings, fees, delivery issues, etc. Optionally scope by customerName, supplierName, or documentId.",
            new
            {
                type = "object",
                properties = new
                {
                    query = new { type = "string", description = "Natural-language search query about document content." },
                    customerName = new { type = "string", description = "Optional: limit to documents of customers matching this name." },
                    supplierName = new { type = "string", description = "Optional: limit to documents of suppliers matching this name." },
                    documentId = new { type = "integer", description = "Optional: limit search to one document id." },
                },
                required = new[] { "query" },
            }),
    ];

    private static OpenAiToolDefinition Tool(string name, string description, object parameters) =>
        new(name, description, parameters);
}

public record OpenAiToolDefinition(string Name, string Description, object Parameters)
{
    public object ToOpenAiTool() => new
    {
        type = "function",
        function = new
        {
            name = Name,
            description = Description,
            parameters = Parameters,
        },
    };
}
