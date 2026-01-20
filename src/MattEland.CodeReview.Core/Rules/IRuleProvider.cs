using MattEland.CodeReview.Core.Models;

namespace MattEland.CodeReview.Core.Rules;

/// <summary>
/// Provides access to code review rules.
/// </summary>
public interface IRuleProvider
{
    /// <summary>
    /// Gets a rule by its unique identifier.
    /// </summary>
    /// <param name="ruleId">The rule ID (e.g., "sql/performance/select-star").</param>
    /// <returns>The rule, or null if not found.</returns>
    Rule? GetRule(string ruleId);

    /// <summary>
    /// Gets all rules for a specific language.
    /// </summary>
    /// <param name="language">The language (e.g., "csharp", "sql").</param>
    /// <returns>All rules for the specified language.</returns>
    IEnumerable<Rule> GetRulesByLanguage(string language);

    /// <summary>
    /// Gets all rules for a specific language and category.
    /// </summary>
    /// <param name="language">The language (e.g., "csharp", "sql").</param>
    /// <param name="category">The category (e.g., "performance", "security").</param>
    /// <returns>All rules matching the criteria.</returns>
    IEnumerable<Rule> GetRulesByCategory(string language, string category);

    /// <summary>
    /// Gets all enabled rules.
    /// </summary>
    /// <returns>All rules that are currently enabled.</returns>
    IEnumerable<Rule> GetEnabledRules();

    /// <summary>
    /// Gets all available rules.
    /// </summary>
    /// <returns>All registered rules.</returns>
    IEnumerable<Rule> GetAllRules();

    /// <summary>
    /// Gets all available languages.
    /// </summary>
    /// <returns>The list of supported languages.</returns>
    IEnumerable<string> GetLanguages();

    /// <summary>
    /// Gets all categories for a language.
    /// </summary>
    /// <param name="language">The language.</param>
    /// <returns>The categories available for that language.</returns>
    IEnumerable<string> GetCategories(string language);
}
