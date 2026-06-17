using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace BodyCamProcessor;

public static class IconFactory
{
    public static Icon CreateIdleIcon() => CreateBodyCamIcon(Color.FromArgb(35, 160, 80));

    public static Icon CreateCopyingIcon() => CreateBodyCamIcon(Color.FromArgb(210, 45, 45));

    private static Icon CreateBodyCamIcon(Color statusColor)
    {
        using var bitmap = new Bitmap(64, 64, PixelFormat.Format32bppArgb);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.Clear(Color.Transparent);

        using var bodyBrush = new SolidBrush(Color.FromArgb(34, 38, 44));
        using var borderPen = new Pen(Color.White, 3);
        using var lensBrush = new SolidBrush(Color.FromArgb(18, 22, 28));
        using var lensPen = new Pen(Color.FromArgb(145, 180, 210), 3);
        using var statusBrush = new SolidBrush(statusColor);

        graphics.FillRoundedRectangle(bodyBrush, new Rectangle(13, 8, 38, 50), 8);
        graphics.DrawRoundedRectangle(borderPen, new Rectangle(13, 8, 38, 50), 8);
        graphics.FillEllipse(lensBrush, 22, 20, 20, 20);
        graphics.DrawEllipse(lensPen, 22, 20, 20, 20);
        graphics.FillEllipse(statusBrush, 39, 12, 8, 8);
        graphics.FillRectangle(statusBrush, 25, 45, 14, 5);

        return Icon.FromHandle(bitmap.GetHicon());
    }

    private static void FillRoundedRectangle(this Graphics graphics, Brush brush, Rectangle bounds, int radius)
    {
        using var path = CreateRoundedRectangle(bounds, radius);
        graphics.FillPath(brush, path);
    }

    private static void DrawRoundedRectangle(this Graphics graphics, Pen pen, Rectangle bounds, int radius)
    {
        using var path = CreateRoundedRectangle(bounds, radius);
        graphics.DrawPath(pen, path);
    }

    private static GraphicsPath CreateRoundedRectangle(Rectangle bounds, int radius)
    {
        var diameter = radius * 2;
        var path = new GraphicsPath();
        path.AddArc(bounds.Left, bounds.Top, diameter, diameter, 180, 90);
        path.AddArc(bounds.Right - diameter, bounds.Top, diameter, diameter, 270, 90);
        path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(bounds.Left, bounds.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        return path;
    }
}
