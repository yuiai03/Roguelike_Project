/// <summary>
/// Interface cho các entity có thể bị đóng băng (freeze/slow)
/// </summary>
public interface IFreezable
{
    /// <summary>Đóng băng entity trong duration giây</summary>
    void Freeze(float duration);

    /// <summary>Làm chậm entity (0-1, 1 = dừng hoàn toàn)</summary>
    void Slow(float slowPercent, float duration);

    bool IsFrozen();
}
