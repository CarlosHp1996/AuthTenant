using AuthTenant.Domain.Entities;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace AuthTenant.Domain.Events
{
    /// <summary>
    /// Evento de domínio disparado quando um novo usuário é registrado no sistema.
    /// Usado para orchestrar processos de onboarding, envio de emails de boas-vindas,
    /// configuração de perfil inicial e integração com sistemas externos.
    /// </summary>
    public record UserRegisteredEvent : DomainEvent
    {
        /// <summary>
        /// Usuário que foi registrado, contendo todas as informações de perfil.
        /// </summary>
        [Required]
        public ApplicationUser User { get; init; }

        /// <summary>
        /// Contexto adicional sobre o registro do usuário.
        /// Inclui informações sobre origem, convites e configurações iniciais.
        /// </summary>
        public UserRegistrationContext Context { get; init; }

        /// <summary>
        /// Indica se o usuário foi registrado através de um convite.
        /// Afeta o fluxo de onboarding e configurações iniciais.
        /// </summary>
        public bool IsInvitedUser { get; init; }

        /// <summary>
        /// Indica se o email do usuário já foi confirmado no momento do registro.
        /// Alguns fluxos (como SSO) podem confirmar automaticamente o email.
        /// </summary>
        public bool IsEmailPreConfirmed { get; init; }

        /// <summary>
        /// Construtor principal para criação do evento.
        /// </summary>
        /// <param name="user">Usuário registrado</param>
        /// <param name="tenantId">ID do tenant onde o usuário foi registrado</param>
        /// <param name="registrationSource">Fonte do registro (Web, API, Import, SSO)</param>
        /// <param name="invitedBy">ID do usuário que enviou o convite (se aplicável)</param>
        /// <param name="isEmailPreConfirmed">Se o email já está confirmado</param>
        /// <param name="userAgent">User agent do navegador/aplicação</param>
        /// <param name="ipAddress">Endereço IP da origem do registro</param>
        /// <param name="correlationId">ID de correlação para rastreamento</param>
        public UserRegisteredEvent(
            ApplicationUser user,
            string tenantId,
            string registrationSource = "Web",
            string? invitedBy = null,
            bool isEmailPreConfirmed = false,
            string? userAgent = null,
            string? ipAddress = null,
            Guid? correlationId = null) : this()
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));

            if (string.IsNullOrWhiteSpace(tenantId))
                throw new ArgumentException("TenantId é obrigatório", nameof(tenantId));

            User = user;
            TenantId = tenantId;
            UserId = user.Id; // O usuário que foi registrado
            IsInvitedUser = !string.IsNullOrWhiteSpace(invitedBy);
            IsEmailPreConfirmed = isEmailPreConfirmed;
            CorrelationId = correlationId;

            Context = new UserRegistrationContext(
                Source: registrationSource,
                RegisteredAt: DateTime.UtcNow,
                InvitedBy: invitedBy,
                UserAgent: userAgent,
                IpAddress: ipAddress,
                IsSSO: registrationSource.Equals("SSO", StringComparison.OrdinalIgnoreCase)
            );
        }

        /// <summary>
        /// Construtor padrão para deserialização e frameworks.
        /// </summary>
        private UserRegisteredEvent()
        {
            User = null!; // Será definido via init
            Context = new UserRegistrationContext("Unknown", DateTime.UtcNow);
        }

        /// <summary>
        /// Valida se o evento está em um estado consistente para processamento.
        /// </summary>
        /// <returns>True se válido, false caso contrário</returns>
        public override bool IsValid()
        {
            return base.IsValid() &&
                   User != null &&
                   !string.IsNullOrWhiteSpace(User.Email) &&
                   !string.IsNullOrWhiteSpace(User.UserName) &&
                   User.TenantId == TenantId && // Garantia de consistência
                   Context.RegisteredAt <= DateTime.UtcNow;
        }

        /// <summary>
        /// Cria uma nova instância com Correlation ID atualizado.
        /// </summary>
        /// <param name="newCorrelationId">Novo ID de correlação</param>
        /// <returns>Nova instância do evento</returns>
        public override DomainEvent WithCorrelationId(Guid newCorrelationId)
        {
            return this with { CorrelationId = newCorrelationId };
        }

        /// <summary>
        /// Cria uma nova instância com metadados adicionais.
        /// </summary>
        /// <param name="additionalMetadata">Metadados em formato JSON</param>
        /// <returns>Nova instância do evento</returns>
        public override DomainEvent WithMetadata(string additionalMetadata)
        {
            var existingMetadata = Metadata ?? "{}";
            var combinedMetadata = CombineJsonMetadata(existingMetadata, additionalMetadata);
            return this with { Metadata = combinedMetadata };
        }

        /// <summary>
        /// Combina metadados JSON existentes com novos metadados.
        /// </summary>
        /// <param name="existing">Metadados existentes</param>
        /// <param name="additional">Metadados adicionais</param>
        /// <returns>JSON combinado</returns>
        private static string CombineJsonMetadata(string existing, string additional)
        {
            try
            {
                var existingDoc = JsonDocument.Parse(existing);
                var additionalDoc = JsonDocument.Parse(additional);

                var combined = new Dictionary<string, object>();

                foreach (var prop in existingDoc.RootElement.EnumerateObject())
                {
                    combined[prop.Name] = prop.Value.GetRawText();
                }

                foreach (var prop in additionalDoc.RootElement.EnumerateObject())
                {
                    combined[prop.Name] = prop.Value.GetRawText();
                }

                return JsonSerializer.Serialize(combined);
            }
            catch
            {
                return additional;
            }
        }

        /// <summary>
        /// Verifica se o usuário precisa de confirmação de email.
        /// </summary>
        public bool RequiresEmailConfirmation => !IsEmailPreConfirmed && !Context.IsSSO;

        /// <summary>
        /// Verifica se deve disparar o fluxo de onboarding completo.
        /// </summary>
        public bool RequiresOnboarding => !IsInvitedUser || Context.Source != "SSO";

        /// <summary>
        /// Representação string detalhada do evento para logging e debugging.
        /// </summary>
        /// <returns>String formatada com informações do evento</returns>
        public override string ToString()
        {
            return $"UserRegisteredEvent [UserId: {User?.Id}, Email: '{User?.Email}', " +
                   $"UserName: '{User?.UserName}', Source: {Context.Source}, " +
                   $"IsInvited: {IsInvitedUser}, EmailPreConfirmed: {IsEmailPreConfirmed}, " +
                   $"TenantId: {TenantId}, OccurredOn: {OccurredOn:yyyy-MM-dd HH:mm:ss} UTC]";
        }
    }

    /// <summary>
    /// Contexto adicional sobre o registro do usuário.
    /// Fornece informações detalhadas sobre como e quando o usuário foi registrado.
    /// </summary>
    public record UserRegistrationContext(
        string Source,
        DateTime RegisteredAt,
        string? InvitedBy = null,
        string? UserAgent = null,
        string? IpAddress = null,
        bool IsSSO = false,
        string? SSOProvider = null,
        string? RegistrationToken = null,
        Dictionary<string, string>? AdditionalData = null)
    {
        /// <summary>
        /// Verifica se o registro foi feito através de convite.
        /// </summary>
        public bool IsInvitedRegistration => !string.IsNullOrWhiteSpace(InvitedBy);

        /// <summary>
        /// Verifica se o registro foi feito via Single Sign-On.
        /// </summary>
        public bool IsSSORegistration => IsSSO && !string.IsNullOrWhiteSpace(SSOProvider);

        /// <summary>
        /// Verifica se o registro foi feito via API (baseado no User Agent).
        /// </summary>
        public bool IsApiRegistration => UserAgent?.Contains("API", StringComparison.OrdinalIgnoreCase) == true;

        /// <summary>
        /// Tempo decorrido desde o registro (útil para métricas de onboarding).
        /// </summary>
        public TimeSpan TimeSinceRegistration => DateTime.UtcNow - RegisteredAt;

        /// <summary>
        /// Obtém informações geográficas básicas do IP (se disponível).
        /// </summary>
        public string? GetLocationInfo()
        {
            // Implementação futura para geo-localização baseada em IP
            return !string.IsNullOrWhiteSpace(IpAddress) ? $"IP: {IpAddress}" : null;
        }
    }
}
