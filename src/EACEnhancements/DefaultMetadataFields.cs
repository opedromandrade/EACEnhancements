using System;
using System.Runtime.InteropServices;
using System.Text;

namespace AudioDataPlugIn
{
    internal static partial class EnhancementRuntime
    {
        // EAC's own main-window metadata edit controls.
        private const int DefaultFirstTrackNumberControlId = 674;
        private const int DefaultCdNumberControlId = 881;
        private const int DefaultTotalCdsControlId = 882;

        // Retried from the existing WM_TIMER path, so only log on change.
        private static string lastDefaultMetadataSkip = String.Empty;

        internal static string FormatTwoDigit(string value)
        {
            string text = (value ?? String.Empty).Trim();
            int number;
            if (!Int32.TryParse(
                    text,
                    System.Globalization.NumberStyles.None,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out number))
            {
                return null;
            }
            if (number < 1 || number > 9)
                return null;
            return "0" + number.ToString(
                System.Globalization.CultureInfo.InvariantCulture);
        }

        internal static void MaybeApplyMetadataDefaults(IntPtr mainWindow)
        {
            try
            {
                if (mainWindow == IntPtr.Zero || !NativeMethods.IsWindow(mainWindow))
                    return;

                // Apply once per disc context. Reset when the CD title goes
                // empty, which is how AlbumMetadata.cs already detects that
                // EAC has torn down the previous panel.
                IntPtr title = NativeMethods.GetDlgItem(mainWindow, 0x03E0);
                if (title == IntPtr.Zero)
                    return;
                string titleText = ReadChildControlText(mainWindow, 0x03E0);
                bool discLoaded = !String.IsNullOrEmpty(titleText);
                if (!discLoaded)
                {
                    metadataDefaultsAppliedForDisc = false;
                    return;
                }

                // Re-run whenever the disc changes, not just on the first
                // panel install, so previously-ripped CDs get padded too.
                if (metadataDefaultsAppliedForDisc)
                    return;

                int changed = 0;
                changed += PadControl(mainWindow, DefaultFirstTrackNumberControlId) ? 1 : 0;
                changed += PadControl(mainWindow, DefaultCdNumberControlId) ? 1 : 0;
                changed += PadControl(mainWindow, DefaultTotalCdsControlId) ? 1 : 0;

                metadataDefaultsAppliedForDisc = true;
                Log(
                    "Metadata two-digit defaults applied for the loaded disc; " +
                    "fieldsChanged=" + changed + ".");
            }
            catch (Exception error)
            {
                Log("Metadata two-digit defaults failed: " + error);
            }
        }

        private static bool metadataDefaultsAppliedForDisc;

        // Reads the control, pads a single digit to "0N", writes it back only
        // when the text actually changes. EAC's own fields are left alone for
        // anything that is already two digits, empty, or non-numeric.
        private static bool PadControl(IntPtr mainWindow, int controlId)
        {
            IntPtr control = NativeMethods.GetDlgItem(mainWindow, controlId);
            if (control == IntPtr.Zero || !NativeMethods.IsWindow(control))
                return false;

            string current = ReadChildControlText(mainWindow, controlId);
            string padded = FormatTwoDigit(current);
            if (padded == null)
                return false;
            if (String.Equals(current, padded, StringComparison.Ordinal))
                return false;

            NativeMethods.SendMessageStringW(
                control,
                NativeMethods.WM_SETTEXT,
                IntPtr.Zero,
                padded);
            Log(
                "Padded EAC metadata control " + controlId + ": '" +
                current + "' -> '" + padded + "'.");
            return true;
        }
    }
}