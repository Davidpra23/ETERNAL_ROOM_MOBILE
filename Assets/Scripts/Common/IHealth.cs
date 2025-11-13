using System;

public interface IHealth
{
    // Toma daño en cantidad float (implementaciones pueden redondear)
    void TakeDamage(float amount);

    // Indica si la entidad está muerta
    bool IsDead { get; }

    // Porcentaje de vida (0..1)
    float HealthPercentage { get; }
}
