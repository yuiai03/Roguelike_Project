

public interface IFreezable
{

    void Freeze(float duration);

    void Slow(float slowPercent, float duration);

    bool IsFrozen();
}
