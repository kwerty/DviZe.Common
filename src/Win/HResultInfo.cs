namespace Kwerty.DviZe.Win;

public sealed class HResultInfo(int hResult)
{
    public const int FacilityWin32 = 7;

    public bool IsFailure { get; } = (hResult & 0x80000000) != 0;

    public int Facility { get; } = (hResult >> 16) & 0x1FFF;

    public int ErrorCode { get; } = hResult & 0xFFFF;
}
