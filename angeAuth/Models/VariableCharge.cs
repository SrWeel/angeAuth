using System.ComponentModel.DataAnnotations;

namespace AngeAuth.Models
{
    public enum VariableChargeType { AwsService, Electricity, ReviewTime, Other }

    public class VariableCharge
    {
        [Key] public Guid Id { get; set; } = Guid.NewGuid();
        public Guid SubscriptionId { get; set; }
        public Subscription Subscription { get; set; } = null!;
        public VariableChargeType Type { get; set; }
        public string? Description { get; set; }
        public decimal Amount { get; set; }          // Si ya tenemos monto fijo
        public decimal? Hours { get; set; }          // Si el cargo depende de horas
        public decimal? RatePerHour { get; set; }    // tarifa por hora (ej. 6.0)
        public int MonthOffset { get; set; } = 0;    // en cuántos meses se aplicará (0 = mes actual)
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // helper
        public decimal EffectiveAmount()
        {
            if (Hours.HasValue && RatePerHour.HasValue)
                return Hours.Value * RatePerHour.Value;
            return Amount;
        }
    }
}
