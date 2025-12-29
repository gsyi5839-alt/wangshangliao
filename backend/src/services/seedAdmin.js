const crypto = require('crypto');
const { pool } = require('../db/pool');
const { hashPassword } = require('../auth/password');
const { ADMIN_BOOTSTRAP_USER, ADMIN_BOOTSTRAP_PASS } = require('../config/env');

/**
 * Ensure there is at least one admin in database.
 * If none exists, create bootstrap admin.
 *
 * Security note:
 * - Prefer setting ADMIN_BOOTSTRAP_PASS in environment.
 * - If missing, we generate a random password and print it once on startup.
 */
async function ensureBootstrapAdmin() {
  const [[row]] = await pool.query('SELECT COUNT(*) AS cnt FROM admins');
  if (Number(row.cnt) > 0) return;

  const username = ADMIN_BOOTSTRAP_USER || 'admin';
  const plain = ADMIN_BOOTSTRAP_PASS || crypto.randomBytes(12).toString('base64url');
  const passwordHash = await hashPassword(plain);

  await pool.query('INSERT INTO admins (username, password_hash) VALUES (?, ?)', [
    username,
    passwordHash
  ]);

  // eslint-disable-next-line no-console
  console.log('[bootstrap-admin] created admin:', { username, password: plain });
}

module.exports = { ensureBootstrapAdmin };


