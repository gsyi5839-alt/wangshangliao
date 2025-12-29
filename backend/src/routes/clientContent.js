const express = require('express');
const { z } = require('zod');
const { pool } = require('../db/pool');
const { asyncHandler } = require('../utils/asyncHandler');
const { userAuth } = require('../middlewares/userAuth');
const { getSetting } = require('../services/settingsService');

const router = express.Router();

/**
 * Client: current user profile (requires user token).
 */
router.get(
  '/client/me',
  userAuth,
  asyncHandler(async (req, res) => {
    const [rows] = await pool.query(
      'SELECT id, username, bound_info, promoter_username, expire_at, created_at, updated_at FROM users WHERE id=?',
      [req.user.id]
    );
    if (!rows.length) return res.status(404).json({ ok: false, error: { message: 'Not found' } });
    res.json({ ok: true, data: rows[0] });
  })
);

/**
 * Client: latest announcement (public; desktop shows it on login).
 */
router.get(
  '/client/announcement',
  asyncHandler(async (req, res) => {
    const now = new Date();
    const [rows] = await pool.query(
      `SELECT id, title, content, starts_at, ends_at, created_at
       FROM announcements
       WHERE is_enabled=1
         AND (starts_at IS NULL OR starts_at <= ?)
         AND (ends_at IS NULL OR ends_at >= ?)
       ORDER BY id DESC
       LIMIT 1`,
      [now, now]
    );
    res.json({ ok: true, data: rows[0] || null });
  })
);

/**
 * Client: version list (public).
 */
router.get(
  '/client/versions',
  asyncHandler(async (req, res) => {
    const limit = Math.min(Math.max(Number(req.query.limit || 50), 1), 200);
    const [rows] = await pool.query(
      'SELECT version, content, download_url_windows, download_url_macos, created_at FROM app_versions ORDER BY id DESC LIMIT ?',
      [limit]
    );
    res.json({ success: true, data: rows });
  })
);

/**
 * Client: settings snapshot (public; desktop can fetch links/tokens here).
 * Only returns a safe allowlist of keys.
 */
router.get(
  '/client/settings',
  asyncHandler(async (req, res) => {
    const schema = z.object({
      keys: z
        .string()
        .optional()
        .transform((v) => (v ? v.split(',').map((s) => s.trim()).filter(Boolean) : []))
    });
    const q = schema.parse(req.query || {});

    // Public settings should NOT include sensitive values.
    const allow = new Set(['framework_download_url', 'machine_pack_download_url', 'lottery_api_url', 'download_count']);

    const keys = (q.keys.length ? q.keys : Array.from(allow)).filter((k) => allow.has(k));

    const data = {};
    for (const k of keys) {
      // eslint-disable-next-line no-await-in-loop
      data[k] = await getSetting(k);
    }

    res.json({ success: true, data });
  })
);

/**
 * Client: settings snapshot (private; requires login).
 * This endpoint may include sensitive values (e.g., lottery token).
 */
router.get(
  '/client/settings-private',
  userAuth,
  asyncHandler(async (req, res) => {
    // eslint-disable-next-line no-unused-vars
    void req;
    const keys = ['framework_download_url', 'machine_pack_download_url', 'lottery_api_url', 'lottery_api_token'];
    const data = {};
    for (const k of keys) {
      // eslint-disable-next-line no-await-in-loop
      data[k] = await getSetting(k);
    }
    res.json({ ok: true, data });
  })
);

module.exports = { router };


