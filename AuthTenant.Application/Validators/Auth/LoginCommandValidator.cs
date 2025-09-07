using AuthTenant.Application.Commands.Auth;
using FluentValidation;
using System.Text.RegularExpressions;

namespace AuthTenant.Application.Validators.Auth
{
    /// <summary>
    /// Validator enterprise para comandos de login com validações robustas de segurança.
    /// Implementa regras de negócio específicas para autenticação em sistemas multi-tenant.
    /// Inclui validações de formato, comprimento e padrões de segurança avançados.
    /// </summary>
    public class LoginCommandValidator : AbstractValidator<LoginCommand>
    {
        // Constantes para validação
        private const int MinPasswordLength = 8;
        private const int MaxPasswordLength = 128;
        private const int MaxEmailLength = 254; // RFC 5321 compliance
        private const string EmailRegexPattern = @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$";
        private const string TenantIdPattern = @"^[a-zA-Z0-9\-_]{3,50}$";

        public LoginCommandValidator()
        {
            ConfigureEmailValidation();
            ConfigurePasswordValidation();
            ConfigureTenantValidation();
        }

        /// <summary>
        /// Configura validações avançadas para email com verificações de formato e segurança.
        /// </summary>
        private void ConfigureEmailValidation()
        {
            RuleFor(x => x.Email)
                .Cascade(CascadeMode.Stop)
                .NotEmpty()
                .WithMessage("Email é obrigatório para autenticação")
                .WithErrorCode("LOGIN_EMAIL_REQUIRED")

                .NotNull()
                .WithMessage("Email não pode ser nulo")
                .WithErrorCode("LOGIN_EMAIL_NULL")

                .Length(5, MaxEmailLength)
                .WithMessage($"Email deve ter entre 5 e {MaxEmailLength} caracteres")
                .WithErrorCode("LOGIN_EMAIL_LENGTH")

                .Matches(EmailRegexPattern)
                .WithMessage("Formato de email inválido. Use um email válido (ex: usuario@dominio.com)")
                .WithErrorCode("LOGIN_EMAIL_FORMAT")

                .Must(BeValidEmailDomain)
                .WithMessage("Domínio de email não permitido ou inválido")
                .WithErrorCode("LOGIN_EMAIL_DOMAIN")

                .Must(NotContainDangerousCharacters)
                .WithMessage("Email contém caracteres não permitidos")
                .WithErrorCode("LOGIN_EMAIL_SECURITY");
        }

        /// <summary>
        /// Configura validações robustas para senha incluindo critérios de segurança.
        /// </summary>
        private void ConfigurePasswordValidation()
        {
            RuleFor(x => x.Password)
                .Cascade(CascadeMode.Stop)
                .NotEmpty()
                .WithMessage("Senha é obrigatória para autenticação")
                .WithErrorCode("LOGIN_PASSWORD_REQUIRED")

                .NotNull()
                .WithMessage("Senha não pode ser nula")
                .WithErrorCode("LOGIN_PASSWORD_NULL")

                .Length(MinPasswordLength, MaxPasswordLength)
                .WithMessage($"Senha deve ter entre {MinPasswordLength} e {MaxPasswordLength} caracteres")
                .WithErrorCode("LOGIN_PASSWORD_LENGTH")

                .Must(NotContainOnlyWhitespace)
                .WithMessage("Senha não pode conter apenas espaços em branco")
                .WithErrorCode("LOGIN_PASSWORD_WHITESPACE")

                .Must(NotContainCommonPasswords)
                .WithMessage("Senha muito comum. Use uma senha mais segura")
                .WithErrorCode("LOGIN_PASSWORD_COMMON")

                .Must(BeValidPasswordFormat)
                .WithMessage("Senha deve conter ao menos: 1 letra maiúscula, 1 minúscula, 1 número e 1 caractere especial")
                .WithErrorCode("LOGIN_PASSWORD_COMPLEXITY");
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
                .WithErrorCode("LOGIN_TENANT_REQUIRED")

                .NotNull()
                .WithMessage("Tenant ID não pode ser nulo")
                .WithErrorCode("LOGIN_TENANT_NULL")

                .Length(3, 50)
                .WithMessage("Tenant ID deve ter entre 3 e 50 caracteres")
                .WithErrorCode("LOGIN_TENANT_LENGTH")

                .Matches(TenantIdPattern)
                .WithMessage("Tenant ID deve conter apenas letras, números, hífens e underscores")
                .WithErrorCode("LOGIN_TENANT_FORMAT")

                .Must(NotContainConsecutiveSpecialChars)
                .WithMessage("Tenant ID não pode conter caracteres especiais consecutivos")
                .WithErrorCode("LOGIN_TENANT_CONSECUTIVE")

                .Must(NotStartOrEndWithSpecialChars)
                .WithMessage("Tenant ID não pode começar ou terminar com caracteres especiais")
                .WithErrorCode("LOGIN_TENANT_BOUNDARIES");
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

                // Lista de domínios bloqueados (exemplos)
                var blockedDomains = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                {
                    "tempmail.com", "10minutemail.com", "guerrillamail.com",
                    "mailinator.com", "throwaway.email", "temp-mail.org"
                };

                // Verificar se não está na lista de bloqueio
                if (blockedDomains.Contains(domain)) return false;

                // Verificar se o domínio tem pelo menos um ponto
                if (!domain.Contains('.')) return false;

                // Verificar se não tem pontos consecutivos
                if (domain.Contains("..")) return false;

                // Verificar se não começa ou termina com ponto
                if (domain.StartsWith('.') || domain.EndsWith('.')) return false;

                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Verifica se o email não contém caracteres perigosos que podem ser usados em ataques.
        /// </summary>
        private static bool NotContainDangerousCharacters(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return false;

            // Caracteres perigosos que podem ser usados em injeção
            var dangerousChars = new[] { '<', '>', '"', '\'', '&', '\n', '\r', '\t' };

            return !dangerousChars.Any(email.Contains);
        }

        /// <summary>
        /// Verifica se a senha não contém apenas espaços em branco.
        /// </summary>
        private static bool NotContainOnlyWhitespace(string password)
        {
            return !string.IsNullOrWhiteSpace(password) && password.Trim().Length > 0;
        }

        /// <summary>
        /// Verifica se a senha não está na lista de senhas comuns.
        /// </summary>
        private static bool NotContainCommonPasswords(string password)
        {
            if (string.IsNullOrWhiteSpace(password)) return false;

            // Lista das senhas mais comuns (top 20)
            var commonPasswords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "password", "123456", "123456789", "12345678", "12345",
                "qwerty", "abc123", "password123", "admin", "letmein",
                "welcome", "monkey", "1234567890", "password1", "123123",
                "login", "guest", "test", "master", "dragon"
            };

            return !commonPasswords.Contains(password);
        }

