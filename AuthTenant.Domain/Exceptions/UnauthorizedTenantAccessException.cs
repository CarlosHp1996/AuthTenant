using System.ComponentModel.DataAnnotations;

namespace AuthTenant.Domain.Exceptions
{
    /// <summary>
    /// Exceção lançada quando um usuário tenta acessar um tenant sem as devidas permissões.
    /// Representa violações de segurança, acesso cross-tenant e problemas de autorização.
    /// Usada para proteção de dados em sistemas multi-tenant.
    /// </summary>
    public sealed class UnauthorizedTenantAccessException : DomainException
    {
        /// <summary>
        /// Identificador do tenant que o usuário tentou acessar.
        /// Preservado para auditoria de segurança.
        /// </summary>
        [StringLength(50)]
        public string? RequestedTenantId { get; }

        /// <summary>
        /// Identificador do tenant ao qual o usuário pertence.
        /// Usado para detectar tentativas de acesso cross-tenant.
        /// </summary>
        [StringLength(50)]
        public string? UserTenantId { get; }

        /// <summary>
        /// Tipo de acesso que foi tentado (Read, Write, Admin, etc.).
        /// Categoriza a gravidade da tentativa de acesso.
        /// </summary>
        [StringLength(20)]
        public string AccessType { get; }

        /// <summary>
        /// Recurso específico que foi solicitado (opcional).
        /// Exemplo: "Product", "User", "Reports", etc.
        /// </summary>
        [StringLength(100)]
        public string? RequestedResource { get; }

        /// <summary>
        /// Identificador da operação ou endpoint acessado.
        /// Útil para auditoria detalhada de segurança.
        /// </summary>
        [StringLength(200)]
        public string? OperationId { get; }

        /// <summary>
        /// Endereço IP de onde veio a tentativa de acesso.
        /// Importante para análise de segurança e bloqueios.
        /// </summary>
        [StringLength(45)] // IPv6 max length
        public string? SourceIpAddress { get; }

        /// <summary>
        /// User Agent da aplicação que fez a requisição.
        /// Ajuda a identificar aplicações maliciosas.
        /// </summary>
        [StringLength(500)]
        public string? UserAgent { get; }

        /// <summary>
        /// Indica se esta é uma tentativa de acesso suspeita.
        /// Baseado em padrões de comportamento e heurísticas.
        /// </summary>
        public bool IsSuspiciousAttempt { get; }

        /// <summary>
        /// Timestamp da tentativa de acesso para análise temporal.
        /// </summary>
        public DateTime AttemptedAt { get; }

        /// <summary>
        /// Construtor principal para acesso não autorizado.
        /// </summary>
        /// <param name="requestedTenantId">ID do tenant solicitado</param>
        /// <param name="userTenantId">ID do tenant do usuário</param>
        /// <param name="userId">ID do usuário que tentou acesso</param>
        /// <param name="accessType">Tipo de acesso tentado</param>
        /// <param name="requestedResource">Recurso solicitado</param>
        /// <param name="operationId">ID da operação</param>
        /// <param name="sourceIpAddress">IP de origem</param>
        /// <param name="userAgent">User agent</param>
        /// <param name="correlationId">ID de correlação</param>
        public UnauthorizedTenantAccessException(
            string? requestedTenantId = null,
            string? userTenantId = null,
            string? userId = null,
            string accessType = "Unknown",
            string? requestedResource = null,
            string? operationId = null,
            string? sourceIpAddress = null,
            string? userAgent = null,
            Guid? correlationId = null)
            : base(
                message: BuildMessage(requestedTenantId, userTenantId, accessType, requestedResource),
                tenantId: requestedTenantId,
                userId: userId,
                errorCode: "UNAUTHORIZED_TENANT_ACCESS",
                category: "Security",
                severity: DetermineSeverity(requestedTenantId, userTenantId, accessType),
                correlationId: correlationId,
                isRetryable: false,
                shouldLog: true)
        {
            RequestedTenantId = requestedTenantId;
            UserTenantId = userTenantId;
            AccessType = accessType ?? "Unknown";
            RequestedResource = requestedResource;
            OperationId = operationId;
            SourceIpAddress = sourceIpAddress;
            UserAgent = userAgent;
            AttemptedAt = DateTime.UtcNow;
            IsSuspiciousAttempt = DetermineSuspiciousActivity(AccessType, requestedResource, sourceIpAddress);

            // Adiciona contexto de segurança
            this.WithContext(new
            {
                RequestedTenantId,
                UserTenantId,
                AccessType,
                RequestedResource,
                OperationId,
                SourceIpAddress = MaskIpAddress(sourceIpAddress),
                UserAgent = TruncateUserAgent(userAgent),
                AttemptedAt,
                IsSuspiciousAttempt,
                IsCrossTenantAttempt = !string.IsNullOrEmpty(requestedTenantId) &&
                                      !string.IsNullOrEmpty(userTenantId) &&
                                      requestedTenantId != userTenantId,
                SecurityLevel = DetermineSeverity(requestedTenantId, userTenantId, AccessType).ToString(),
                ThreatIndicators = GetThreatIndicators(AccessType, requestedResource, sourceIpAddress)
            });
        }

