using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Timers;
using WangShangLiaoBot.Models;
using WangShangLiaoBot.Services;
using WangShangLiaoBot.Services.HPSocket;
using WangShangLiaoBot.Forms.Settings;
using WangShangLiaoBot.Controls;
using WangShangLiaoBot.Controls.BetProcess;
using WangShangLiaoBot.Controls.Odds;

namespace WangShangLiaoBot.Forms
{
    public partial class MainForm : Form
    {
        private void InitializeSettingsControls()
        {
            // =====================================================
            // 账单设置 Tab - 使用两个面板实现自适应布局
            // =====================================================
            tabBillSettings.AutoScroll = true;
            
            // 左侧面板 - 固定宽度，可滚动
            var pnlBillLeft = new Panel();
            pnlBillLeft.Location = new System.Drawing.Point(0, 0);
            pnlBillLeft.Size = new System.Drawing.Size(340, 500);
            pnlBillLeft.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Bottom;
            pnlBillLeft.AutoScroll = true;
            tabBillSettings.Controls.Add(pnlBillLeft);
            
            // 右侧面板 - 自适应宽度
            var pnlBillRight = new Panel();
            pnlBillRight.Location = new System.Drawing.Point(345, 0);
            pnlBillRight.Size = new System.Drawing.Size(450, 500);
            pnlBillRight.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
            pnlBillRight.AutoScroll = true;
            tabBillSettings.Controls.Add(pnlBillRight);
            
            // 左侧面板控件
            _billSendCtrl = new BillSendSettingsControl();
            _billSendCtrl.Location = new System.Drawing.Point(3, 3);
            pnlBillLeft.Controls.Add(_billSendCtrl);

            _billFormatCtrl = new BillFormatSettingsControl();
            _billFormatCtrl.Location = new System.Drawing.Point(3, 58);
            pnlBillLeft.Controls.Add(_billFormatCtrl);

            _groupTaskCtrl = new GroupTaskSettingsControl();
            _groupTaskCtrl.Location = new System.Drawing.Point(3, 250);
            pnlBillLeft.Controls.Add(_groupTaskCtrl);

            _billReplaceCtrl = new BillReplaceCommandControl();
            _billReplaceCtrl.Location = new System.Drawing.Point(3, 355);
            pnlBillLeft.Controls.Add(_billReplaceCtrl);

            // 右侧面板控件 - 使用 Dock.Top 自适应宽度，按反序添加（后加的在上面）
            _feedbackSettingsCtrl = new FeedbackSettingsControl();
            _feedbackSettingsCtrl.Padding = new Padding(3, 5, 3, 5);
            pnlBillRight.Controls.Add(_feedbackSettingsCtrl);

            _muteSettingsCtrl = new MuteSettingsControl();
            _muteSettingsCtrl.Padding = new Padding(3, 5, 3, 5);
            pnlBillRight.Controls.Add(_muteSettingsCtrl);

            _basicSettingsCtrl = new BasicSettingsControl();
            _basicSettingsCtrl.Padding = new Padding(3, 5, 3, 5);
            pnlBillRight.Controls.Add(_basicSettingsCtrl);

            // =====================================================
            // 下注处理 Tab - 包含二级 TabControl
            // =====================================================
            _betProcessTabControl = new TabControl();
            _betProcessTabControl.Location = new System.Drawing.Point(0, 0);
            _betProcessTabControl.Size = new System.Drawing.Size(670, 450);
            _betProcessTabControl.Dock = DockStyle.Fill;

            // 基本设置 子标签
            var tabBasicSettings = new TabPage();
            tabBasicSettings.Text = "基本设置";
            tabBasicSettings.Padding = new Padding(3);
            _betProcessTabControl.TabPages.Add(tabBasicSettings);

            _betBasicSettingsCtrl = new BetBasicSettingsControl();
            _betBasicSettingsCtrl.Location = new System.Drawing.Point(5, 5);
            _betBasicSettingsCtrl.Dock = DockStyle.Fill;
            tabBasicSettings.Controls.Add(_betBasicSettingsCtrl);

            // 攻击范围 子标签
            var tabAttackRange = new TabPage();
            tabAttackRange.Text = "攻击范围";
            tabAttackRange.Padding = new Padding(3);
            _betProcessTabControl.TabPages.Add(tabAttackRange);

            _betAttackRangeCtrl = new BetAttackRangeControl();
            _betAttackRangeCtrl.Location = new System.Drawing.Point(5, 5);
            _betAttackRangeCtrl.Dock = DockStyle.Fill;
            tabAttackRange.Controls.Add(_betAttackRangeCtrl);

            tabBetProcess.Controls.Add(_betProcessTabControl);

            // =====================================================
            // 黑名单/刷屏检测 Tab
            // =====================================================
            tabBlacklist.AutoScroll = true;
            _blacklistSpamCtrl = new BlacklistSpamSettingsControl();
            _blacklistSpamCtrl.Dock = DockStyle.Fill;
            tabBlacklist.Controls.Add(_blacklistSpamCtrl);
            
            // =====================================================
            // 名片设置 Tab
            // =====================================================
            tabCard.AutoScroll = true;
            _cardSettingsCtrl = new CardSettingsControl();
            _cardSettingsCtrl.Dock = DockStyle.Fill;
            tabCard.Controls.Add(_cardSettingsCtrl);
            
            // =====================================================
            // 托管设置 Tab
            // =====================================================
            tabTrustee.AutoScroll = true;
            _trusteeSettingsCtrl = new TrusteeSettingsControl();
            _trusteeSettingsCtrl.Dock = DockStyle.Fill;
            tabTrustee.Controls.Add(_trusteeSettingsCtrl);
            
            // =====================================================
            // 送分活动 Tab
            // =====================================================
            tabBonus.AutoScroll = true;
            _bonusSettingsCtrl = new BonusActivitySettingsControl();
            _bonusSettingsCtrl.Dock = DockStyle.Fill;
            tabBonus.Controls.Add(_bonusSettingsCtrl);
            
            // =====================================================
            // 托设置 Tab
            // =====================================================
            tabTrusteeSettings.AutoScroll = true;
            _shillSettingsCtrl = new ShillSettingsControl();
            _shillSettingsCtrl.Dock = DockStyle.Fill;
            tabTrusteeSettings.Controls.Add(_shillSettingsCtrl);
            
            // =====================================================
            // 其他设置 Tab
            // =====================================================
            tabOther.AutoScroll = true;
            _otherSettingsCtrl = new OtherSettingsControl();
            _otherSettingsCtrl.Dock = DockStyle.Fill;
            tabOther.Controls.Add(_otherSettingsCtrl);
            
            // =====================================================
            // 玩法赔率设置 Tab - 包含二级 TabControl
            // =====================================================
            _oddsTabControl = new TabControl();
            _oddsTabControl.Location = new System.Drawing.Point(0, 0);
            _oddsTabControl.Size = new System.Drawing.Size(670, 450);
            _oddsTabControl.Dock = DockStyle.Fill;
            
            // 经典玩法 子标签
            var tabClassicPlay = new TabPage();
            tabClassicPlay.Text = "经典玩法";
            tabClassicPlay.Padding = new Padding(3);
            tabClassicPlay.AutoScroll = true;
            _oddsTabControl.TabPages.Add(tabClassicPlay);
            
            _classicPlayCtrl = new ClassicPlaySettingsControl();
            _classicPlayCtrl.Location = new System.Drawing.Point(0, 0);
            _classicPlayCtrl.Dock = DockStyle.Fill;
            tabClassicPlay.Controls.Add(_classicPlayCtrl);
            
            // 尾球玩法 子标签
            var tabTailBall = new TabPage();
            tabTailBall.Text = "尾球玩法";
            tabTailBall.Padding = new Padding(3);
            _oddsTabControl.TabPages.Add(tabTailBall);
            _tailBallCtrl = new TailBallSettingsControl();
            _tailBallCtrl.Dock = DockStyle.Fill;
            tabTailBall.Controls.Add(_tailBallCtrl);
            
            // 龙虎玩法 子标签
            var tabDragonTiger = new TabPage();
            tabDragonTiger.Text = "龙虎玩法";
            tabDragonTiger.Padding = new Padding(3);
            _oddsTabControl.TabPages.Add(tabDragonTiger);
            _dragonTigerCtrl = new DragonTigerSettingsControl();
            _dragonTigerCtrl.Dock = DockStyle.Fill;
            tabDragonTiger.Controls.Add(_dragonTigerCtrl);
            
            // 三军玩法 子标签
            var tabThreeArmy = new TabPage();
            tabThreeArmy.Text = "三军玩法";
            tabThreeArmy.Padding = new Padding(3);
            _oddsTabControl.TabPages.Add(tabThreeArmy);
            _threeArmyCtrl = new ThreeArmySettingsControl();
            _threeArmyCtrl.Dock = DockStyle.Fill;
            tabThreeArmy.Controls.Add(_threeArmyCtrl);
            
            // 定位球玩法 子标签
            var tabPositionBall = new TabPage();
            tabPositionBall.Text = "定位球玩法";
            tabPositionBall.Padding = new Padding(3);
            _oddsTabControl.TabPages.Add(tabPositionBall);
            _positionBallCtrl = new PositionBallSettingsControl();
            _positionBallCtrl.Dock = DockStyle.Fill;
            tabPositionBall.Controls.Add(_positionBallCtrl);
            
            // 其他玩法 子标签
            var tabOtherPlay = new TabPage();
            tabOtherPlay.Text = "其他玩法";
            tabOtherPlay.Padding = new Padding(3);
            _oddsTabControl.TabPages.Add(tabOtherPlay);
            _otherPlayCtrl = new OtherPlaySettingsControl();
            _otherPlayCtrl.Dock = DockStyle.Fill;
            tabOtherPlay.Controls.Add(_otherPlayCtrl);
            
            tabOdds.Controls.Add(_oddsTabControl);
            
            // =====================================================
            // 自动回复 Tab - 包含二级 TabControl
            // =====================================================
            _autoReplyTabControl = new TabControl();
            _autoReplyTabControl.Location = new System.Drawing.Point(0, 0);
            _autoReplyTabControl.Size = new System.Drawing.Size(670, 450);
            _autoReplyTabControl.Dock = DockStyle.Fill;
            
            // 回复 子标签
            var tabReply = new TabPage();
            tabReply.Text = "回复";
            tabReply.Padding = new Padding(3);
            _autoReplyTabControl.TabPages.Add(tabReply);
            _replyCtrl = new AutoReplySettingsControl();
            _replyCtrl.Dock = DockStyle.Fill;
            tabReply.Controls.Add(_replyCtrl);
            
            // 内部回复 子标签
            var tabInternalReply = new TabPage();
            tabInternalReply.Text = "内部回复";
            tabInternalReply.Padding = new Padding(3);
            _autoReplyTabControl.TabPages.Add(tabInternalReply);
            _internalReplyCtrl = new Controls.AutoReply.InternalReplySettingsControl();
            _internalReplyCtrl.Dock = DockStyle.Fill;
            tabInternalReply.Controls.Add(_internalReplyCtrl);
            
            // 变量说明 子标签
            var tabVariableHelp = new TabPage();
            tabVariableHelp.Text = "变量说明";
            tabVariableHelp.Padding = new Padding(3);
            _autoReplyTabControl.TabPages.Add(tabVariableHelp);
            _variableHelpCtrl = new Controls.AutoReply.VariableHelpControl();
            _variableHelpCtrl.Dock = DockStyle.Fill;
            tabVariableHelp.Controls.Add(_variableHelpCtrl);
            
            tabAutoReply.Controls.Add(_autoReplyTabControl);
            
            // =====================================================
            // 封盘设置 Tab - 初始化各游戏类型的封盘设置面板
            // =====================================================
            _sealPanelPC = new Controls.SealSettings.SealTabPanel("PC");
            _sealPanelPC.Dock = DockStyle.Fill;
            tabSealPC.Controls.Add(_sealPanelPC);
            
            _sealPanelCanada = new Controls.SealSettings.SealTabPanel("加拿大");
            _sealPanelCanada.Dock = DockStyle.Fill;
            tabSealCanada.Controls.Add(_sealPanelCanada);
            
            _sealPanelBitcoin = new Controls.SealSettings.SealTabPanel("比特");
            _sealPanelBitcoin.Dock = DockStyle.Fill;
            tabSealBitcoin.Controls.Add(_sealPanelBitcoin);
            
            _sealPanelBeijing = new Controls.SealSettings.SealTabPanel("北京");
            _sealPanelBeijing.Dock = DockStyle.Fill;
            tabSealBeijing.Controls.Add(_sealPanelBeijing);
        }

        private void AddPlaceholderToTab(TabPage tab, string text)
        {
            var lbl = new Label
            {
                Text = text,
                AutoSize = false,
                TextAlign = System.Drawing.ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill,
                Font = new System.Drawing.Font("Microsoft YaHei UI", 12F)
            };
            tab.Controls.Add(lbl);
        }

        private void LoadConfig()
        {
            var config = ConfigService.Instance.Config;
            
            // 加载设置控件的值
            _billSendCtrl?.LoadSettings();
            _billFormatCtrl?.LoadSettings();
            _groupTaskCtrl?.LoadSettings();
            _basicSettingsCtrl?.LoadSettings();
            _muteSettingsCtrl?.LoadSettings();
            _feedbackSettingsCtrl?.LoadSettings();
        }

    }
}
