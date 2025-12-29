const crypto = require('crypto');
const express = require('express');
const { z } = require('zod');
const { pool } = require('../db/pool');
const { adminAuth } = require('../middlewares/adminAuth');
const { asyncHandler } = require('../utils/asyncHandler');
const { HttpError } = require('../utils/httpError');
const { getSetting, setSetting } = require('../services/settingsService');
const { hashPassword } = require('../auth/password');

const router = express.Router();

const CARD_CODE_RE = /^[A-Za-z0-9]{26}$/;
const ALLOWED_DAYS = new Set([1, 3, 7, 15, 30]);

/**
 * Generate a fixed-length alphanumeric string (A-Z a-z 0-9).
 * @param {number} len
 * @returns {string}
 */
function genAlphaNum(len) {
  const chars = 'ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789';
  const bytes = crypto.randomBytes(len);
  let out = '';
  for (let i = 0; i < len; i++) out += chars[bytes[i] % chars.length];
  return out;
}

/**
 * Admin: create recharge cards in batch.
 */
router.post(
  '/admin/recharge-cards',
  adminAuth,
  asyncHandler(async (req, res) => {
    const schema = z.object({
      count: z.number().int().min(1).max(500).default(10),
      days: z.number().int().default(30)
    });
    const body = schema.parse(req.body || {});

    if (!ALLOWED_DAYS.has(body.days)) throw new HttpError(400, 'Invalid days');

    const created = [];
    for (let i = 0; i < body.count; i++) {
      // Card code: 26 chars (A-Z a-z 0-9)
      // Card password: 18 chars (A-Z a-z 0-9), store bcrypt hash only.
      const code = genAlphaNum(26);
      if (!CARD_CODE_RE.test(code)) throw new Error('card code generation failed');
      const password = genAlphaNum(18);
      // eslint-disable-next-line no-await-in-loop
      const passwordHash = await hashPassword(password);
      // eslint-disable-next-line no-await-in-loop
      await pool.query('INSERT INTO recharge_cards (card_code, card_password_hash, days) VALUES (?, ?, ?)', [
        code,
        passwordHash,
        body.days
      ]);
      created.push({ code, password, days: body.days });
    }

    res.json({ ok: true, data: created });
  })
);

/**
 * Admin: list recharge cards (latest first).
 */
router.get(
  '/admin/recharge-cards',
  adminAuth,
  asyncHandler(async (req, res) => {
    const limit = Math.min(Math.max(Number(req.query.limit || 200), 1), 1000);
    const [rows] = await pool.query(
      `SELECT c.id, c.card_code, c.days, c.used_at, u.username AS used_by
       FROM recharge_cards c
       LEFT JOIN users u ON u.id = c.used_by_user_id
       ORDER BY c.id DESC
       LIMIT ?`,
      [limit]
    );
    res.json({ ok: true, data: rows });
  })
);

/**
 * Admin: create or update announcement.
 */
router.post(
  '/admin/announcements',
  adminAuth,
  asyncHandler(async (req, res) => {
    const schema = z.object({
      title: z.string().min(1).max(128),
      content: z.string().min(1),
      startsAt: z.string().datetime().optional().nullable(),
      endsAt: z.string().datetime().optional().nullable(),
      isEnabled: z.boolean().default(true)
    });
    const body = schema.parse(req.body);

    await pool.query(
      'INSERT INTO announcements (title, content, starts_at, ends_at, is_enabled) VALUES (?, ?, ?, ?, ?)',
      [
        body.title,
        body.content,
        body.startsAt ? new Date(body.startsAt) : null,
        body.endsAt ? new Date(body.endsAt) : null,
        body.isEnabled ? 1 : 0
      ]
    );

    const [[row]] = await pool.query('SELECT * FROM announcements ORDER BY id DESC LIMIT 1');
    res.json({ ok: true, data: row });
  })
);

/**
 * Admin: list announcements.
 */
router.get(
  '/admin/announcements',
  adminAuth,
  asyncHandler(async (req, res) => {
    const limit = Math.min(Math.max(Number(req.query.limit || 50), 1), 200);
    const [rows] = await pool.query('SELECT * FROM announcements ORDER BY id DESC LIMIT ?', [limit]);
    res.json({ ok: true, data: rows });
  })
);

