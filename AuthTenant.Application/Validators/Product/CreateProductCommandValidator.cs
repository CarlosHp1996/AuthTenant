using AuthTenant.Application.Commands.Product;
using FluentValidation;
using System.Text.RegularExpressions;

namespace AuthTenant.Application.Validators.Product
{
    /// <summary>
    /// Validator enterprise para comandos de criação de produtos com validações abrangentes.
    /// Implementa regras de negócio específicas para catálogo de produtos em sistemas multi-tenant.
    /// Inclui validações de formato, integridade de dados e regras de negócio específicas do domínio.
    /// </summary>
    public class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
    {
        // Constantes para validação
        private const int MinNameLength = 2;
        private const int MaxNameLength = 200;
        private const int MaxDescriptionLength = 2000;
        private const int MinSkuLength = 3;
        private const int MaxSkuLength = 50;
        private const decimal MinPrice = 0.01m;
        private const decimal MaxPrice = 999999.99m;
        private const int MaxStockQuantity = 1000000;

        // Padrões regex
        private const string SkuPattern = @"^[A-Z0-9\-_]+$";
        private const string ProductNamePattern = @"^[a-zA-ZÀ-ÿ0-9\s\-\.\/\(\)\&\'\+]+$";

        public CreateProductCommandValidator()
        {
            ConfigureNameValidation();
            ConfigureDescriptionValidation();
            ConfigurePriceValidation();
            ConfigureSkuValidation();
            ConfigureStockValidation();
        }

        /// <summary>
        /// Configura validações robustas para nome do produto.
        /// </summary>
        private void ConfigureNameValidation()
        {
            RuleFor(x => x.Name)
                .Cascade(CascadeMode.Stop)
                .NotEmpty()
                .WithMessage("Nome do produto é obrigatório")
                .WithErrorCode("PRODUCT_NAME_REQUIRED")

                .NotNull()
                .WithMessage("Nome do produto não pode ser nulo")
                .WithErrorCode("PRODUCT_NAME_NULL")

                .Length(MinNameLength, MaxNameLength)
                .WithMessage($"Nome do produto deve ter entre {MinNameLength} e {MaxNameLength} caracteres")
                .WithErrorCode("PRODUCT_NAME_LENGTH")

                .Matches(ProductNamePattern)
                .WithMessage("Nome do produto contém caracteres não permitidos")
                .WithErrorCode("PRODUCT_NAME_FORMAT")

                .Must(NotContainOnlyWhitespace)
                .WithMessage("Nome do produto não pode conter apenas espaços em branco")
                .WithErrorCode("PRODUCT_NAME_WHITESPACE")

                .Must(NotContainExcessiveWhitespace)
                .WithMessage("Nome do produto não pode conter espaços excessivos")
                .WithErrorCode("PRODUCT_NAME_EXCESSIVE_WHITESPACE")

                .Must(NotContainRepeatedWords)
                .WithMessage("Nome do produto não deve conter palavras repetidas")
                .WithErrorCode("PRODUCT_NAME_REPEATED_WORDS")

                .Must(BeValidProductName)
                .WithMessage("Nome do produto deve ser descritivo e profissional")
                .WithErrorCode("PRODUCT_NAME_INVALID");
        }

        /// <summary>
        /// Configura validações para descrição do produto.
        /// </summary>
        private void ConfigureDescriptionValidation()
        {
            RuleFor(x => x.Description)
                .Cascade(CascadeMode.Stop)
                .MaximumLength(MaxDescriptionLength)
                .When(x => !string.IsNullOrWhiteSpace(x.Description))
                .WithMessage($"Descrição não pode exceder {MaxDescriptionLength} caracteres")
                .WithErrorCode("PRODUCT_DESCRIPTION_LENGTH")

                .Must(NotContainOnlyWhitespace)
                .When(x => !string.IsNullOrWhiteSpace(x.Description))
                .WithMessage("Descrição não pode conter apenas espaços em branco")
                .WithErrorCode("PRODUCT_DESCRIPTION_WHITESPACE")

                .Must(NotContainDangerousContent)
                .When(x => !string.IsNullOrWhiteSpace(x.Description))
                .WithMessage("Descrição contém conteúdo não permitido")
                .WithErrorCode("PRODUCT_DESCRIPTION_DANGEROUS")

                .Must(BeValidDescription)
                .When(x => !string.IsNullOrWhiteSpace(x.Description))
                .WithMessage("Descrição deve ser informativa e profissional")
                .WithErrorCode("PRODUCT_DESCRIPTION_INVALID");
        }

