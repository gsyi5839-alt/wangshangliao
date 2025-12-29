using System;
using System.Windows.Forms;

namespace WangShangLiaoBot.Controls.BetProcess
{
    /// <summary>
    /// Bet processing basic settings control - Contains all checkbox options for bet handling
    /// </summary>
    public partial class BetBasicSettingsControl : UserControl
    {
        public BetBasicSettingsControl()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Load settings from config
        /// </summary>
        public void LoadSettings()
        {
            // TODO: Load from ConfigService
        }

        /// <summary>
        /// Save settings to config
        /// </summary>
        public void SaveSettings()
        {
            // TODO: Save to ConfigService
        }
    }
}
