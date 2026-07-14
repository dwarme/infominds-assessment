namespace Backend.Features.Chat;

public class ChatDataTools(BackendContext context)
{
    public const int MaxResults = 20;

    public async Task<object> ListCustomerCategoriesAsync(CancellationToken cancellationToken)
    {
        var categories = await context.CustomerCategories
            .OrderBy(category => category.Description)
            .Select(category => new
            {
                category.Id,
                category.Code,
                category.Description,
            })
            .ToListAsync(cancellationToken);

        return new
        {
            count = categories.Count,
            categories,
        };
    }

    public async Task<object> CountCustomersByCategoryAsync(string categoryDescription, CancellationToken cancellationToken)
    {
        var (categoryDescriptionResult, matchingCategories, categoryIds) =
            await ResolveCategoriesAsync(categoryDescription, cancellationToken);

        if (matchingCategories.Count == 0)
        {
            return new
            {
                categoryDescription = categoryDescriptionResult,
                matchingCategories,
                count = 0,
            };
        }

        var count = await CountCustomersInCategoriesAsync(categoryIds, cancellationToken);

        return new
        {
            categoryDescription = categoryDescriptionResult,
            matchingCategories,
            count,
        };
    }

    public async Task<object> ListCustomersByCategoryAsync(string categoryDescription, CancellationToken cancellationToken)
    {
        var (categoryDescriptionResult, matchingCategories, categoryIds) =
            await ResolveCategoriesAsync(categoryDescription, cancellationToken);

        if (matchingCategories.Count == 0)
        {
            return new
            {
                categoryDescription = categoryDescriptionResult,
                matchingCategories,
                totalCount = 0,
                returnedCount = 0,
                customers = Array.Empty<object>(),
            };
        }

        var query = context.Customers
            .Where(customer => customer.CustomerCategoryId != null && categoryIds.Contains(customer.CustomerCategoryId.Value));

        var totalCount = await query.CountAsync(cancellationToken);
        var customers = await query
            .OrderBy(customer => customer.Name)
            .Take(MaxResults)
            .Select(customer => new
            {
                customer.Id,
                customer.Name,
                customer.Address,
                customer.Email,
                customer.Phone,
                customer.Iban,
                Category = customer.CustomerCategory != null ? customer.CustomerCategory.Description : null,
            })
            .ToListAsync(cancellationToken);

        return new
        {
            categoryDescription = categoryDescriptionResult,
            matchingCategories,
            totalCount,
            returnedCount = customers.Count,
            truncated = totalCount > customers.Count,
            customers,
        };
    }

    public async Task<object> SearchCustomersAsync(
        string? name,
        string? email,
        string? phone,
        string? iban,
        CancellationToken cancellationToken)
    {
        EnsureSearchFilter(name, "name");
        EnsureSearchFilter(email, "email");
        EnsureSearchFilter(phone, "phone");
        EnsureSearchFilter(iban, "iban");

        var query = context.Customers.AsQueryable();

        if (!string.IsNullOrWhiteSpace(name))
        {
            var formatted = name.Trim().ToLower();
            query = query.Where(customer => customer.Name.ToLower().Contains(formatted));
        }

        if (!string.IsNullOrWhiteSpace(email))
        {
            var formatted = email.Trim().ToLower();
            query = query.Where(customer => customer.Email.ToLower().Contains(formatted));
        }

        if (!string.IsNullOrWhiteSpace(phone))
        {
            var formatted = phone.Trim().ToLower();
            query = query.Where(customer => customer.Phone.ToLower().Contains(formatted));
        }

        if (!string.IsNullOrWhiteSpace(iban))
        {
            var formatted = iban.Trim().ToLower();
            query = query.Where(customer => customer.Iban.ToLower().Contains(formatted));
        }

        if (string.IsNullOrWhiteSpace(name) &&
            string.IsNullOrWhiteSpace(email) &&
            string.IsNullOrWhiteSpace(phone) &&
            string.IsNullOrWhiteSpace(iban))
        {
            throw new BadHttpRequestException("At least one customer search filter is required.");
        }

        return await BuildCustomerSearchResultAsync(query, cancellationToken);
    }

    public async Task<object> GetCustomerByNameAsync(string name, CancellationToken cancellationToken)
    {
        SearchQueryLimits.EnsureWithinLimit(name, "name");
        var formatted = name.Trim().ToLower();

        var query = context.Customers.Where(customer => customer.Name.ToLower().Contains(formatted));
        var result = await BuildCustomerSearchResultAsync(query, cancellationToken, name);

        return result;
    }

