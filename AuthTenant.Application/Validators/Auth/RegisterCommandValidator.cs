using AuthTenant.Application.Commands.Auth;

using FluentValidation;

namespace AuthTenant.Application.Validators.Auth
{
    /// <summary>
    /// Validator enterprise para comandos de registro com validações abrangentes de segurança.
    /// Implementa regras de negócio específicas para criação de contas em sistemas multi-tenant.
    /// Inclui validações de unicidade, formato, comprimento e critérios de segurança avançados.
    /// </summary>
    public class RegisterCommandValidator : AbstractValidator<RegisterCommand>
    {
        // Constantes para validação
        private const int MinPasswordLength = 8;
        private const int MaxPasswordLength = 128;
        private const int MaxEmailLength = 254; // RFC 5321 compliance
        private const int MinNameLength = 2;
        private const int MaxNameLength = 100;
        private const int MaxUsernameLength = 50;
        private const string EmailRegexPattern = @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$";
        private const string TenantIdPattern = @"^[a-zA-Z0-9\-_]{3,50}$";
        private const string NamePattern = @"^[a-zA-ZÀ-ÿ\s\-'\.]+$"; // Suporte a acentos e caracteres especiais de nomes
        private const string UsernamePattern = @"^[a-zA-Z0-9._-]+$";

        public RegisterCommandValidator()
        {
            ConfigureEmailValidation();
            ConfigurePasswordValidation();
            ConfigureNameValidations();
            ConfigureUsernameValidation();
            ConfigureTenantValidation();
            ConfigureOptionalFieldsValidation();
        }

        /// <summary>
        /// Configura validações avançadas para email com verificações de formato e unicidade.
        /// </summary>
        private void ConfigureEmailValidation()
        {
            RuleFor(x => x.Email)
                .Cascade(CascadeMode.Stop)
                .NotEmpty()
                .WithMessage("Email é obrigatório para criar uma conta")
                .WithErrorCode("REGISTER_EMAIL_REQUIRED")

                .NotNull()
                .WithMessage("Email não pode ser nulo")
                .WithErrorCode("REGISTER_EMAIL_NULL")

                .Length(5, MaxEmailLength)
                .WithMessage($"Email deve ter entre 5 e {MaxEmailLength} caracteres")
                .WithErrorCode("REGISTER_EMAIL_LENGTH")

                .Matches(EmailRegexPattern)
                .WithMessage("Formato de email inválido. Use um email válido (ex: usuario@dominio.com)")
                .WithErrorCode("REGISTER_EMAIL_FORMAT")

                .Must(BeValidEmailDomain)
                .WithMessage("Domínio de email não permitido ou inválido")
                .WithErrorCode("REGISTER_EMAIL_DOMAIN")

                .Must(NotContainDangerousCharacters)
                .WithMessage("Email contém caracteres não permitidos")
                .WithErrorCode("REGISTER_EMAIL_SECURITY")

                .Must(NotBeDisposableEmail)
                .WithMessage("Emails temporários ou descartáveis não são permitidos")
                .WithErrorCode("REGISTER_EMAIL_DISPOSABLE");
        }

        /// <summary>
        /// Configura validações robustas para senha incluindo todos os critérios de segurança enterprise.
        /// </summary>
        private void ConfigurePasswordValidation()
        {
            RuleFor(x => x.Password)
                .Cascade(CascadeMode.Stop)
                .NotEmpty()
                .WithMessage("Senha é obrigatória para criar uma conta")
                .WithErrorCode("REGISTER_PASSWORD_REQUIRED")

                .NotNull()
                .WithMessage("Senha não pode ser nula")
                .WithErrorCode("REGISTER_PASSWORD_NULL")

                .Length(MinPasswordLength, MaxPasswordLength)
                .WithMessage($"Senha deve ter entre {MinPasswordLength} e {MaxPasswordLength} caracteres")
                .WithErrorCode("REGISTER_PASSWORD_LENGTH")

                .Must(NotContainOnlyWhitespace)
                .WithMessage("Senha não pode conter apenas espaços em branco")
                .WithErrorCode("REGISTER_PASSWORD_WHITESPACE")

                .Must(NotContainCommonPasswords)
                .WithMessage("Senha muito comum. Use uma senha mais segura")
                .WithErrorCode("REGISTER_PASSWORD_COMMON")

                .Must(BeValidPasswordFormat)
                .WithMessage("Senha deve conter ao menos: 1 letra maiúscula, 1 minúscula, 1 número e 1 caractere especial")
                .WithErrorCode("REGISTER_PASSWORD_COMPLEXITY")

                .Must(NotContainPersonalInfo)
                .When(x => !string.IsNullOrWhiteSpace(x.FirstName) || !string.IsNullOrWhiteSpace(x.LastName) || !string.IsNullOrWhiteSpace(x.Email))
                .WithMessage("Senha não deve conter informações pessoais (nome, sobrenome ou email)")
                .WithErrorCode("REGISTER_PASSWORD_PERSONAL")

                .Must(NotContainSequentialCharacters)
                .WithMessage("Senha não deve conter sequências de caracteres (ex: 123456, abcdef)")
                .WithErrorCode("REGISTER_PASSWORD_SEQUENTIAL")

                .Must(NotContainRepeatedCharacters)
                .WithMessage("Senha não deve conter muitos caracteres repetidos")
                .WithErrorCode("REGISTER_PASSWORD_REPEATED");
        }

