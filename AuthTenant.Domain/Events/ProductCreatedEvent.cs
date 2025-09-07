using AuthTenant.Domain.Entities;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace AuthTenant.Domain.Events
{
    /// <summary>
    /// Evento de domínio disparado quando um novo produto é criado no sistema.
    /// Usado para notificar outros bounded contexts sobre a criação de produtos,
    /// permitindo integração e sincronização de dados.
    /// </summary>
    public record ProductCreatedEvent : DomainEvent
    {
        /// <summary>
        /// Produto que foi criado, contendo todas as informações relevantes.
        /// </summary>
        [Required]
        public Product Product { get; init; }

        /// <summary>
        /// Informações adicionais sobre o contexto da criação.
        /// Usado para auditoria e analytics.
        /// </summary>
        public ProductCreationContext Context { get; init; }

        /// <summary>
        /// Construtor para criação via parâmetros posicionais (primary constructor).
        /// </summary>
        /// <param name="product">Produto criado</param>
        /// <param name="tenantId">ID do tenant onde o produto foi criado</param>
        /// <param name="userId">ID do usuário que criou o produto</param>
        /// <param name="source">Fonte da criação (Web, API, Import, etc.)</param>
        /// <param name="correlationId">ID de correlação para rastreamento</param>
        public ProductCreatedEvent(
            Product product,
            string tenantId,
            string? userId = null,
            string source = "Unknown",
            Guid? correlationId = null) : this()
        {
            if (product == null)
                throw new ArgumentNullException(nameof(product));

            if (string.IsNullOrWhiteSpace(tenantId))
                throw new ArgumentException("TenantId é obrigatório", nameof(tenantId));

            Product = product;
            TenantId = tenantId;
            UserId = userId;
            Context = new ProductCreationContext(source, DateTime.UtcNow);
            CorrelationId = correlationId;
        }

        /// <summary>
        /// Construtor padrão para deserialização e frameworks.
        /// </summary>
        private ProductCreatedEvent()
        {
            Product = null!; // Será definido via init
            Context = new ProductCreationContext("Unknown", DateTime.UtcNow);
        }

        /// <summary>
        /// Valida se o evento está em um estado consistente para processamento.
        /// </summary>
        /// <returns>True se válido, false caso contrário</returns>
        public override bool IsValid()
        {
            return base.IsValid() &&
                   Product != null &&
                   Product.IsValid() &&
                   !string.IsNullOrWhiteSpace(Product.Name) &&
                   Product.TenantId == TenantId; // Garantia de consistência
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

                // Implementação simplificada - combina os objetos JSON
                var combined = new Dictionary<string, object>();

                // Adiciona propriedades existentes
                foreach (var prop in existingDoc.RootElement.EnumerateObject())
                {
                    combined[prop.Name] = prop.Value.GetRawText();
                }

                // Adiciona/sobrescreve com propriedades adicionais
                foreach (var prop in additionalDoc.RootElement.EnumerateObject())
                {
                    combined[prop.Name] = prop.Value.GetRawText();
                }

                return JsonSerializer.Serialize(combined);
            }
            catch
            {
                // Em caso de erro, retorna os metadados adicionais
                return additional;
            }
        }

        /// <summary>
        /// Representação string detalhada do evento para logging e debugging.
        /// </summary>
        /// <returns>String formatada com informações do evento</returns>
        public override string ToString()
        {
            return $"ProductCreatedEvent [ProductId: {Product?.Id}, ProductName: '{Product?.Name}', " +
                   $"SKU: '{Product?.SKU}', Price: {Product?.Price:C}, Source: {Context.Source}, " +
                   $"TenantId: {TenantId}, OccurredOn: {OccurredOn:yyyy-MM-dd HH:mm:ss} UTC]";
        }
    }

    /// <summary>
    /// Contexto adicional sobre a criação do produto.
    /// Fornece informações sobre como e quando o produto foi criado.
    /// </summary>
    public record ProductCreationContext(
        string Source,
        DateTime CreatedAt,
        string? ImportBatch = null,
        string? ApiVersion = null,
        Dictionary<string, string>? AdditionalData = null)
    {
        /// <summary>
        /// Verifica se a criação foi feita via importação em lote.
        /// </summary>
        public bool IsImportCreation => !string.IsNullOrWhiteSpace(ImportBatch);

        /// <summary>
        /// Verifica se a criação foi feita via API.
        /// </summary>
        public bool IsApiCreation => !string.IsNullOrWhiteSpace(ApiVersion);

        /// <summary>
        /// Tempo decorrido desde a criação (útil para métricas).
        /// </summary>
        public TimeSpan TimeSinceCreation => DateTime.UtcNow - CreatedAt;
    }
}