        /// <summary>
        /// Configura validações rigorosas para preço do produto.
        /// </summary>
        private void ConfigurePriceValidation()
        {
            RuleFor(x => x.Price)
                .Cascade(CascadeMode.Stop)
                .GreaterThan(0)
                .WithMessage("Preço deve ser maior que zero")
                .WithErrorCode("PRODUCT_PRICE_POSITIVE")

                .InclusiveBetween(MinPrice, MaxPrice)
                .WithMessage($"Preço deve estar entre {MinPrice:C} e {MaxPrice:C}")
                .WithErrorCode("PRODUCT_PRICE_RANGE")

                .Must(HaveValidDecimalPlaces)
                .WithMessage("Preço deve ter no máximo 2 casas decimais")
                .WithErrorCode("PRODUCT_PRICE_DECIMALS")

                .Must(NotBeExtremeValue)
                .WithMessage("Preço parece incorreto. Verifique o valor informado")
                .WithErrorCode("PRODUCT_PRICE_EXTREME");
        }

        /// <summary>
        /// Configura validações para SKU (Stock Keeping Unit).
        /// </summary>
        private void ConfigureSkuValidation()
        {
            RuleFor(x => x.SKU)
                .Cascade(CascadeMode.Stop)
                .Length(MinSkuLength, MaxSkuLength)
                .When(x => !string.IsNullOrWhiteSpace(x.SKU))
                .WithMessage($"SKU deve ter entre {MinSkuLength} e {MaxSkuLength} caracteres")
                .WithErrorCode("PRODUCT_SKU_LENGTH")

                .Matches(SkuPattern)
                .When(x => !string.IsNullOrWhiteSpace(x.SKU))
                .WithMessage("SKU deve conter apenas letras maiúsculas, números, hífens e underscores")
                .WithErrorCode("PRODUCT_SKU_FORMAT")

                .Must(NotContainConsecutiveSpecialChars)
                .When(x => !string.IsNullOrWhiteSpace(x.SKU))
                .WithMessage("SKU não pode conter caracteres especiais consecutivos")
                .WithErrorCode("PRODUCT_SKU_CONSECUTIVE")

                .Must(NotStartOrEndWithSpecialChars)
                .When(x => !string.IsNullOrWhiteSpace(x.SKU))
                .WithMessage("SKU não pode começar ou terminar com caracteres especiais")
                .WithErrorCode("PRODUCT_SKU_BOUNDARIES")

                .Must(BeValidSkuFormat)
                .When(x => !string.IsNullOrWhiteSpace(x.SKU))
                .WithMessage("SKU deve seguir um formato válido (ex: PROD-2024-001)")
                .WithErrorCode("PRODUCT_SKU_PATTERN");
        }

        /// <summary>
        /// Configura validações para quantidade em estoque.
        /// </summary>
        private void ConfigureStockValidation()
        {
            RuleFor(x => x.StockQuantity)
                .Cascade(CascadeMode.Stop)
                .GreaterThanOrEqualTo(0)
                .WithMessage("Quantidade em estoque não pode ser negativa")
                .WithErrorCode("PRODUCT_STOCK_NEGATIVE")

                .LessThanOrEqualTo(MaxStockQuantity)
                .WithMessage($"Quantidade em estoque não pode exceder {MaxStockQuantity:N0} unidades")
                .WithErrorCode("PRODUCT_STOCK_MAXIMUM")

                .Must(BeReasonableStockQuantity)
                .WithMessage("Quantidade em estoque parece incorreta. Verifique o valor informado")
                .WithErrorCode("PRODUCT_STOCK_UNREASONABLE");
        }

        #region Custom Validation Methods

        /// <summary>
        /// Verifica se não contém apenas espaços em branco.
        /// </summary>
        private static bool NotContainOnlyWhitespace(string input)
        {
            return !string.IsNullOrWhiteSpace(input) && input.Trim().Length > 0;
        }

        /// <summary>
        /// Verifica se não contém espaços excessivos.
        /// </summary>
        private static bool NotContainExcessiveWhitespace(string input)
        {
            return !string.IsNullOrWhiteSpace(input) && !input.Contains("  ");
        }

        /// <summary>
        /// Verifica se o nome não contém palavras repetidas.
        /// </summary>
        private static bool NotContainRepeatedWords(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return false;

            var words = name.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                           .Select(w => w.ToLowerInvariant())
                           .ToArray();

            return words.Length == words.Distinct().Count();
        }

        /// <summary>
        /// Valida se é um nome de produto válido.
        /// </summary>
        private static bool BeValidProductName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return false;

            // Lista de palavras não permitidas em nomes de produtos
            var forbiddenWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "teste", "test", "dummy", "sample", "exemplo", "fake", "temporário", "temp"
            };

