using System.ComponentModel.DataAnnotations;

namespace AuthTenant.Domain.Interfaces
{
    /// <summary>
    /// Interface que define o contrato para entidades auditáveis no sistema.
    /// Fornece propriedades para rastreamento completo de criação e modificação.
    /// Implementa padrões de auditoria para compliance e rastreabilidade.
    /// </summary>
    public interface IAuditableEntity
    {
        /// <summary>
        /// Data e hora UTC de criação da entidade.
        /// Definida automaticamente no momento da persistência inicial.
        /// Imutável após a criação para garantir integridade do audit trail.
        /// </summary>
        [Required]
        DateTime CreatedAt { get; set; }

        /// <summary>
        /// Identificador do usuário ou sistema que criou a entidade.
        /// Pode ser um ID de usuário, nome de sistema ou processo automatizado.
        /// Essencial para auditoria e compliance regulatório.
        /// </summary>
        [StringLength(100)]
        string? CreatedBy { get; set; }

        /// <summary>
        /// Data e hora UTC da última modificação da entidade.
        /// Atualizada automaticamente a cada operação de update.
        /// Null se a entidade nunca foi modificada após a criação.
        /// </summary>
        DateTime? UpdatedAt { get; set; }

        /// <summary>
        /// Identificador do usuário ou sistema que fez a última modificação.
        /// Rastreia o responsável pelas mudanças para auditoria.
        /// Null se a entidade nunca foi modificada após a criação.
        /// </summary>
        [StringLength(100)]
        string? UpdatedBy { get; set; }

        /// <summary>
        /// Indica se a entidade foi modificada desde a criação.
        /// Propriedade computada baseada na existência de UpdatedAt.
        /// Útil para otimizações e validações de estado.
        /// </summary>
        bool HasBeenModified { get; }

        /// <summary>
        /// Retorna o tempo decorrido desde a criação da entidade.
        /// Calculado dinamicamente baseado no timestamp atual.
        /// Útil para métricas de idade de dados e políticas de retenção.
        /// </summary>
        TimeSpan Age { get; }

        /// <summary>
        /// Retorna o tempo decorrido desde a última modificação.
        /// Null se a entidade nunca foi modificada.
        /// Útil para detectar dados obsoletos e triggering de atualizações.
        /// </summary>
        TimeSpan? TimeSinceLastUpdate { get; }

        /// <summary>
        /// Marca a entidade como atualizada no momento atual.
        /// Define UpdatedAt para UTC now e UpdatedBy para o usuário fornecido.
        /// Deve ser chamado antes de operações de update.
        /// </summary>
        /// <param name="updatedBy">Identificador do usuário responsável pela atualização</param>
        void MarkAsUpdated(string? updatedBy = null);

        /// <summary>
        /// Valida se as propriedades de auditoria estão em estado consistente.
        /// Verifica regras como: CreatedAt não pode ser futuro, UpdatedAt deve ser posterior a CreatedAt, etc.
        /// Útil para validação de integridade antes da persistência.
        /// </summary>
        /// <returns>True se o estado de auditoria é válido</returns>
        bool IsAuditStateValid();

        /// <summary>
        /// Cria um resumo das informações de auditoria para logging.
        /// Formata as datas e responsáveis de forma legível.
        /// Útil para logs estruturados e debugging.
        /// </summary>
        /// <returns>String formatada com informações de auditoria</returns>
        string GetAuditSummary();
    }
}
