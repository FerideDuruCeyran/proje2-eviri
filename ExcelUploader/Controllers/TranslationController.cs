using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ExcelUploader.Services;

namespace ExcelUploader.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TranslationController : ControllerBase
    {
        private readonly ITranslationService _translationService;
        private readonly ILogger<TranslationController> _logger;

        public TranslationController(ITranslationService translationService, ILogger<TranslationController> logger)
        {
            _translationService = translationService;
            _logger = logger;
        }

        /// <summary>
        /// Get translation preview for a column name using all available strategies
        /// </summary>
        [HttpGet]
        [Route("preview")]
        [AllowAnonymous]
        public IActionResult GetTranslationPreview([FromQuery] string columnName)
        {
            try
            {
                if (string.IsNullOrEmpty(columnName))
                {
                    return BadRequest(new { error = "Column name is required" });
                }

                var preview = _translationService.GetTranslationPreview(columnName);
                var validationResults = new Dictionary<TranslationStrategy, TranslationValidationResult>();

                foreach (var strategy in preview.Keys)
                {
                    validationResults[strategy] = _translationService.ValidateTranslatedName(preview[strategy]);
                }

                return Ok(new
                {
                    originalName = columnName,
                    translations = preview,
                    validations = validationResults,
                    availableStrategies = _translationService.GetAvailableStrategies()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating translation preview for column: {ColumnName}", columnName);
                return StatusCode(500, new { error = "Translation preview generation failed", details = ex.Message });
            }
        }

        /// <summary>
        /// Translate a column name using a specific strategy
        /// </summary>
        [HttpPost]
        [Route("translate-column")]
        [AllowAnonymous]
        public IActionResult TranslateColumnName([FromBody] TranslateColumnRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.ColumnName))
                {
                    return BadRequest(new { error = "Column name is required" });
                }

                var translatedName = _translationService.TranslateColumnName(request.ColumnName, request.Strategy);
                var validation = _translationService.ValidateTranslatedName(translatedName);

                return Ok(new
                {
                    originalName = request.ColumnName,
                    translatedName = translatedName,
                    strategy = request.Strategy,
                    validation = validation
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error translating column name: {ColumnName}", request.ColumnName);
                return StatusCode(500, new { error = "Column translation failed", details = ex.Message });
            }
        }

        /// <summary>
        /// Translate a table name using a specific strategy
        /// </summary>
        [HttpPost]
        [Route("translate-table")]
        [AllowAnonymous]
        public IActionResult TranslateTableName([FromBody] TranslateTableRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.TableName))
                {
                    return BadRequest(new { error = "Table name is required" });
                }

                var translatedName = _translationService.TranslateTableName(request.TableName, request.Strategy);
                var validation = _translationService.ValidateTranslatedName(translatedName);

                return Ok(new
                {
                    originalName = request.TableName,
                    translatedName = translatedName,
                    strategy = request.Strategy,
                    validation = validation
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error translating table name: {TableName}", request.TableName);
                return StatusCode(500, new { error = "Table translation failed", details = ex.Message });
            }
        }

        /// <summary>
        /// Get all available translation strategies
        /// </summary>
        [HttpGet]
        [Route("strategies")]
        [AllowAnonymous]
        public IActionResult GetStrategies()
        {
            try
            {
                var strategies = _translationService.GetAvailableStrategies();
                var strategyDescriptions = new Dictionary<TranslationStrategy, string>
                {
                    { TranslationStrategy.Simple, "Simple character replacement (current approach)" },
                    { TranslationStrategy.Intelligent, "Intelligent translation with common business terms" },
                    { TranslationStrategy.EnglishTranslation, "English translation of Turkish terms" },
                    { TranslationStrategy.Abbreviated, "Abbreviated version for shorter names" },
                    { TranslationStrategy.Technical, "Technical naming convention" },
                    { TranslationStrategy.Preserve, "Preserve original with minimal changes" }
                };

                return Ok(new
                {
                    strategies = strategies,
                    descriptions = strategyDescriptions
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting translation strategies");
                return StatusCode(500, new { error = "Failed to get translation strategies", details = ex.Message });
            }
        }

        /// <summary>
        /// Validate a translated name
        /// </summary>
        [HttpPost]
        [Route("validate")]
        [AllowAnonymous]
        public IActionResult ValidateTranslatedName([FromBody] ValidateNameRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.Name))
                {
                    return BadRequest(new { error = "Name is required" });
                }

                var validation = _translationService.ValidateTranslatedName(request.Name);

                return Ok(new
                {
                    name = request.Name,
                    validation = validation
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating name: {Name}", request.Name);
                return StatusCode(500, new { error = "Name validation failed", details = ex.Message });
            }
        }

        /// <summary>
        /// Batch translate multiple column names
        /// </summary>
        [HttpPost]
        [Route("batch-translate")]
        [AllowAnonymous]
        public IActionResult BatchTranslate([FromBody] BatchTranslateRequest request)
        {
            try
            {
                if (request.ColumnNames == null || !request.ColumnNames.Any())
                {
                    return BadRequest(new { error = "Column names are required" });
                }

                var results = new List<object>();

                foreach (var columnName in request.ColumnNames)
                {
                    try
                    {
                        var translatedName = _translationService.TranslateColumnName(columnName, request.Strategy);
                        var validation = _translationService.ValidateTranslatedName(translatedName);

                        results.Add(new
                        {
                            originalName = columnName,
                            translatedName = translatedName,
                            strategy = request.Strategy,
                            validation = validation
                        });
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error translating column: {ColumnName}", columnName);
                        results.Add(new
                        {
                            originalName = columnName,
                            translatedName = "ERROR",
                            strategy = request.Strategy,
                            error = ex.Message
                        });
                    }
                }

                return Ok(new
                {
                    strategy = request.Strategy,
                    results = results,
                    totalCount = request.ColumnNames.Count,
                    successCount = results.Count(r => !r.ToString().Contains("ERROR"))
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in batch translation");
                return StatusCode(500, new { error = "Batch translation failed", details = ex.Message });
            }
        }

        /// <summary>
        /// Compare different translation strategies for a set of column names
        /// </summary>
        [HttpPost]
        [Route("compare-strategies")]
        [AllowAnonymous]
        public IActionResult CompareStrategies([FromBody] CompareStrategiesRequest request)
        {
            try
            {
                if (request.ColumnNames == null || !request.ColumnNames.Any())
                {
                    return BadRequest(new { error = "Column names are required" });
                }

                var comparison = new Dictionary<string, Dictionary<TranslationStrategy, string>>();

                foreach (var columnName in request.ColumnNames)
                {
                    var preview = _translationService.GetTranslationPreview(columnName);
                    comparison[columnName] = preview;
                }

                return Ok(new
                {
                    columnNames = request.ColumnNames,
                    comparison = comparison,
                    strategies = _translationService.GetAvailableStrategies()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error comparing translation strategies");
                return StatusCode(500, new { error = "Strategy comparison failed", details = ex.Message });
            }
        }
    }

    public class TranslateColumnRequest
    {
        public string ColumnName { get; set; } = string.Empty;
        public TranslationStrategy Strategy { get; set; } = TranslationStrategy.Intelligent;
    }

    public class TranslateTableRequest
    {
        public string TableName { get; set; } = string.Empty;
        public TranslationStrategy Strategy { get; set; } = TranslationStrategy.Intelligent;
    }

    public class ValidateNameRequest
    {
        public string Name { get; set; } = string.Empty;
    }

    public class BatchTranslateRequest
    {
        public List<string> ColumnNames { get; set; } = new List<string>();
        public TranslationStrategy Strategy { get; set; } = TranslationStrategy.Intelligent;
    }

    public class CompareStrategiesRequest
    {
        public List<string> ColumnNames { get; set; } = new List<string>();
    }
}
