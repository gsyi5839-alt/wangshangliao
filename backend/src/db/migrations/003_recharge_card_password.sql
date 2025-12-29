-- Upgrade recharge card format:
-- - Card code: 26 chars (A-Z a-z 0-9)
-- - Card password: 18 chars (A-Z a-z 0-9), stored as bcrypt hash
-- - Days must be one of: 1, 3, 7, 15, 30 (enforced in application layer)

ALTER TABLE recharge_cards
  ADD COLUMN card_password_hash VARCHAR(255) NULL AFTER card_code;


