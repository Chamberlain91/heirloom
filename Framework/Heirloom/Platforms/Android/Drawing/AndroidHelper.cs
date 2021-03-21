#if ANDROID
using Android.App;
using Android.OS;
using Android.Util;
using Android.Views;

using Heirloom.Mathematics;

namespace Heirloom.Android
{
    public static class AndroidHelper
    {
        internal static void SetWindowFullscreen(Activity activity)
        {
            // Set application to fullscreen
            activity.RequestWindowFeature(WindowFeatures.NoTitle);
            activity.Window.SetFlags(WindowManagerFlags.Fullscreen, WindowManagerFlags.Fullscreen);

            if (Build.VERSION.SdkInt >= BuildVersionCodes.P)
            {
                // This allows the window to be visible behind camera notch, etc
                activity.Window.SetFlags(WindowManagerFlags.LayoutNoLimits, WindowManagerFlags.LayoutNoLimits);
                activity.Window.Attributes.LayoutInDisplayCutoutMode = LayoutInDisplayCutoutMode.ShortEdges;
            }
        }

        public static IntSize ComputeAutomaticResolution(Activity activity, int pixelsPerMM = 10)
        {
            var w = activity.Resources.DisplayMetrics.WidthPixels;
            var h = activity.Resources.DisplayMetrics.HeightPixels;
            var d = activity.Resources.DisplayMetrics.DensityDpi;

            return ComputeAutomaticResolution(w, h, (float) d, pixelsPerMM);
        }

        public static IntSize ComputeAutomaticResolution(int width, int height, float dpi, int pixelsPerMM)
        {
            const float MillimetersPerInch = 25.4F; // mmpi

            var wPX = width;
            var hPX = height;

            var wIN = wPX / dpi;
            var hIN = hPX / dpi;

            var wMM = wIN * MillimetersPerInch;
            var hMM = hIN * MillimetersPerInch;

            // Compute pixels per millimeter
            var wQQ = Calc.Floor(wMM * pixelsPerMM);
            var hQQ = Calc.Floor(hMM * pixelsPerMM);

            // Note: We clamp here to ensure we never go more dense than the actual pixels on the screen.
            return new IntSize(width: Calc.Min(width, wQQ), height: Calc.Min(height, hQQ));
        }
    }
}
#endif
