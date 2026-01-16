using System;
using System.Drawing;
using System.Windows.Forms;
using WangShangLiaoBot.Services;
using WangShangLiaoBot.Controls.Odds;

namespace WangShangLiaoBot.Controls
{
    /// <summary>
    /// Odds settings control - manages game play odds configuration
    /// Contains sub-tabs: Classic, Tail Ball, Dragon Tiger, Three Army, Position Ball, Other
    /// </summary>
    public partial class OddsSettingsControl : UserControl
    {
        // Sub-controls for each tab
        private ClassicPlaySettingsControl _classicControl;
        private TailBallSettingsControl _tailBallControl;
        private DragonTigerSettingsControl _dragonTigerControl;
        private ThreeArmySettingsControl _threeArmyControl;
        private PositionBallSettingsControl _positionBallControl;
        private OtherPlaySettingsControl _otherPlayControl;
        
        /// <summary>
        /// Constructor - initializes the control
        /// </summary>
        public OddsSettingsControl()
        {
            InitializeComponent();
            InitializeTabContents();
        }
        
        /// <summary>
        /// Initialize all tab page contents with actual settings controls
        /// </summary>
        private void InitializeTabContents()
        {
            // Tab 1: Classic Play (经典玩法) - Use actual ClassicPlaySettingsControl
            InitializeClassicPlayTab();
            
            // Tab 2: Tail Ball Play (尾球玩法) - Use actual TailBallSettingsControl
            InitializeTailBallTab();
            
            // Tab 3: Dragon Tiger Play (龙虎玩法) - Use actual DragonTigerSettingsControl
            InitializeDragonTigerTab();
            
            // Tab 4: Three Army Play (三军玩法) - Use actual ThreeArmySettingsControl
            InitializeThreeArmyTab();
            
            // Tab 5: Position Ball Play (定位球玩法) - Use actual PositionBallSettingsControl
            InitializePositionBallTab();
            
            // Tab 6: Other Play (其他玩法)
            InitializeOtherTab();
        }
        
        #region Classic Play Tab (经典玩法)
        
        /// <summary>
        /// Initialize Classic Play tab with actual ClassicPlaySettingsControl
        /// </summary>
        private void InitializeClassicPlayTab()
        {
            _classicControl = new ClassicPlaySettingsControl
            {
                Dock = DockStyle.Fill
            };
            _classicControl.LoadSettings();
            tabClassic.Controls.Add(_classicControl);
        }
        
        #endregion
        
        #region Tail Ball Tab (尾球玩法)
        
        /// <summary>
        /// Initialize Tail Ball tab with actual TailBallSettingsControl
        /// </summary>
        private void InitializeTailBallTab()
        {
            _tailBallControl = new TailBallSettingsControl
            {
                Dock = DockStyle.Fill
            };
            // LoadSettings is called in constructor
            tabTailBall.Controls.Add(_tailBallControl);
        }
        
        #endregion
        
        #region Dragon Tiger Tab (龙虎玩法)
        
        /// <summary>
        /// Initialize Dragon Tiger tab with actual DragonTigerSettingsControl
        /// </summary>
        private void InitializeDragonTigerTab()
        {
            _dragonTigerControl = new DragonTigerSettingsControl
            {
                Dock = DockStyle.Fill
            };
            // LoadSettings is called in constructor
            tabDragonTiger.Controls.Add(_dragonTigerControl);
        }
        
        #endregion
        
        #region Three Army Tab (三军玩法)
        
        /// <summary>
        /// Initialize Three Army tab with actual ThreeArmySettingsControl
        /// </summary>
        private void InitializeThreeArmyTab()
        {
            _threeArmyControl = new ThreeArmySettingsControl
            {
                Dock = DockStyle.Fill
            };
            // LoadSettings is called in constructor
            tabThreeArmy.Controls.Add(_threeArmyControl);
        }
        
        #endregion
        
        #region Position Ball Tab (定位球玩法)
        
        /// <summary>
        /// Initialize Position Ball tab with actual PositionBallSettingsControl
        /// </summary>
        private void InitializePositionBallTab()
        {
            _positionBallControl = new PositionBallSettingsControl
            {
                Dock = DockStyle.Fill
            };
            // LoadSettings is called in constructor
            tabPositionBall.Controls.Add(_positionBallControl);
        }
        
        #endregion
        
        #region Other Tab (其他玩法)
        
        /// <summary>
        /// Initialize Other tab content - 二七玩法、反向开奖、长龙玩法设置
        /// </summary>
        private void InitializeOtherTab()
        {
            _otherPlayControl = new OtherPlaySettingsControl
            {
                Dock = DockStyle.Fill
            };
            // LoadSettings is called in constructor
            tabOther.Controls.Add(_otherPlayControl);
        }
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// Reload all settings from config
        /// </summary>
        public void ReloadSettings()
        {
            _classicControl?.LoadSettings();
            _tailBallControl?.LoadSettings();
            _dragonTigerControl?.LoadSettings();
            _threeArmyControl?.LoadSettings();
            _positionBallControl?.LoadSettings();
            _otherPlayControl?.LoadSettings();
        }
        
        #endregion
    }
}
