using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace WangShangLiaoBot.Utils
{
    /// <summary>
    /// IME guard helpers for WinForms TextBox.
    /// - Disable IME UI (composition underline / candidate UI) by detaching IME context.
    /// - Force switch to English input language on focus to avoid "first char not visible" issues.
    /// </summary>
    public static class ImeGuard
    {
        [DllImport("imm32.dll")]
        private static extern IntPtr ImmAssociateContext(IntPtr hWnd, IntPtr hIMC);

        /// <summary>
        /// Disable IME for an input control in a safe way (do NOT intercept WndProc).
        /// </summary>
        public static void DisableIme(Control control, bool forceEnglishInput = true)
        {
            if (control == null) return;

            // Important: do not use custom WndProc for TextBox, it breaks native edit control behaviors.
            control.ImeMode = ImeMode.Disable;

            control.HandleCreated += (_, __) => DetachImeContext(control);
            control.Enter += (_, __) =>
            {
                if (forceEnglishInput) TrySwitchToEnglishInput();
                DetachImeContext(control);
            };
        }

        private static void DetachImeContext(Control control)
        {
            try
            {
                if (control.IsHandleCreated)
                {
                    ImmAssociateContext(control.Handle, IntPtr.Zero);
                }
            }
            catch
            {
                // Ignore: best-effort only.
            }
        }

        private static void TrySwitchToEnglishInput()
        {
            try
            {
                var en = InputLanguage.InstalledInputLanguages
                    .Cast<InputLanguage>()
                    .FirstOrDefault(l =>
                        l?.Culture != null &&
                        l.Culture.Name.StartsWith("en", StringComparison.OrdinalIgnoreCase));

                if (en != null)
                {
                    InputLanguage.CurrentInputLanguage = en;
                }
            }
            catch
            {
                // Ignore: some systems may not expose input languages as expected.
            }
        }
    }
}


