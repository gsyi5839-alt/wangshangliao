-- Lottery API management table
-- Stores multiple lottery API endpoints for different lottery types

CREATE TABLE IF NOT EXISTS lottery_apis (
  id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
  name VARCHAR(64) NOT NULL COMMENT 'Display name, e.g., 加拿大28',
  code VARCHAR(32) NOT NULL COMMENT 'Lottery code, e.g., jnd28',
  token VARCHAR(128) NOT NULL COMMENT 'API token from bcapi.cn',
  api_url VARCHAR(512) NOT NULL COMMENT 'Primary API URL',
  backup_url VARCHAR(512) NULL COMMENT 'Backup API URL',
  format_type VARCHAR(16) NOT NULL DEFAULT 'json' COMMENT 'Response format: json/jsonp/xml',
  callback_name VARCHAR(64) NULL DEFAULT 'jsonpReturn' COMMENT 'JSONP callback name',
  rows_count INT NOT NULL DEFAULT 1 COMMENT 'Number of rows to fetch (1-20)',
  request_interval INT NOT NULL DEFAULT 1000 COMMENT 'Request interval in milliseconds',
  max_requests_per_30s INT NOT NULL DEFAULT 40 COMMENT 'Max requests per 30 seconds',
  is_enabled TINYINT(1) NOT NULL DEFAULT 1 COMMENT 'Whether this API is enabled',
  remark TEXT NULL COMMENT 'Additional notes',
  created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
  updated_at TIMESTAMP NULL DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (id),
  UNIQUE KEY uq_lottery_apis_code (code),
  KEY idx_lottery_apis_enabled (is_enabled)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- Insert default Canada 28 API
-- 官方限制：30秒内超过40次会被封禁1天，单次请求间隔需>=1秒
INSERT INTO lottery_apis (name, code, token, api_url, backup_url, format_type, rows_count, request_interval, max_requests_per_30s, remark)
VALUES (
  '加拿大28',
  'jnd28',
  '314e1914d84711f091245baa515fd558',
  'https://bcapi.cn/token/{token}/code/{code}/rows/{rows}.{format}',
  'https://vip.bcapi.cn:2096/token/{token}/code/{code}/rows/{rows}.{format}',
  'json',
  1,
  1000,
  40,
  '菠菜接口网 - 加拿大28开奖数据接口。官方限制：30秒40次，间隔>=1秒'
);

