-- Core schema for admin console + agent (desktop bot) integration.
-- Keep this file idempotent with IF NOT EXISTS where possible.

CREATE TABLE IF NOT EXISTS schema_migrations (
  id INT UNSIGNED NOT NULL AUTO_INCREMENT,
  filename VARCHAR(255) NOT NULL,
  applied_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (id),
  UNIQUE KEY uq_schema_migrations_filename (filename)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS admins (
  id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
  username VARCHAR(64) NOT NULL,
  password_hash VARCHAR(255) NOT NULL,
  created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
  updated_at TIMESTAMP NULL DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (id),
  UNIQUE KEY uq_admins_username (username)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS agents (
  id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
  name VARCHAR(128) NOT NULL,
  agent_key VARCHAR(128) NOT NULL,
  description VARCHAR(255) NULL,
  last_seen_at TIMESTAMP NULL DEFAULT NULL,
  last_ip VARCHAR(64) NULL,
  status VARCHAR(32) NOT NULL DEFAULT 'offline',
  created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
  updated_at TIMESTAMP NULL DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (id),
  UNIQUE KEY uq_agents_agent_key (agent_key)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS groups (
  id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
  agent_id BIGINT UNSIGNED NOT NULL,
  group_cloud_id VARCHAR(64) NOT NULL,
  name VARCHAR(128) NULL,
  is_bound TINYINT(1) NOT NULL DEFAULT 0,
  created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
  updated_at TIMESTAMP NULL DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (id),
  UNIQUE KEY uq_groups_agent_group (agent_id, group_cloud_id),
  CONSTRAINT fk_groups_agent_id FOREIGN KEY (agent_id) REFERENCES agents(id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS auto_reply_rules (
  id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
  agent_id BIGINT UNSIGNED NOT NULL,
  group_cloud_id VARCHAR(64) NULL,
  enabled TINYINT(1) NOT NULL DEFAULT 1,
  match_type VARCHAR(16) NOT NULL DEFAULT 'contains', -- contains | regex | equals
  match_text VARCHAR(255) NOT NULL,
  reply_text TEXT NOT NULL,
  priority INT NOT NULL DEFAULT 100,
  created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
  updated_at TIMESTAMP NULL DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (id),
  KEY idx_rules_agent_group (agent_id, group_cloud_id),
  CONSTRAINT fk_rules_agent_id FOREIGN KEY (agent_id) REFERENCES agents(id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS broadcast_tasks (
  id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
  agent_id BIGINT UNSIGNED NOT NULL,
  group_cloud_id VARCHAR(64) NULL,
  scene VARCHAR(16) NOT NULL DEFAULT 'team', -- team | p2p
  to_id VARCHAR(64) NULL,
  text TEXT NOT NULL,
  status VARCHAR(16) NOT NULL DEFAULT 'pending', -- pending | running | done | failed | cancelled
  scheduled_at TIMESTAMP NULL DEFAULT NULL,
  started_at TIMESTAMP NULL DEFAULT NULL,
  finished_at TIMESTAMP NULL DEFAULT NULL,
  result_json JSON NULL,
  created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
  updated_at TIMESTAMP NULL DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (id),
  KEY idx_tasks_agent_status (agent_id, status),
  CONSTRAINT fk_tasks_agent_id FOREIGN KEY (agent_id) REFERENCES agents(id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS agent_logs (
  id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
  agent_id BIGINT UNSIGNED NOT NULL,
  level VARCHAR(16) NOT NULL DEFAULT 'info', -- debug | info | warn | error
  message VARCHAR(255) NOT NULL,
  meta_json JSON NULL,
  created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (id),
  KEY idx_logs_agent_time (agent_id, created_at),
  CONSTRAINT fk_logs_agent_id FOREIGN KEY (agent_id) REFERENCES agents(id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;