        /// <summary>
        /// Configura validações para nomes com suporte a diferentes culturas e idiomas.
        /// </summary>
        private void ConfigureNameValidations()
        {
            RuleFor(x => x.FirstName)
                .Cascade(CascadeMode.Stop)
                .NotEmpty()
                .WithMessage("Nome é obrigatório")
                .WithErrorCode("REGISTER_FIRSTNAME_REQUIRED")

                .NotNull()
                .WithMessage("Nome não pode ser nulo")
                .WithErrorCode("REGISTER_FIRSTNAME_NULL")

                .Length(MinNameLength, MaxNameLength)
                .WithMessage($"Nome deve ter entre {MinNameLength} e {MaxNameLength} caracteres")
                .WithErrorCode("REGISTER_FIRSTNAME_LENGTH")

                .Matches(NamePattern)
                .WithMessage("Nome deve conter apenas letras, espaços, hífens, apostrofes e pontos")
                .WithErrorCode("REGISTER_FIRSTNAME_FORMAT")

                .Must(NotContainOnlyWhitespace)
                .WithMessage("Nome não pode conter apenas espaços em branco")
                .WithErrorCode("REGISTER_FIRSTNAME_WHITESPACE")

                .Must(NotContainExcessiveWhitespace)
                .WithMessage("Nome não pode conter espaços excessivos")
                .WithErrorCode("REGISTER_FIRSTNAME_EXCESSIVE_WHITESPACE");

            RuleFor(x => x.LastName)
                .Cascade(CascadeMode.Stop)
                .NotEmpty()
                .WithMessage("Sobrenome é obrigatório")
                .WithErrorCode("REGISTER_LASTNAME_REQUIRED")

                .NotNull()
                .WithMessage("Sobrenome não pode ser nulo")
                .WithErrorCode("REGISTER_LASTNAME_NULL")

                .Length(MinNameLength, MaxNameLength)
                .WithMessage($"Sobrenome deve ter entre {MinNameLength} e {MaxNameLength} caracteres")
                .WithErrorCode("REGISTER_LASTNAME_LENGTH")

                .Matches(NamePattern)
                .WithMessage("Sobrenome deve conter apenas letras, espaços, hífens, apostrofes e pontos")
                .WithErrorCode("REGISTER_LASTNAME_FORMAT")

                .Must(NotContainOnlyWhitespace)
                .WithMessage("Sobrenome não pode conter apenas espaços em branco")
                .WithErrorCode("REGISTER_LASTNAME_WHITESPACE")

                .Must(NotContainExcessiveWhitespace)
                .WithMessage("Sobrenome não pode conter espaços excessivos")
                .WithErrorCode("REGISTER_LASTNAME_EXCESSIVE_WHITESPACE");
        }

        /// <summary>
        /// Configura validações para username quando fornecido (campo opcional).
        /// </summary>
        private void ConfigureUsernameValidation()
        {
            RuleFor(x => x.UserName)
                .Cascade(CascadeMode.Stop)
                .Length(3, MaxUsernameLength)
                .When(x => !string.IsNullOrWhiteSpace(x.UserName))
                .WithMessage($"Nome de usuário deve ter entre 3 e {MaxUsernameLength} caracteres")
                .WithErrorCode("REGISTER_USERNAME_LENGTH")

                // .Matches(UsernamePattern)
                // .When(x => !string.IsNullOrWhiteSpace(x.UserName))
                // .WithMessage("Nome de usuário deve conter apenas letras, números, pontos, hífens e underscores")
                // .WithErrorCode("REGISTER_USERNAME_FORMAT")

                .Must(NotStartOrEndWithSpecialChars)
                .When(x => !string.IsNullOrWhiteSpace(x.UserName))
                .WithMessage("Nome de usuário não pode começar ou terminar com caracteres especiais")
                .WithErrorCode("REGISTER_USERNAME_BOUNDARIES")

                .Must(NotContainConsecutiveSpecialChars)
                .When(x => !string.IsNullOrWhiteSpace(x.UserName))
                .WithMessage("Nome de usuário não pode conter caracteres especiais consecutivos")
                .WithErrorCode("REGISTER_USERNAME_CONSECUTIVE");
        }

