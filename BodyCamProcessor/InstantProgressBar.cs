namespace BodyCamProcessor;

public sealed class InstantProgressBar : ProgressBar
{
    private int _pendingValue;

    public void SetInstantValue(int value)
    {
        _pendingValue = Math.Clamp(value, Minimum, Maximum);

        if (IsHandleCreated)
        {
            ApplyInstantValue();
        }
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        ApplyInstantValue();
    }

    private void ApplyInstantValue()
    {
        var value = Math.Clamp(_pendingValue, Minimum, Maximum);
        Value = value;

        if (value <= Minimum)
        {
            return;
        }

        Value = value - 1;
        Value = value;
    }
}