        /// <summary>
        /// Constrói a mensagem de erro baseada nos parâmetros de segurança.
        /// </summary>
        /// <param name="requestedTenantId">ID do tenant solicitado</param>
        /// <param name="userTenantId">ID do tenant do usuário</param>
        /// <param name="accessType">Tipo de acesso</param>
        /// <param name="requestedResource">Recurso solicitado</param>
        /// <returns>Mensagem de erro contextualizada</returns>
        private static string BuildMessage(
            string? requestedTenantId,
            string? userTenantId,
            string accessType,
            string? requestedResource)
        {
            var message = "Acesso negado ao tenant solicitado.";

            if (!string.IsNullOrEmpty(requestedTenantId) && !string.IsNullOrEmpty(userTenantId))
            {
                if (requestedTenantId != userTenantId)
                {
                    message = "Tentativa de acesso cross-tenant detectada. Acesso negado por questões de segurança.";
                }
            }

            if (!string.IsNullOrEmpty(requestedResource))
            {
                message += $" Recurso: {requestedResource}.";
            }

            if (!string.IsNullOrEmpty(accessType) && accessType != "Unknown")
            {
                message += $" Tipo de acesso: {accessType}.";
            }

            message += " Verifique suas permissões e o contexto de tenant atual.";

            return message;
        }

        /// <summary>
        /// Determina a severidade baseada no contexto do acesso.
        /// </summary>
        /// <param name="requestedTenantId">ID do tenant solicitado</param>
        /// <param name="userTenantId">ID do tenant do usuário</param>
        /// <param name="accessType">Tipo de acesso</param>
        /// <returns>Nível de severidade</returns>
        private static ExceptionSeverity DetermineSeverity(
            string? requestedTenantId,
            string? userTenantId,
            string accessType)
        {
            // Cross-tenant access é sempre high severity
            if (!string.IsNullOrEmpty(requestedTenantId) &&
                !string.IsNullOrEmpty(userTenantId) &&
                requestedTenantId != userTenantId)
            {
                return ExceptionSeverity.High;
            }

            // Admin access attempts são critical
            if (accessType?.ToLowerInvariant().Contains("admin") == true)
            {
                return ExceptionSeverity.Critical;
            }

            // Write access é medium-high
            if (accessType?.ToLowerInvariant().Contains("write") == true ||
                accessType?.ToLowerInvariant().Contains("delete") == true)
            {
                return ExceptionSeverity.High;
            }

            // Read access é medium
            return ExceptionSeverity.Medium;
        }

