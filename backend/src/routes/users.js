const express = require('express');
const { z } = require('zod');
const { pool } = require('../db/pool');
const { adminAuth } = require('../middlewares/adminAuth');
const { asyncHandler } = require('../utils/asyncHandler');
const { HttpError } = require('../utils/httpError');

const router = express.Router();

/**
 * Admin: list users with expire info.
 */
router.get(
  '/admin/users',
  adminAuth,
  asyncHandler(async (req, res) => {
    const limit = Math.min(Math.max(Number(req.query.limit || 200), 1), 1000);
    const [rows] = await pool.query(
      'SELECT id, username, bound_info, promoter_username, expire_at, created_at, updated_at FROM users ORDER BY id DESC LIMIT ?',
      [limit]
    );
    res.json({ ok: true, data: rows });
  })
);

/**
 * Admin: extend a user expiration by N days.
 * Body: { days: number }
 */
router.post(
  '/admin/users/:id/extend',
  adminAuth,
  asyncHandler(async (req, res) => {
    const id = Number(req.params.id);
    if (!Number.isFinite(id) || id <= 0) throw new HttpError(400, 'Invalid id');

    const schema = z.object({ days: z.number().int().min(1).max(3650) });
    const body = schema.parse(req.body);

    await pool.query(
      'UPDATE users SET expire_at = DATE_ADD(GREATEST(IFNULL(expire_at, NOW()), NOW()), INTERVAL ? DAY) WHERE id=?',
      [body.days, id]
    );

    const [[row]] = await pool.query(
      'SELECT id, username, expire_at, updated_at FROM users WHERE id=?',
      [id]
    );
    if (!row) throw new HttpError(404, 'Not found');
    res.json({ ok: true, data: row });
  })
);

module.exports = { router };


