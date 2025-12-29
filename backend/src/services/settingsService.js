const { pool } = require('../db/pool');

/**
 * Get a setting value by key.
 * @param {string} key
 * @returns {Promise<string|null>}
 */
async function getSetting(key) {
  const [rows] = await pool.query('SELECT v FROM settings WHERE k=?', [key]);
  if (!rows.length) return null;
  return String(rows[0].v);
}

/**
 * Upsert a setting value by key.
 * @param {string} key
 * @param {string} value
 * @returns {Promise<void>}
 */
async function setSetting(key, value) {
  await pool.query('INSERT INTO settings (k, v) VALUES (?, ?) ON DUPLICATE KEY UPDATE v=VALUES(v)', [
    key,
    value
  ]);
}

module.exports = { getSetting, setSetting };