        /// <summary>
        /// Determina se a atividade é suspeita baseada em heurísticas.
        /// </summary>
        /// <param name="accessType">Tipo de acesso</param>
        /// <param name="requestedResource">Recurso solicitado</param>
        /// <param name="sourceIpAddress">IP de origem</param>
        /// <returns>True se suspeito</returns>
        private static bool DetermineSuspiciousActivity(
            string accessType,
            string? requestedResource,
            string? sourceIpAddress)
        {
            // Admin access sempre suspeito
            if (accessType?.ToLowerInvariant().Contains("admin") == true)
                return true;

            // Recursos sensíveis
            var sensitiveResources = new[] { "user", "admin", "config", "billing", "security" };
            if (!string.IsNullOrEmpty(requestedResource) &&
                sensitiveResources.Any(r => requestedResource.ToLowerInvariant().Contains(r)))
                return true;

            // IPs suspeitos (exemplo básico)
            if (!string.IsNullOrEmpty(sourceIpAddress))
            {
                // IPs locais em produção podem ser suspeitos
                if (sourceIpAddress.StartsWith("127.") ||
                    sourceIpAddress.StartsWith("192.168.") ||
                    sourceIpAddress.StartsWith("10."))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Mascara o endereço IP para privacy.
        /// </summary>
        /// <param name="ipAddress">IP original</param>
        /// <returns>IP mascarado</returns>
        private static string? MaskIpAddress(string? ipAddress)
        {
            if (string.IsNullOrEmpty(ipAddress))
                return ipAddress;

            var parts = ipAddress.Split('.');
            if (parts.Length == 4)
            {
                return $"{parts[0]}.{parts[1]}.xxx.xxx";
            }

            // Para IPv6, mascarar a segunda metade
            if (ipAddress.Contains(':'))
            {
                var colonIndex = ipAddress.LastIndexOf(':');
                if (colonIndex > 0)
                {
                    return ipAddress.Substring(0, colonIndex) + ":xxxx";
                }
            }

            return "xxx.xxx.xxx.xxx";
        }

        /// <summary>
        /// Trunca o User Agent para evitar logs excessivos.
        /// </summary>
        /// <param name="userAgent">User Agent original</param>
        /// <returns>User Agent truncado</returns>
        private static string? TruncateUserAgent(string? userAgent)
        {
            if (string.IsNullOrEmpty(userAgent))
                return userAgent;

            return userAgent.Length > 200 ? userAgent.Substring(0, 200) + "..." : userAgent;
        }

        /// <summary>
        /// Retorna indicadores de ameaça para análise de segurança.
        /// </summary>
        /// <param name="accessType">Tipo de acesso</param>
        /// <param name="requestedResource">Recurso solicitado</param>
        /// <param name="sourceIpAddress">IP de origem</param>
        /// <returns>Lista de indicadores</returns>
        private static List<string> GetThreatIndicators(
            string accessType,
            string? requestedResource,
            string? sourceIpAddress)
        {
            var indicators = new List<string>();

            if (accessType?.ToLowerInvariant().Contains("admin") == true)
                indicators.Add("ADMIN_ACCESS_ATTEMPT");

            if (!string.IsNullOrEmpty(requestedResource))
            {
                var sensitiveResources = new[] { "user", "admin", "config", "billing" };
                if (sensitiveResources.Any(r => requestedResource.ToLowerInvariant().Contains(r)))
                    indicators.Add("SENSITIVE_RESOURCE_ACCESS");
            }

            if (!string.IsNullOrEmpty(sourceIpAddress))
            {
                if (sourceIpAddress.StartsWith("127.") ||
                    sourceIpAddress.StartsWith("192.168.") ||
                    sourceIpAddress.StartsWith("10."))
                    indicators.Add("INTERNAL_IP_ACCESS");
            }

            return indicators;
        }

        /// <summary>
        /// Cria uma exceção para acesso cross-tenant.
        /// </summary>
        /// <param name="requestedTenantId">ID do tenant solicitado</param>
        /// <param name="userTenantId">ID do tenant do usuário</param>
        /// <param name="userId">ID do usuário</param>
        /// <param name="resource">Recurso solicitado</param>
        /// <returns>Nova instância da exceção</returns>
        public static UnauthorizedTenantAccessException ForCrossTenantAccess(
            string requestedTenantId,
            string userTenantId,
            string? userId = null,
            string? resource = null)
        {
            return new UnauthorizedTenantAccessException(
                requestedTenantId: requestedTenantId,
                userTenantId: userTenantId,
                userId: userId,
                accessType: "CrossTenant",
                requestedResource: resource);
        }

        /// <summary>
        /// Cria uma exceção para acesso administrativo não autorizado.
        /// </summary>
        /// <param name="tenantId">ID do tenant</param>
        /// <param name="userId">ID do usuário</param>
        /// <param name="operation">Operação administrativa tentada</param>
        /// <param name="sourceIp">IP de origem</param>
        /// <returns>Nova instância da exceção</returns>
        public static UnauthorizedTenantAccessException ForAdminAccess(
            string? tenantId = null,
            string? userId = null,
            string? operation = null,
            string? sourceIp = null)
        {
            return new UnauthorizedTenantAccessException(
                requestedTenantId: tenantId,
                userId: userId,
                accessType: "Admin",
                operationId: operation,
                sourceIpAddress: sourceIp);
        }

        /// <summary>
        /// Cria uma exceção para usuário sem tenant válido.
        /// </summary>
        /// <param name="userId">ID do usuário</param>
        /// <param name="sourceIp">IP de origem</param>
        /// <returns>Nova instância da exceção</returns>
        public static UnauthorizedTenantAccessException ForUserWithoutTenant(
            string? userId = null,
            string? sourceIp = null)
        {
            return new UnauthorizedTenantAccessException(
                userId: userId,
                accessType: "NoTenant",
                sourceIpAddress: sourceIp);
        }

        /// <summary>
        /// Representação string detalhada da exceção para logging de segurança.
        /// </summary>
        /// <returns>String formatada com informações de segurança</returns>
        public override string ToString()
        {
            var baseString = base.ToString();
            var securityInfo = new List<string>();

            if (!string.IsNullOrEmpty(RequestedTenantId))
                securityInfo.Add($"RequestedTenant: {RequestedTenantId}");

            if (!string.IsNullOrEmpty(UserTenantId))
                securityInfo.Add($"UserTenant: {UserTenantId}");

            securityInfo.Add($"AccessType: {AccessType}");

            if (!string.IsNullOrEmpty(RequestedResource))
                securityInfo.Add($"Resource: {RequestedResource}");

            if (!string.IsNullOrEmpty(SourceIpAddress))
                securityInfo.Add($"SourceIP: {MaskIpAddress(SourceIpAddress)}");

            if (IsSuspiciousAttempt)
                securityInfo.Add("SUSPICIOUS: true");

            securityInfo.Add($"AttemptedAt: {AttemptedAt:yyyy-MM-dd HH:mm:ss} UTC");

            var securityDetails = string.Join(" | ", securityInfo);
            return $"{baseString}\nSecurity Details: {securityDetails}";
        }
    }
}
