using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace AuthTenant.Domain.Exceptions
{
    /// <summary>
    /// Classe base abstrata para todas as exceções de domínio no sistema AuthTenant.
    /// Representa violações de regras de negócio, invariantes de domínio e estados inválidos.
    /// Fornece funcionalidades comuns de contextualização, auditoria e serialização.
    /// </summary>
    public abstract class DomainException : Exception
    {
        /// <summary>
        /// Identificador único da exceção para rastreamento e correlação.
        /// Útil para logging, debugging e suporte técnico.
        /// </summary>
        public Guid ExceptionId { get; init; } = Guid.NewGuid();

        /// <summary>
        /// Timestamp UTC de quando a exceção foi criada.
        /// Usado para auditoria, análise temporal e debugging.
        /// </summary>
        public DateTime OccurredAt { get; init; } = DateTime.UtcNow;

        /// <summary>
        /// Identificador do tenant onde a exceção ocorreu.
        /// Essencial para sistemas multi-tenant e isolamento de dados.
        /// </summary>
        [StringLength(50)]
        public string? TenantId { get; init; }

        /// <summary>
        /// Identificador do usuário que causou a exceção (se aplicável).
        /// Usado para auditoria e rastreamento de ações do usuário.
        /// </summary>
        [StringLength(50)]
        public string? UserId { get; init; }

        /// <summary>
        /// Código de erro específico do domínio para categorização.
        /// Permite tratamento programático diferenciado por tipo de erro.
        /// </summary>
        [StringLength(20)]
        public string? ErrorCode { get; init; }

        /// <summary>
        /// Categoria da exceção para agrupamento e filtragem.
        /// Exemplos: Validation, Business, Security, NotFound, etc.
        /// </summary>
        [StringLength(50)]
        public string Category { get; init; } = "Domain";

        /// <summary>
        /// Severidade da exceção para priorização e alertas.
        /// Valores: Low, Medium, High, Critical
        /// </summary>
        public ExceptionSeverity Severity { get; init; } = ExceptionSeverity.Medium;

        /// <summary>
        /// Contexto adicional da exceção em formato JSON.
        /// Permite informações estruturadas específicas do erro.
        /// </summary>
        [StringLength(2000)]
        public string? Context { get; private set; }

        /// <summary>
        /// Correlação de ID para rastrear exceções relacionadas.
        /// Útil para distributed tracing e debugging de fluxos complexos.
        /// </summary>
        public Guid? CorrelationId { get; init; }

        /// <summary>
        /// Indica se a exceção pode ser recuperada através de retry.
        /// Útil para implementação de políticas de resilência.
        /// </summary>
        public bool IsRetryable { get; init; } = false;

        /// <summary>
        /// Indica se a exceção deve ser logada.
        /// Algumas exceções esperadas podem não precisar de log.
        /// </summary>
        public bool ShouldLog { get; init; } = true;

        /// <summary>
        /// Propriedades adicionais da exceção para extensibilidade.
        /// Permite dados específicos sem quebrar contratos existentes.
        /// </summary>
        public Dictionary<string, object> Properties { get; init; } = new();

        /// <summary>
        /// Nome do tipo de exceção para categorização.
        /// Calculado automaticamente baseado no nome da classe.
        /// </summary>
        public string ExceptionType => GetType().Name;

        /// <summary>
        /// Namespace da exceção para organização hierárquica.
        /// Usado para categorizar exceções por contexto de domínio.
        /// </summary>
        public string ExceptionNamespace => GetType().Namespace ?? "Unknown";

        /// <summary>
        /// Construtor base para exceções de domínio com contexto mínimo.
        /// </summary>
        /// <param name="message">Mensagem descritiva do erro</param>
        /// <param name="tenantId">ID do tenant onde ocorreu o erro</param>
        /// <param name="userId">ID do usuário relacionado ao erro</param>
        /// <param name="errorCode">Código específico do erro</param>
        protected DomainException(
            string message,
            string? tenantId = null,
            string? userId = null,
            string? errorCode = null) : base(message)
        {
            TenantId = tenantId;
            UserId = userId;
            ErrorCode = errorCode;
        }

        /// <summary>
        /// Construtor para exceções de domínio com exceção interna.
        /// </summary>
        /// <param name="message">Mensagem descritiva do erro</param>
        /// <param name="innerException">Exceção que causou este erro</param>
        /// <param name="tenantId">ID do tenant onde ocorreu o erro</param>
        /// <param name="userId">ID do usuário relacionado ao erro</param>
        /// <param name="errorCode">Código específico do erro</param>
        protected DomainException(
            string message,
            Exception innerException,
            string? tenantId = null,
            string? userId = null,
            string? errorCode = null) : base(message, innerException)
        {
            TenantId = tenantId;
            UserId = userId;
            ErrorCode = errorCode;
        }

        /// <summary>
        /// Construtor completo para máximo controle sobre a exceção.
        /// </summary>
        /// <param name="message">Mensagem descritiva do erro</param>
        /// <param name="tenantId">ID do tenant onde ocorreu o erro</param>
        /// <param name="userId">ID do usuário relacionado ao erro</param>
        /// <param name="errorCode">Código específico do erro</param>
        /// <param name="category">Categoria da exceção</param>
        /// <param name="severity">Severidade da exceção</param>
        /// <param name="context">Contexto adicional em JSON</param>
        /// <param name="correlationId">ID de correlação</param>
        /// <param name="isRetryable">Se a operação pode ser tentada novamente</param>
        /// <param name="shouldLog">Se a exceção deve ser logada</param>
        /// <param name="innerException">Exceção interna</param>
        protected DomainException(
            string message,
            string? tenantId = null,
            string? userId = null,
            string? errorCode = null,
            string category = "Domain",
            ExceptionSeverity severity = ExceptionSeverity.Medium,
            string? context = null,
            Guid? correlationId = null,
            bool isRetryable = false,
            bool shouldLog = true,
            Exception? innerException = null) : base(message, innerException)
        {
            TenantId = tenantId;
            UserId = userId;
            ErrorCode = errorCode;
            Category = category;
            Severity = severity;
            Context = context;
            CorrelationId = correlationId;
            IsRetryable = isRetryable;
            ShouldLog = shouldLog;
        }

        /// <summary>
        /// Adiciona uma propriedade personalizada à exceção.
        /// </summary>
        /// <param name="key">Chave da propriedade</param>
        /// <param name="value">Valor da propriedade</param>
        /// <returns>A própria instância para fluent API</returns>
        public DomainException WithProperty(string key, object value)
        {
            if (!string.IsNullOrWhiteSpace(key))
            {
                Properties[key] = value;
            }
            return this;
        }

        /// <summary>
        /// Adiciona contexto estruturado à exceção.
        /// </summary>
        /// <param name="contextData">Dados de contexto para serializar</param>
        /// <returns>A própria instância para fluent API</returns>
        public DomainException WithContext(object contextData)
        {
            try
            {
                var contextJson = JsonSerializer.Serialize(contextData, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = false
                });

                // Combina contexto existente com novo contexto
                Context = string.IsNullOrWhiteSpace(Context)
                    ? contextJson
                    : CombineJsonContext(Context, contextJson);
            }
            catch
            {
                // Se falhar na serialização, armazena como string
                Context = contextData?.ToString();
            }

            return this;
        }

        /// <summary>
        /// Define o ID de correlação da exceção.
        /// </summary>
        /// <param name="correlationId">ID de correlação</param>
        /// <returns>A própria instância para fluent API</returns>
        public DomainException WithCorrelationId(Guid correlationId)
        {
            Properties["CorrelationId"] = correlationId;
            return this;
        }

        /// <summary>
        /// Serializa a exceção para JSON incluindo todas as propriedades relevantes.
        /// Útil para logging estruturado e diagnóstico.
        /// </summary>
        /// <returns>JSON representando a exceção</returns>
        public string ToJson()
        {
            var exceptionData = new
            {
                ExceptionId,
                OccurredAt,
                ExceptionType,
                ExceptionNamespace,
                Message,
                TenantId,
                UserId,
                ErrorCode,
                Category,
                Severity = Severity.ToString(),
                Context,
                CorrelationId,
                IsRetryable,
                ShouldLog,
                Properties,
                StackTrace = StackTrace?.Split('\n').Take(10), // Limita stack trace
                InnerException = InnerException?.Message
            };

            return JsonSerializer.Serialize(exceptionData, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            });
        }

        /// <summary>
        /// Combina dois objetos JSON em um único objeto.
        /// </summary>
        /// <param name="existing">JSON existente</param>
        /// <param name="additional">JSON adicional</param>
        /// <returns>JSON combinado</returns>
        private static string CombineJsonContext(string existing, string additional)
        {
            try
            {
                var existingObj = JsonSerializer.Deserialize<Dictionary<string, object>>(existing);
                var additionalObj = JsonSerializer.Deserialize<Dictionary<string, object>>(additional);

                if (existingObj != null && additionalObj != null)
                {
                    foreach (var kvp in additionalObj)
                    {
                        existingObj[kvp.Key] = kvp.Value;
                    }

                    return JsonSerializer.Serialize(existingObj, new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    });
                }
            }
            catch
            {
                // Se falhar, retorna os JSONs separados
                return $"{existing},{additional}";
            }

            return additional;
        }

        /// <summary>
        /// Representação string estruturada da exceção para logging.
        /// Inclui informações essenciais para debugging e auditoria.
        /// </summary>
        /// <returns>String formatada com detalhes da exceção</returns>
        public override string ToString()
        {
            var details = new List<string>
            {
                $"Type: {ExceptionType}",
                $"Message: {Message}",
                $"ExceptionId: {ExceptionId}",
                $"OccurredAt: {OccurredAt:yyyy-MM-dd HH:mm:ss} UTC",
                $"Category: {Category}",
                $"Severity: {Severity}"
            };

            if (!string.IsNullOrWhiteSpace(TenantId))
                details.Add($"TenantId: {TenantId}");

            if (!string.IsNullOrWhiteSpace(UserId))
                details.Add($"UserId: {UserId}");

            if (!string.IsNullOrWhiteSpace(ErrorCode))
                details.Add($"ErrorCode: {ErrorCode}");

            if (CorrelationId.HasValue)
                details.Add($"CorrelationId: {CorrelationId}");

            if (!string.IsNullOrWhiteSpace(Context))
                details.Add($"Context: {Context}");

            if (Properties.Any())
                details.Add($"Properties: {string.Join(", ", Properties.Select(p => $"{p.Key}={p.Value}"))}");

            var result = string.Join(" | ", details);

            if (InnerException != null)
                result += $"\nInner Exception: {InnerException.Message}";

            return result;
        }
    }

    /// <summary>
    /// Enumeração para categorizar a severidade das exceções de domínio.
    /// Permite priorização no tratamento, alertas e escalação.
    /// </summary>
    public enum ExceptionSeverity
    {
        /// <summary>
        /// Baixa severidade - Erros esperados e controláveis.
        /// Exemplo: Validação de entrada, regras de negócio simples.
        /// </summary>
        Low = 1,

        /// <summary>
        /// Média severidade - Erros significativos mas não críticos.
        /// Exemplo: Dados não encontrados, permissões insuficientes.
        /// </summary>
        Medium = 2,

        /// <summary>
        /// Alta severidade - Erros que afetam funcionalidades importantes.
        /// Exemplo: Falhas em integrações, inconsistências de dados.
        /// </summary>
        High = 3,

        /// <summary>
        /// Severidade crítica - Erros que comprometem o sistema.
        /// Exemplo: Corrupção de dados, falhas de segurança.
        /// </summary>
        Critical = 4
    }
}
