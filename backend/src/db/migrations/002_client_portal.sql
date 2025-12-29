-- Client portal schema for desktop bot:
-- - User login/register/recharge/change password
-- - Announcement and version changelog
-- - Key/value settings (e.g., lottery API token, download links)

CREATE TABLE IF NOT EXISTS users (
  id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
  username VARCHAR(64) NOT NULL,
  password_hash VARCHAR(255) NOT NULL,
  super_password_hash VARCHAR(255) NOT NULL,
  bound_info VARCHAR(255) NULL,
  promoter_username VARCHAR(64) NULL,
  expire_at DATETIME NULL,
  created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
  updated_at TIMESTAMP NULL DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (id),
  UNIQUE KEY uq_users_username (username)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS recharge_cards (
  id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
  card_code VARCHAR(64) NOT NULL,
  days INT NOT NULL DEFAULT 30,
  used_by_user_id BIGINT UNSIGNED NULL,
  used_at DATETIME NULL,
  created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (id),
  UNIQUE KEY uq_recharge_cards_code (card_code),
  KEY idx_recharge_cards_used (used_by_user_id, used_at),
  CONSTRAINT fk_recharge_cards_user_id FOREIGN KEY (used_by_user_id) REFERENCES users(id) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS announcements (
  id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
  title VARCHAR(128) NOT NULL,
  content TEXT NOT NULL,
  starts_at DATETIME NULL,
  ends_at DATETIME NULL,
  is_enabled TINYINT(1) NOT NULL DEFAULT 1,
  created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
  updated_at TIMESTAMP NULL DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (id),
  KEY idx_announce_enabled_time (is_enabled, starts_at, ends_at)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS app_versions (
  id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
  version VARCHAR(32) NOT NULL,
  content TEXT NOT NULL,
  created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (id),
  UNIQUE KEY uq_app_versions_version (version)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS settings (
  id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
  k VARCHAR(128) NOT NULL,
  v TEXT NOT NULL,
  updated_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (id),
  UNIQUE KEY uq_settings_k (k)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;


