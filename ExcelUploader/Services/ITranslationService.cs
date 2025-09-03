using System.Collections.Generic;

namespace ExcelUploader.Services
{
    public interface ITranslationService
    {
        /// <summary>
        /// Translates Excel column names to SQL-friendly column names using multiple strategies
        /// </summary>
        /// <param name="excelColumnName">Original Excel column name</param>
        /// <param name="strategy">Translation strategy to use</param>
        /// <returns>Translated SQL column name</returns>
        string TranslateColumnName(string excelColumnName, TranslationStrategy strategy = TranslationStrategy.Intelligent);

        /// <summary>
        /// Translates Excel table names to SQL-friendly table names
        /// </summary>
        /// <param name="excelTableName">Original Excel table name</param>
        /// <param name="strategy">Translation strategy to use</param>
        /// <returns>Translated SQL table name</returns>
        string TranslateTableName(string excelTableName, TranslationStrategy strategy = TranslationStrategy.Intelligent);

        /// <summary>
        /// Gets all available translation strategies
        /// </summary>
        /// <returns>List of available strategies</returns>
        List<TranslationStrategy> GetAvailableStrategies();

        /// <summary>
        /// Gets a preview of how a column name would be translated using different strategies
        /// </summary>
        /// <param name="excelColumnName">Original Excel column name</param>
        /// <returns>Dictionary of strategy -> translated name</returns>
        Dictionary<TranslationStrategy, string> GetTranslationPreview(string excelColumnName);

        /// <summary>
        /// Validates if a translated name is valid for SQL Server
        /// </summary>
        /// <param name="translatedName">The translated name to validate</param>
        /// <returns>Validation result with details</returns>
        TranslationValidationResult ValidateTranslatedName(string translatedName);
    }

    public enum TranslationStrategy
    {
        /// <summary>
        /// Simple character replacement (current approach)
        /// </summary>
        Simple,
        
        /// <summary>
        /// Intelligent translation with common business terms
        /// </summary>
        Intelligent,
        
        /// <summary>
        /// English translation of Turkish terms
        /// </summary>
        EnglishTranslation,
        
        /// <summary>
        /// Abbreviated version for shorter names
        /// </summary>
        Abbreviated,
        
        /// <summary>
        /// Technical naming convention
        /// </summary>
        Technical,
        
        /// <summary>
        /// Preserve original with minimal changes
        /// </summary>
        Preserve
    }

    public class TranslationValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
        public List<string> Warnings { get; set; } = new List<string>();
        public string SuggestedName { get; set; } = string.Empty;
    }
}