/**
 * Admin: list versions (latest first).
 */
router.get(
  '/admin/versions',
  adminAuth,
  asyncHandler(async (req, res) => {
    const limit = Math.min(Math.max(Number(req.query.limit || 50), 1), 200);
    const [rows] = await pool.query(
      'SELECT id, version, content, download_url_windows, download_url_macos, created_at FROM app_versions ORDER BY id DESC LIMIT ?',
      [limit]
    );
    res.json({ ok: true, data: rows });
  })
);

/**
 * Admin: create a version entry.
 */
router.post(
  '/admin/versions',
  adminAuth,
  asyncHandler(async (req, res) => {
    const schema = z.object({
      version: z.string().min(1).max(32),
      content: z.string().min(1),
      downloadUrlWindows: z.string().url().optional().nullable(),
      downloadUrlMacos: z.string().url().optional().nullable()
    });
    const body = schema.parse(req.body);

    await pool.query(
      'INSERT INTO app_versions (version, content, download_url_windows, download_url_macos) VALUES (?, ?, ?, ?)',
      [body.version, body.content, body.downloadUrlWindows || null, body.downloadUrlMacos || null]
    );
    const [[row]] = await pool.query('SELECT * FROM app_versions ORDER BY id DESC LIMIT 1');
    res.json({ ok: true, data: row });
  })
);

/**
 * Admin: update a version entry.
 */
router.put(
  '/admin/versions/:id',
  adminAuth,
  asyncHandler(async (req, res) => {
    const id = Number(req.params.id);
    if (!id || id < 1) throw new HttpError(400, 'Invalid version id');

    const schema = z.object({
      version: z.string().min(1).max(32).optional(),
      content: z.string().min(1).optional(),
      downloadUrlWindows: z.string().url().optional().nullable(),
      downloadUrlMacos: z.string().url().optional().nullable()
    });
    const body = schema.parse(req.body);

    const updates = [];
    const params = [];

    if (body.version !== undefined) {
      updates.push('version = ?');
      params.push(body.version);
    }
    if (body.content !== undefined) {
      updates.push('content = ?');
      params.push(body.content);
    }
    if (body.downloadUrlWindows !== undefined) {
      updates.push('download_url_windows = ?');
      params.push(body.downloadUrlWindows);
    }
    if (body.downloadUrlMacos !== undefined) {
      updates.push('download_url_macos = ?');
      params.push(body.downloadUrlMacos);
    }

    if (updates.length === 0) throw new HttpError(400, 'No fields to update');

    params.push(id);
    await pool.query(`UPDATE app_versions SET ${updates.join(', ')} WHERE id = ?`, params);
    const [[row]] = await pool.query('SELECT * FROM app_versions WHERE id = ?', [id]);
    res.json({ ok: true, data: row });
  })
);

/**
 * Admin: set a setting value (k/v).
 */
router.post(
  '/admin/settings',
  adminAuth,
  asyncHandler(async (req, res) => {
    const schema = z.object({
      key: z.string().min(1).max(128),
      value: z.string()
    });
    const body = schema.parse(req.body);

    // Minimal validation for known keys (avoid garbage keys).
    const allow = new Set([
      'framework_download_url',
      'machine_pack_download_url',
      'lottery_api_url',
      'lottery_api_token',
      'download_count'
    ]);
    if (!allow.has(body.key)) throw new HttpError(400, 'Unsupported setting key');

    await setSetting(body.key, body.value);
    res.json({ ok: true });
  })
);

/**
 * Admin: read current settings (allowlist).
 */
router.get(
  '/admin/settings',
  adminAuth,
  asyncHandler(async (req, res) => {
    // eslint-disable-next-line no-unused-vars
    void req;
    const keys = [
      'framework_download_url',
      'machine_pack_download_url',
      'lottery_api_url',
      'lottery_api_token',
      'download_count'
    ];
    const data = {};
    for (const k of keys) {
      // eslint-disable-next-line no-await-in-loop
      data[k] = await getSetting(k);
    }
    res.json({ ok: true, data });
  })
);

module.exports = { router };