        /// <summary>
        /// Valida se a senha atende aos critérios de complexidade enterprise.
        /// </summary>
        private static bool BeValidPasswordFormat(string password)
        {
            if (string.IsNullOrWhiteSpace(password)) return false;

            // Pelo menos uma letra maiúscula
            var hasUpperCase = password.Any(char.IsUpper);

            // Pelo menos uma letra minúscula  
            var hasLowerCase = password.Any(char.IsLower);

            // Pelo menos um dígito
            var hasDigit = password.Any(char.IsDigit);

            // Pelo menos um caractere especial
            var hasSpecialChar = password.Any(ch => !char.IsLetterOrDigit(ch));

            return hasUpperCase && hasLowerCase && hasDigit && hasSpecialChar;
        }

        /// <summary>
        /// Verifica se o Tenant ID não contém caracteres especiais consecutivos.
        /// </summary>
        private static bool NotContainConsecutiveSpecialChars(string tenantId)
        {
            if (string.IsNullOrWhiteSpace(tenantId)) return false;

            var specialChars = new[] { '-', '_' };

            for (int i = 0; i < tenantId.Length - 1; i++)
            {
                if (specialChars.Contains(tenantId[i]) && specialChars.Contains(tenantId[i + 1]))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Verifica se o Tenant ID não começa ou termina com caracteres especiais.
        /// </summary>
        private static bool NotStartOrEndWithSpecialChars(string tenantId)
        {
            if (string.IsNullOrWhiteSpace(tenantId)) return false;

            var specialChars = new[] { '-', '_' };

            return !specialChars.Contains(tenantId[0]) && !specialChars.Contains(tenantId[^1]);
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
            // Mensagens em português são o padrão
        }

        /// <summary>
        /// Configura mensagens em inglês para cenários internacionais.
        /// </summary>
        private void ConfigureEnglishMessages()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required for authentication")
                .EmailAddress().WithMessage("Invalid email format. Use a valid email (e.g., user@domain.com)");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Password is required for authentication")
                .Length(MinPasswordLength, MaxPasswordLength)
                .WithMessage($"Password must be between {MinPasswordLength} and {MaxPasswordLength} characters");

            RuleFor(x => x.TenantId)
                .NotEmpty().WithMessage("Tenant ID is required for multi-tenant systems")
                .Matches(TenantIdPattern)
                .WithMessage("Tenant ID must contain only letters, numbers, hyphens and underscores");
        }

        #endregion
    }
}