    public async Task<object> SearchSuppliersByEmailDomainAsync(string domain, CancellationToken cancellationToken)
    {
        SearchQueryLimits.EnsureWithinLimit(domain, "domain");
        var formatted = domain.Trim().ToLower();

        var query = context.Suppliers.Where(supplier => supplier.Email.ToLower().Contains(formatted));
        return await BuildSupplierSearchResultAsync(query, cancellationToken, domain);
    }

    public async Task<object> SearchSuppliersAsync(
        string? name,
        string? email,
        string? phone,
        CancellationToken cancellationToken)
    {
        EnsureSearchFilter(name, "name");
        EnsureSearchFilter(email, "email");
        EnsureSearchFilter(phone, "phone");

        var query = context.Suppliers.AsQueryable();

        if (!string.IsNullOrWhiteSpace(name))
        {
            var formatted = name.Trim().ToLower();
            query = query.Where(supplier => supplier.Name.ToLower().Contains(formatted));
        }

        if (!string.IsNullOrWhiteSpace(email))
        {
            var formatted = email.Trim().ToLower();
            query = query.Where(supplier => supplier.Email.ToLower().Contains(formatted));
        }

        if (!string.IsNullOrWhiteSpace(phone))
        {
            var formatted = phone.Trim().ToLower();
            query = query.Where(supplier => supplier.Phone.ToLower().Contains(formatted));
        }

        if (string.IsNullOrWhiteSpace(name) && string.IsNullOrWhiteSpace(email) && string.IsNullOrWhiteSpace(phone))
            throw new BadHttpRequestException("At least one supplier search filter is required.");

        return await BuildSupplierSearchResultAsync(query, cancellationToken);
    }

    private async Task<(string CategoryDescription, List<object> MatchingCategories, List<int> CategoryIds)> ResolveCategoriesAsync(
        string categoryDescription,
        CancellationToken cancellationToken)
    {
        SearchQueryLimits.EnsureWithinLimit(categoryDescription, "categoryDescription");
        var formatted = categoryDescription.Trim().ToLower();

        var matchingCategories = await context.CustomerCategories
            .Where(category => category.Description.ToLower().Contains(formatted))
            .Select(category => new { category.Id, category.Description })
            .ToListAsync(cancellationToken);

        return (
            categoryDescription.Trim(),
            matchingCategories.Cast<object>().ToList(),
            matchingCategories.Select(category => category.Id).ToList());
    }

    private async Task<int> CountCustomersInCategoriesAsync(List<int> categoryIds, CancellationToken cancellationToken) =>
        await context.Customers
            .Where(customer => customer.CustomerCategoryId != null && categoryIds.Contains(customer.CustomerCategoryId.Value))
            .CountAsync(cancellationToken);

    private async Task<object> BuildCustomerSearchResultAsync(
        IQueryable<Customer> query,
        CancellationToken cancellationToken,
        string? searchLabel = null)
    {
        var totalCount = await query.CountAsync(cancellationToken);
        var customers = await query
            .OrderBy(customer => customer.Name)
            .Take(MaxResults)
            .Select(customer => new
            {
                customer.Id,
                customer.Name,
                customer.Address,
                customer.Email,
                customer.Phone,
                customer.Iban,
                Category = customer.CustomerCategory != null ? customer.CustomerCategory.Description : null,
            })
            .ToListAsync(cancellationToken);

        return new
        {
            searchLabel,
            totalCount,
            returnedCount = customers.Count,
            truncated = totalCount > customers.Count,
            customers,
        };
    }

    private async Task<object> BuildSupplierSearchResultAsync(
        IQueryable<Supplier> query,
        CancellationToken cancellationToken,
        string? searchLabel = null)
    {
        var totalCount = await query.CountAsync(cancellationToken);
        var suppliers = await query
            .OrderBy(supplier => supplier.Name)
            .Take(MaxResults)
            .Select(supplier => new
            {
                supplier.Id,
                supplier.Name,
                supplier.Email,
                supplier.Phone,
            })
            .ToListAsync(cancellationToken);

        return new
        {
            searchLabel,
            totalCount,
            returnedCount = suppliers.Count,
            truncated = totalCount > suppliers.Count,
            suppliers,
        };
    }

    private static void EnsureSearchFilter(string? value, string parameterName)
    {
        if (!string.IsNullOrWhiteSpace(value))
            SearchQueryLimits.EnsureWithinLimit(value, parameterName);
    }
}