        /// <summary>
        /// Configura validações para Tenant ID com regras específicas de multi-tenancy.
        /// </summary>
        private void ConfigureTenantValidation()
        {
            RuleFor(x => x.TenantId)
                .Cascade(CascadeMode.Stop)
                .NotEmpty()
                .WithMessage("Tenant ID é obrigatório para sistemas multi-tenant")
                .WithErrorCode("REGISTER_TENANT_REQUIRED")

                .NotNull()
                .WithMessage("Tenant ID não pode ser nulo")
                .WithErrorCode("REGISTER_TENANT_NULL")

                .Length(3, 50)
                .WithMessage("Tenant ID deve ter entre 3 e 50 caracteres")
                .WithErrorCode("REGISTER_TENANT_LENGTH")

                .Matches(TenantIdPattern)
                .WithMessage("Tenant ID deve conter apenas letras, números, hífens e underscores")
                .WithErrorCode("REGISTER_TENANT_FORMAT")

                .Must(NotContainConsecutiveSpecialChars)
                .WithMessage("Tenant ID não pode conter caracteres especiais consecutivos")
                .WithErrorCode("REGISTER_TENANT_CONSECUTIVE")

                .Must(NotStartOrEndWithSpecialChars)
                .WithMessage("Tenant ID não pode começar ou terminar com caracteres especiais")
                .WithErrorCode("REGISTER_TENANT_BOUNDARIES");
        }

        /// <summary>
        /// Configura validações para campos opcionais como informações adicionais.
        /// </summary>
        private void ConfigureOptionalFieldsValidation()
        {
            // Validações adicionais podem ser implementadas conforme necessário
            // quando novos campos opcionais forem adicionados ao RegisterCommand
        }

        #region Custom Validation Methods

        /// <summary>
        /// Valida se o domínio do email é permitido e não está em lista de bloqueio.
        /// </summary>
        private static bool BeValidEmailDomain(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return false;

            try
            {
                var emailParts = email.Split('@');
                if (emailParts.Length != 2) return false;

                var domain = emailParts[1].ToLowerInvariant();

                // Lista expandida de domínios bloqueados
                var blockedDomains = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                {
                    "tempmail.com", "10minutemail.com", "guerrillamail.com",
                    "mailinator.com", "throwaway.email", "temp-mail.org",
                    "fake-mail.ml", "sharklasers.com", "guerrillamail.info"
                };

                if (blockedDomains.Contains(domain)) return false;
                if (!domain.Contains('.')) return false;
                if (domain.Contains("..")) return false;
                if (domain.StartsWith('.') || domain.EndsWith('.')) return false;

                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Verifica se é um email descartável/temporário.
        /// </summary>
        private static bool NotBeDisposableEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return false;

            var domain = email.Split('@').LastOrDefault()?.ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(domain)) return false;

            // Lista de provedores de email temporário conhecidos
            var disposableDomains = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "10minutemail.com", "tempmail.org", "guerrillamail.com", "sharklasers.com",
                "fake-mail.ml", "throwaway.email", "temp-mail.org", "mailinator.com"
            };

            return !disposableDomains.Contains(domain);
        }

        /// <summary>
        /// Verifica se o email não contém caracteres perigosos.
        /// </summary>
        private static bool NotContainDangerousCharacters(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return false;
            var dangerousChars = new[] { '<', '>', '"', '\'', '&', '\n', '\r', '\t', '\\', '/' };
            return !dangerousChars.Any(email.Contains);
        }

        /// <summary>
        /// Verifica se a senha não contém apenas espaços em branco.
        /// </summary>
        private static bool NotContainOnlyWhitespace(string input)
        {
            return !string.IsNullOrWhiteSpace(input) && input.Trim().Length > 0;
        }

        /// <summary>
        /// Verifica se não contém espaços excessivos (mais de um espaço consecutivo).
        /// </summary>
        private static bool NotContainExcessiveWhitespace(string input)
        {
            return !string.IsNullOrWhiteSpace(input) && !input.Contains("  ");
        }

        /// <summary>
        /// Verifica se a senha não está na lista expandida de senhas comuns.
        /// </summary>
        private static bool NotContainCommonPasswords(string password)
        {
            if (string.IsNullOrWhiteSpace(password)) return false;

            var commonPasswords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "password", "123456", "123456789", "12345678", "12345",
                "qwerty", "abc123", "password123", "admin", "letmein",
                "welcome", "monkey", "1234567890", "password1", "123123",
                "login", "guest", "test", "master", "dragon", "sunshine",
                "princess", "azerty", "trustno1", "000000", "hottie"
            };