            var words = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return !words.Any(word => forbiddenWords.Contains(word.ToLowerInvariant()));
        }

        /// <summary>
        /// Verifica se a descrição não contém conteúdo perigoso.
        /// </summary>
        private static bool NotContainDangerousContent(string description)
        {
            if (string.IsNullOrWhiteSpace(description)) return true;

            var dangerousPatterns = new[]
            {
                "<script", "javascript:", "vbscript:", "onload=", "onerror=",
                "eval(", "document.cookie", "window.location"
            };

            return !dangerousPatterns.Any(pattern =>
                description.Contains(pattern, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Valida se é uma descrição válida.
        /// </summary>
        private static bool BeValidDescription(string description)
        {
            if (string.IsNullOrWhiteSpace(description)) return true;

            // Verificar se não é muito repetitiva
            if (description.Length > 50)
            {
                var words = description.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (words.Length > 10)
                {
                    var uniqueWords = words.Distinct(StringComparer.OrdinalIgnoreCase).Count();
                    var repetitionRatio = (double)uniqueWords / words.Length;

                    // Se menos de 30% das palavras são únicas, pode ser spam
                    if (repetitionRatio < 0.3) return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Verifica se o preço tem no máximo 2 casas decimais.
        /// </summary>
        private static bool HaveValidDecimalPlaces(decimal price)
        {
            var decimalPlaces = BitConverter.GetBytes(decimal.GetBits(price)[3])[2];
            return decimalPlaces <= 2;
        }

        /// <summary>
        /// Verifica se o preço não é um valor extremo suspeito.
        /// </summary>
        private static bool NotBeExtremeValue(decimal price)
        {
            // Verificar valores muito baixos (menos de 1 centavo) ou muito altos
            return price >= 0.01m && price <= 100000m;
        }

        /// <summary>
        /// Verifica se não contém caracteres especiais consecutivos.
        /// </summary>
        private static bool NotContainConsecutiveSpecialChars(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return false;

            var specialChars = new[] { '-', '_' };

            for (int i = 0; i < input.Length - 1; i++)
            {
                if (specialChars.Contains(input[i]) && specialChars.Contains(input[i + 1]))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Verifica se não começa ou termina com caracteres especiais.
        /// </summary>
        private static bool NotStartOrEndWithSpecialChars(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return false;

            var specialChars = new[] { '-', '_' };

            return !specialChars.Contains(input[0]) && !specialChars.Contains(input[^1]);
        }

        /// <summary>
        /// Valida se o SKU segue um formato válido.
        /// </summary>
        private static bool BeValidSkuFormat(string sku)
        {
            if (string.IsNullOrWhiteSpace(sku)) return true;

            // Aceitar diferentes formatos comuns de SKU
            var validPatterns = new[]
            {
                @"^[A-Z]{2,4}-\d{4}-\d{3,6}$",      // PROD-2024-001
                @"^[A-Z]{3,10}-\d{3,10}$",          // PRODUCT-123
                @"^[A-Z]{2,5}\d{3,10}$",            // ABC123
                @"^[A-Z0-9]{5,15}$"                 // ALPHANUMERIC
            };

            return validPatterns.Any(pattern => Regex.IsMatch(sku, pattern));
        }

        /// <summary>
        /// Verifica se a quantidade em estoque é razoável.
        /// </summary>
        private static bool BeReasonableStockQuantity(int stockQuantity)
        {
            // Para novos produtos, quantidades muito altas podem ser suspeitas
            return stockQuantity <= 50000; // Limite razoável para a maioria dos produtos
        }

        #endregion

        #region Validation Message Customization

        /// <summary>
        /// Configura mensagens de erro personalizadas baseadas na cultura do usuário.
        /// </summary>
        public void ConfigureCustomMessages(string culture = "pt-BR")
        {
            if (culture == "en-US")
            {
                ConfigureEnglishMessages();
            }
        }

        /// <summary>
        /// Configura mensagens em inglês para cenários internacionais.
        /// </summary>
        private void ConfigureEnglishMessages()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Product name is required")
                .Length(MinNameLength, MaxNameLength)
                .WithMessage($"Product name must be between {MinNameLength} and {MaxNameLength} characters");

            RuleFor(x => x.Price)
                .GreaterThan(0).WithMessage("Price must be greater than zero")
                .InclusiveBetween(MinPrice, MaxPrice)
                .WithMessage($"Price must be between {MinPrice:C} and {MaxPrice:C}");

            RuleFor(x => x.StockQuantity)
                .GreaterThanOrEqualTo(0).WithMessage("Stock quantity cannot be negative");
        }

        #endregion
    }
}
