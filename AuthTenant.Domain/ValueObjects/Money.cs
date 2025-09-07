using System.Globalization;

namespace AuthTenant.Domain.ValueObjects
{
    /// <summary>
    /// Representa um valor monetário com sua respectiva moeda.
    /// Value Object que garante imutabilidade e encapsula regras de negócio relacionadas a dinheiro.
    /// Implementa princípios de DDD para modelagem de domínio rica.
    /// </summary>
    public record Money : IComparable<Money>
    {
        #region Properties

        /// <summary>
        /// Valor monetário. Sempre não-negativo e com precisão decimal.
        /// </summary>
        public decimal Amount { get; }

        /// <summary>
        /// Código da moeda no formato ISO 4217 (ex: BRL, USD, EUR).
        /// </summary>
        public string Currency { get; }

        /// <summary>
        /// Verifica se o valor é zero.
        /// </summary>
        public bool IsZero => Amount == 0;

        /// <summary>
        /// Verifica se o valor é positivo.
        /// </summary>
        public bool IsPositive => Amount > 0;

        #endregion

        #region Constructors

        /// <summary>
        /// Inicializa uma nova instância de Money.
        /// </summary>
        /// <param name="amount">Valor monetário (deve ser não-negativo)</param>
        /// <param name="currency">Código da moeda (formato ISO 4217)</param>
        /// <exception cref="ArgumentException">Lançado quando amount é negativo ou currency é inválida</exception>
        public Money(decimal amount, string currency = "BRL")
        {
            ValidateAmount(amount);
            ValidateCurrency(currency);

            Amount = Math.Round(amount, 2, MidpointRounding.AwayFromZero);
            Currency = currency.ToUpperInvariant();
        }

        #endregion

        #region Static Factory Methods

        /// <summary>
        /// Cria uma instância Money com valor zero.
        /// </summary>
        /// <param name="currency">Código da moeda</param>
        /// <returns>Instância Money com valor zero</returns>
        public static Money Zero(string currency = "BRL") => new(0, currency);

        /// <summary>
        /// Cria uma instância Money a partir de um valor em centavos.
        /// </summary>
        /// <param name="cents">Valor em centavos</param>
        /// <param name="currency">Código da moeda</param>
        /// <returns>Instância Money convertida de centavos</returns>
        public static Money FromCents(long cents, string currency = "BRL") => new(cents / 100m, currency);

        #endregion

        #region Arithmetic Operations

        /// <summary>
        /// Adiciona outro valor monetário. Ambos devem ter a mesma moeda.
        /// </summary>
        /// <param name="other">Valor a ser adicionado</param>
        /// <returns>Novo Money com a soma dos valores</returns>
        /// <exception cref="InvalidOperationException">Lançado quando as moedas são diferentes</exception>
        public Money Add(Money other)
        {
            ValidateSameCurrency(other);
            return new Money(Amount + other.Amount, Currency);
        }

        /// <summary>
        /// Subtrai outro valor monetário. Ambos devem ter a mesma moeda.
        /// </summary>
        /// <param name="other">Valor a ser subtraído</param>
        /// <returns>Novo Money com a subtração dos valores</returns>
        /// <exception cref="InvalidOperationException">Lançado quando as moedas são diferentes</exception>
        public Money Subtract(Money other)
        {
            ValidateSameCurrency(other);
            return new Money(Amount - other.Amount, Currency);
        }

        /// <summary>
        /// Multiplica o valor por um fator.
        /// </summary>
        /// <param name="factor">Fator multiplicador</param>
        /// <returns>Novo Money com o valor multiplicado</returns>
        public Money Multiply(decimal factor)
        {
            if (factor < 0)
                throw new ArgumentException("Multiplication factor cannot be negative", nameof(factor));

            return new Money(Amount * factor, Currency);
        }

        /// <summary>
        /// Divide o valor por um divisor.
        /// </summary>
        /// <param name="divisor">Divisor</param>
        /// <returns>Novo Money com o valor dividido</returns>
        public Money Divide(decimal divisor)
        {
            if (divisor <= 0)
                throw new ArgumentException("Divisor must be positive", nameof(divisor));

            return new Money(Amount / divisor, Currency);
        }

        #endregion

        #region Operators

        public static Money operator +(Money left, Money right) => left.Add(right);
        public static Money operator -(Money left, Money right) => left.Subtract(right);
        public static Money operator *(Money money, decimal factor) => money.Multiply(factor);
        public static Money operator *(decimal factor, Money money) => money.Multiply(factor);
        public static Money operator /(Money money, decimal divisor) => money.Divide(divisor);

        public static bool operator <(Money left, Money right) => left.CompareTo(right) < 0;
        public static bool operator <=(Money left, Money right) => left.CompareTo(right) <= 0;
        public static bool operator >(Money left, Money right) => left.CompareTo(right) > 0;
        public static bool operator >=(Money left, Money right) => left.CompareTo(right) >= 0;

        public static implicit operator decimal(Money money) => money.Amount;

        #endregion

        #region Validation Methods

        private static void ValidateAmount(decimal amount)
        {
            if (amount < 0)
                throw new ArgumentException("Amount cannot be negative", nameof(amount));

            if (amount > decimal.MaxValue)
                throw new ArgumentException("Amount exceeds maximum allowed value", nameof(amount));
        }

        private static void ValidateCurrency(string currency)
        {
            if (string.IsNullOrWhiteSpace(currency))
                throw new ArgumentException("Currency cannot be null or empty", nameof(currency));

            if (currency.Length != 3)
                throw new ArgumentException("Currency must be a 3-letter ISO 4217 code", nameof(currency));

            if (!currency.All(char.IsLetter))
                throw new ArgumentException("Currency must contain only letters", nameof(currency));
        }

        private void ValidateSameCurrency(Money other)
        {
            if (other == null)
                throw new ArgumentNullException(nameof(other));

            if (Currency != other.Currency)
                throw new InvalidOperationException($"Cannot perform operation between different currencies: {Currency} and {other.Currency}");
        }

        #endregion

        #region IComparable Implementation

        /// <summary>
        /// Compara este Money com outro. Ambos devem ter a mesma moeda.
        /// </summary>
        /// <param name="other">Money para comparação</param>
        /// <returns>Valor indicando a relação de ordem</returns>
        public int CompareTo(Money? other)
        {
            if (other == null) return 1;

            ValidateSameCurrency(other);
            return Amount.CompareTo(other.Amount);
        }

        #endregion

        #region Conversion Methods

        /// <summary>
        /// Converte o valor para centavos (long).
        /// </summary>
        /// <returns>Valor em centavos</returns>
        public long ToCents() => (long)(Amount * 100);

        /// <summary>
        /// Formata o valor monetário de acordo com a cultura especificada.
        /// </summary>
        /// <param name="culture">Cultura para formatação (opcional)</param>
        /// <returns>String formatada do valor monetário</returns>
        public string ToString(CultureInfo? culture = null)
        {
            culture ??= CultureInfo.CurrentCulture;
            return $"{Amount.ToString("C", culture)} {Currency}";
        }

        /// <summary>
        /// Retorna uma representação string simplificada do valor monetário.
        /// </summary>
        /// <returns>String no formato "Amount Currency"</returns>
        public override string ToString() => $"{Amount:F2} {Currency}";

        #endregion
    }
}
