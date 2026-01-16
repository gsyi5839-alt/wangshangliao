-- 006_betting_system.sql
-- 下注系统数据库迁移 - 基于招财狗(ZCG)数据结构
-- Betting system migration based on ZCG data structure

-- 玩家余额表
CREATE TABLE IF NOT EXISTS player_balances (
    id INT AUTO_INCREMENT PRIMARY KEY,
    player_id VARCHAR(64) NOT NULL UNIQUE COMMENT 'NIM账号',
    player_nick VARCHAR(128) COMMENT '玩家昵称',
    balance DECIMAL(15,2) NOT NULL DEFAULT 0 COMMENT '当前余额',
    total_deposit DECIMAL(15,2) NOT NULL DEFAULT 0 COMMENT '总上分',
    total_withdraw DECIMAL(15,2) NOT NULL DEFAULT 0 COMMENT '总下分',
    total_bet DECIMAL(15,2) NOT NULL DEFAULT 0 COMMENT '总下注',
    total_win DECIMAL(15,2) NOT NULL DEFAULT 0 COMMENT '总中奖',
    total_rebate DECIMAL(15,2) NOT NULL DEFAULT 0 COMMENT '总回水',
    bet_count INT NOT NULL DEFAULT 0 COMMENT '下注次数',
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    INDEX idx_balance (balance),
    INDEX idx_player_nick (player_nick)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='玩家余额表';

-- 积分交易记录表
CREATE TABLE IF NOT EXISTS score_transactions (
    id INT AUTO_INCREMENT PRIMARY KEY,
    transaction_id VARCHAR(64) NOT NULL UNIQUE COMMENT '交易ID',
    player_id VARCHAR(64) NOT NULL COMMENT '玩家ID',
    player_nick VARCHAR(128) COMMENT '玩家昵称',
    type ENUM('deposit', 'withdraw', 'bet', 'win', 'rebate', 'bonus', 'adjust') NOT NULL COMMENT '交易类型',
    amount DECIMAL(15,2) NOT NULL COMMENT '交易金额',
    balance_before DECIMAL(15,2) NOT NULL COMMENT '交易前余额',
    balance_after DECIMAL(15,2) NOT NULL COMMENT '交易后余额',
    reason VARCHAR(256) COMMENT '交易原因',
    operator_id VARCHAR(64) COMMENT '操作员ID',
    period VARCHAR(32) COMMENT '期号(下注相关)',
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    INDEX idx_player (player_id),
    INDEX idx_type (type),
    INDEX idx_created (created_at),
    INDEX idx_period (period)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='积分交易记录';

-- 下注记录表
CREATE TABLE IF NOT EXISTS bet_records (
    id INT AUTO_INCREMENT PRIMARY KEY,
    bet_id VARCHAR(64) NOT NULL UNIQUE COMMENT '下注ID',
    period VARCHAR(32) NOT NULL COMMENT '期号',
    team_id VARCHAR(64) NOT NULL COMMENT '群ID',
    player_id VARCHAR(64) NOT NULL COMMENT '玩家ID',
    player_nick VARCHAR(128) COMMENT '玩家昵称',
    raw_text TEXT COMMENT '原始下注消息',
    normalized_text TEXT COMMENT '标准化下注内容',
    total_amount DECIMAL(15,2) NOT NULL COMMENT '总下注金额',
    balance_before DECIMAL(15,2) COMMENT '下注前余额',
    status ENUM('pending', 'settled', 'cancelled') DEFAULT 'pending' COMMENT '状态',
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    settled_at TIMESTAMP NULL COMMENT '结算时间',
    INDEX idx_period (period),
    INDEX idx_team (team_id),
    INDEX idx_player (player_id),
    INDEX idx_status (status),
    INDEX idx_created (created_at)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='下注记录';

-- 下注项明细表
CREATE TABLE IF NOT EXISTS bet_items (
    id INT AUTO_INCREMENT PRIMARY KEY,
    bet_id VARCHAR(64) NOT NULL COMMENT '关联的下注记录ID',
    kind VARCHAR(32) NOT NULL COMMENT '下注类型: dxds, digit, pair, straight, etc.',
    code VARCHAR(16) NOT NULL COMMENT '下注代码: DD, XS, 13, DZ, etc.',
    amount DECIMAL(15,2) NOT NULL COMMENT '下注金额',
    odds DECIMAL(8,4) COMMENT '赔率',
    is_win BOOLEAN DEFAULT NULL COMMENT '是否中奖',
    profit DECIMAL(15,2) COMMENT '盈亏',
    INDEX idx_bet (bet_id),
    INDEX idx_kind (kind),
    FOREIGN KEY (bet_id) REFERENCES bet_records(bet_id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='下注项明细';

-- 开奖记录表
CREATE TABLE IF NOT EXISTS lottery_results (
    id INT AUTO_INCREMENT PRIMARY KEY,
    period VARCHAR(32) NOT NULL UNIQUE COMMENT '期号',
    lottery_type VARCHAR(32) NOT NULL COMMENT '彩种: canada28, pc28, etc.',
    dice1 INT NOT NULL COMMENT '第一个骰子',
    dice2 INT NOT NULL COMMENT '第二个骰子',
    dice3 INT NOT NULL COMMENT '第三个骰子',
    sum_value INT NOT NULL COMMENT '和值',
    is_big BOOLEAN NOT NULL COMMENT '是否大',
    is_odd BOOLEAN NOT NULL COMMENT '是否单',
    special_type VARCHAR(32) COMMENT '特殊类型: pair, straight, leopard',
    dragon_tiger VARCHAR(16) COMMENT '龙虎结果',
    raw_data TEXT COMMENT '原始开奖数据',
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    INDEX idx_lottery_type (lottery_type),
    INDEX idx_created (created_at)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='开奖记录';

-- 账单记录表
CREATE TABLE IF NOT EXISTS bill_records (
    id INT AUTO_INCREMENT PRIMARY KEY,
    period VARCHAR(32) NOT NULL COMMENT '期号',
    team_id VARCHAR(64) NOT NULL COMMENT '群ID',
    player_count INT NOT NULL DEFAULT 0 COMMENT '下注人数',
    total_bet DECIMAL(15,2) NOT NULL DEFAULT 0 COMMENT '总下注',
    total_win DECIMAL(15,2) NOT NULL DEFAULT 0 COMMENT '总中奖',
    house_profit DECIMAL(15,2) NOT NULL DEFAULT 0 COMMENT '庄家盈利',
    lottery_result VARCHAR(64) COMMENT '开奖结果',
    bill_content TEXT COMMENT '账单内容',
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    INDEX idx_period (period),
    INDEX idx_team (team_id),
    INDEX idx_created (created_at)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='账单记录';

-- 玩家账单明细
CREATE TABLE IF NOT EXISTS player_bills (
    id INT AUTO_INCREMENT PRIMARY KEY,
    period VARCHAR(32) NOT NULL COMMENT '期号',
    team_id VARCHAR(64) NOT NULL COMMENT '群ID',
    player_id VARCHAR(64) NOT NULL COMMENT '玩家ID',
    player_nick VARCHAR(128) COMMENT '玩家昵称',
    total_bet DECIMAL(15,2) NOT NULL DEFAULT 0 COMMENT '下注金额',
    profit DECIMAL(15,2) NOT NULL DEFAULT 0 COMMENT '盈亏',
    balance_before DECIMAL(15,2) COMMENT '结算前余额',
    balance_after DECIMAL(15,2) COMMENT '结算后余额',
    detail TEXT COMMENT '下注明细',
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    INDEX idx_period (period),
    INDEX idx_player (player_id),
    INDEX idx_team (team_id),
    UNIQUE KEY uk_period_player (period, player_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='玩家账单明细';

-- 回水记录表
CREATE TABLE IF NOT EXISTS rebate_records (
    id INT AUTO_INCREMENT PRIMARY KEY,
    player_id VARCHAR(64) NOT NULL COMMENT '玩家ID',
    player_nick VARCHAR(128) COMMENT '玩家昵称',
    rebate_date DATE NOT NULL COMMENT '回水日期',
    total_bet DECIMAL(15,2) NOT NULL DEFAULT 0 COMMENT '总下注',
    bet_count INT NOT NULL DEFAULT 0 COMMENT '下注次数',
    total_loss DECIMAL(15,2) NOT NULL DEFAULT 0 COMMENT '总输分',
    rebate_percent DECIMAL(5,2) NOT NULL DEFAULT 0 COMMENT '回水比例',
    rebate_amount DECIMAL(15,2) NOT NULL DEFAULT 0 COMMENT '回水金额',
    status ENUM('pending', 'paid', 'cancelled') DEFAULT 'pending' COMMENT '状态',
    paid_at TIMESTAMP NULL COMMENT '发放时间',
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    INDEX idx_player (player_id),
    INDEX idx_date (rebate_date),
    INDEX idx_status (status),
    UNIQUE KEY uk_player_date (player_id, rebate_date)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='回水记录';

-- 赔率配置表
CREATE TABLE IF NOT EXISTS odds_config (
    id INT AUTO_INCREMENT PRIMARY KEY,
    config_key VARCHAR(64) NOT NULL UNIQUE COMMENT '配置键',
    config_value VARCHAR(256) NOT NULL COMMENT '配置值',
    description VARCHAR(256) COMMENT '描述',
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='赔率配置';

-- 插入默认赔率配置
INSERT INTO odds_config (config_key, config_value, description) VALUES
('single_bet_odds', '1.8', '大小单双赔率'),
('combination_odds', '3.8', '组合赔率(大单/大双/小单/小双)'),
('big_odd_small_even_odds', '5', '大单小双赔率'),
('big_even_small_odd_odds', '5', '大双小单赔率'),
('extreme_odds', '11', '极大极小赔率'),
('pair_odds', '2', '对子赔率'),
('straight_odds', '11', '顺子赔率'),
('half_straight_odds', '1.7', '半顺赔率'),
('leopard_odds', '59', '豹子赔率'),
('mixed_odds', '2.2', '杂赔率'),
('dragon_tiger_odds', '1.92', '龙虎赔率'),
('digit_0_odds', '665', '数字0赔率'),
('digit_13_odds', '10', '数字13赔率'),
('digit_14_odds', '10', '数字14赔率'),
('digit_27_odds', '665', '数字27赔率'),
('single_min_bet', '20', '单注下限'),
('single_max_bet', '50000', '单注上限'),
('total_max_bet', '60000', '总额上限')
ON DUPLICATE KEY UPDATE config_value = VALUES(config_value);

-- 消息模板表
CREATE TABLE IF NOT EXISTS message_templates (
    id INT AUTO_INCREMENT PRIMARY KEY,
    template_key VARCHAR(64) NOT NULL UNIQUE COMMENT '模板键',
    template_content TEXT NOT NULL COMMENT '模板内容',
    description VARCHAR(256) COMMENT '描述',
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='消息模板';

-- 插入默认消息模板
INSERT INTO message_templates (template_key, template_content, description) VALUES
('bet_success', '[艾特]([旺旺])\n本次攻擊:[玩家攻击],余粮:[余粮]', '下注成功'),
('balance_insufficient', '[艾特]([旺旺])\n余粮不足，请先上分！当前余额:[余粮]', '余额不足'),
('deposit_success', '[艾特] [分数]到\n粮库:[余粮]\n您的分已为您上到游戏中，祝您大吉大利！', '上分成功'),
('withdraw_success', '[艾特] [分数]查\n粮库:[留分]\n老板请查收及核实，已转账到您的账号！', '下分成功'),
('lottery_result', '开:[一区]+[二区]+[三区]=[开奖号码] [大小单双] 第[期数]期', '开奖结果'),
('bill_summary', '開:[一区]+[二区]+[三区]=[开奖号码] [大小单双]\n人數:[客户人数] 總分:[总分数]', '账单汇总'),
('rebate_success', '[艾特]([旺旺])\n本次回水[分数],余粮：[余粮]', '回水成功')
ON DUPLICATE KEY UPDATE template_content = VALUES(template_content);
