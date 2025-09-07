using MediatR;
using System.ComponentModel.DataAnnotations;

namespace AuthTenant.Domain.Events
{
    /// <summary>
    /// Representa um evento de domínio base que pode ser publicado quando algo significativo acontece no domínio.
    /// Implementa INotification do MediatR para integração com o padrão Mediator.
    /// Todos os eventos de domínio herdam desta classe base.
    /// </summary>
    public abstract record DomainEvent : INotification
    {
        /// <summary>
        /// Identificador único do evento.
        /// Gerado automaticamente para rastreabilidade e debugging.
        /// </summary>
        public Guid Id { get; init; } = Guid.NewGuid();

        /// <summary>
        /// Timestamp UTC de quando o evento ocorreu.
        /// Usado para auditoria, ordenação e analytics.
        /// </summary>
        public DateTime OccurredOn { get; init; } = DateTime.UtcNow;

        /// <summary>
        /// Identificador do tenant onde o evento ocorreu.
        /// Essencial para sistemas multi-tenant e isolamento de dados.
        /// </summary>
        [Required]
        [StringLength(50, MinimumLength = 1)]
        public string TenantId { get; init; } = string.Empty;

        /// <summary>
        /// Identificador do usuário que causou o evento (opcional).
        /// Usado para auditoria e rastreamento de ações do usuário.
        /// </summary>
        [StringLength(50)]
        public string? UserId { get; init; }

        /// <summary>
        /// Versão do evento para versionamento e evolução de esquemas.
        /// Permite compatibilidade com versões anteriores.
        /// </summary>
        public int Version { get; init; } = 1;

        /// <summary>
        /// Metadados adicionais do evento em formato JSON.
        /// Permite extensibilidade sem quebrar contratos existentes.
        /// </summary>
        [StringLength(2000)]
        public string? Metadata { get; init; }

        /// <summary>
        /// Correlação de ID para rastrear eventos relacionados.
        /// Útil para distributed tracing e debugging de fluxos complexos.
        /// </summary>
        public Guid? CorrelationId { get; init; }

        /// <summary>
        /// Nome do tipo de evento para indexação e busca.
        /// Calculado automaticamente baseado no nome da classe.
        /// </summary>
        public string EventType => GetType().Name;

        /// <summary>
        /// Namespace do evento para categorização e filtragem.
        /// Usado para organizar eventos por contexto de domínio.
        /// </summary>
        public string EventNamespace => GetType().Namespace ?? "Unknown";

        /// <summary>
        /// Verifica se o evento é válido para processamento.
        /// Valida propriedades obrigatórias e consistência de dados.
        /// </summary>
        /// <returns>True se o evento é válido, false caso contrário.</returns>
        public virtual bool IsValid()
        {
            return !string.IsNullOrWhiteSpace(TenantId) &&
                   OccurredOn <= DateTime.UtcNow &&
                   Version > 0;
        }

        /// <summary>
        /// Cria uma cópia do evento com um novo Correlation ID.
        /// Útil para reprocessamento e retry de eventos.
        /// </summary>
        /// <param name="newCorrelationId">Novo ID de correlação</param>
        /// <returns>Nova instância do evento com correlation ID atualizado</returns>
        public abstract DomainEvent WithCorrelationId(Guid newCorrelationId);

        /// <summary>
        /// Cria uma cópia do evento com metadados adicionais.
        /// Permite enriquecimento de eventos sem modificar a estrutura original.
        /// </summary>
        /// <param name="additionalMetadata">Metadados a serem adicionados</param>
        /// <returns>Nova instância do evento com metadados atualizados</returns>
        public abstract DomainEvent WithMetadata(string additionalMetadata);

        /// <summary>
        /// Representação string estruturada do evento para logging.
        /// Inclui informações essenciais para debugging e auditoria.
        /// </summary>
        /// <returns>String formatada com detalhes do evento</returns>
        public override string ToString()
        {
            return $"{EventType} [Id: {Id}, TenantId: {TenantId}, OccurredOn: {OccurredOn:yyyy-MM-dd HH:mm:ss} UTC, Version: {Version}]";
        }
    }
}
