-- Add platform-specific download URLs to app_versions table
-- Windows and macOS download links for each version

ALTER TABLE app_versions
  ADD COLUMN download_url_windows VARCHAR(512) NULL AFTER content,
  ADD COLUMN download_url_macos VARCHAR(512) NULL AFTER download_url_windows;

-- Add download_count to settings if not exists
INSERT INTO settings (k, v) VALUES ('download_count', '0') 
  ON DUPLICATE KEY UPDATE k = k;