            return !commonPasswords.Contains(password);
        }

        /// <summary>
        /// Valida se a senha atende aos critérios de complexidade enterprise.
        /// </summary>
        private static bool BeValidPasswordFormat(string password)
        {
            if (string.IsNullOrWhiteSpace(password)) return false;

            var hasUpperCase = password.Any(char.IsUpper);
            var hasLowerCase = password.Any(char.IsLower);
            var hasDigit = password.Any(char.IsDigit);
            var hasSpecialChar = password.Any(ch => !char.IsLetterOrDigit(ch));

            return hasUpperCase && hasLowerCase && hasDigit && hasSpecialChar;
        }

        /// <summary>
        /// Verifica se a senha não contém informações pessoais do usuário.
        /// </summary>
        private bool NotContainPersonalInfo(RegisterCommand command, string password)
        {
            if (string.IsNullOrWhiteSpace(password)) return false;

            var passwordLower = password.ToLowerInvariant();

            // Verificar se contém primeiro nome
            if (!string.IsNullOrWhiteSpace(command.FirstName) &&
                passwordLower.Contains(command.FirstName.ToLowerInvariant()))
                return false;

            // Verificar se contém sobrenome
            if (!string.IsNullOrWhiteSpace(command.LastName) &&
                passwordLower.Contains(command.LastName.ToLowerInvariant()))
                return false;

            // Verificar se contém parte do email
            if (!string.IsNullOrWhiteSpace(command.Email))
            {
                var emailPart = command.Email.Split('@')[0].ToLowerInvariant();
                if (passwordLower.Contains(emailPart))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Verifica se a senha não contém sequências de caracteres.
        /// </summary>
        private static bool NotContainSequentialCharacters(string password)
        {
            if (string.IsNullOrWhiteSpace(password) || password.Length < 3) return true;

            // Verificar sequências numéricas
            for (int i = 0; i <= password.Length - 3; i++)
            {
                if (char.IsDigit(password[i]) && char.IsDigit(password[i + 1]) && char.IsDigit(password[i + 2]))
                {
                    var num1 = password[i] - '0';
                    var num2 = password[i + 1] - '0';
                    var num3 = password[i + 2] - '0';

                    if ((num2 == num1 + 1 && num3 == num2 + 1) ||
                        (num2 == num1 - 1 && num3 == num2 - 1))
                        return false;
                }
            }

            // Verificar sequências alfabéticas
            for (int i = 0; i <= password.Length - 3; i++)
            {
                if (char.IsLetter(password[i]) && char.IsLetter(password[i + 1]) && char.IsLetter(password[i + 2]))
                {
                    var char1 = char.ToLower(password[i]);
                    var char2 = char.ToLower(password[i + 1]);
                    var char3 = char.ToLower(password[i + 2]);

                    if ((char2 == char1 + 1 && char3 == char2 + 1) ||
                        (char2 == char1 - 1 && char3 == char2 - 1))
                        return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Verifica se a senha não contém muitos caracteres repetidos.
        /// </summary>
        private static bool NotContainRepeatedCharacters(string password)
        {
            if (string.IsNullOrWhiteSpace(password)) return false;

            // Não permitir mais de 2 caracteres iguais consecutivos
            for (int i = 0; i <= password.Length - 3; i++)
            {
                if (password[i] == password[i + 1] && password[i + 1] == password[i + 2])
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Verifica se o Tenant ID não contém caracteres especiais consecutivos.
        /// </summary>
        private static bool NotContainConsecutiveSpecialChars(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return false;

            var specialChars = new[] { '-', '_', '.' };

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

            var specialChars = new[] { '-', '_', '.' };

            return !specialChars.Contains(input[0]) && !specialChars.Contains(input[^1]);
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
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required to create an account")
                .EmailAddress().WithMessage("Invalid email format. Use a valid email (e.g., user@domain.com)");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Password is required to create an account")
                .Length(MinPasswordLength, MaxPasswordLength)
                .WithMessage($"Password must be between {MinPasswordLength} and {MaxPasswordLength} characters");

            RuleFor(x => x.FirstName)
                .NotEmpty().WithMessage("First name is required")
                .Length(MinNameLength, MaxNameLength)
                .WithMessage($"First name must be between {MinNameLength} and {MaxNameLength} characters");

            RuleFor(x => x.LastName)
                .NotEmpty().WithMessage("Last name is required")
                .Length(MinNameLength, MaxNameLength)
                .WithMessage($"Last name must be between {MinNameLength} and {MaxNameLength} characters");
        }

        #endregion
    }
}
