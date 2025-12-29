const express = require('express');
const { pool } = require('../db/pool');
const { adminAuth } = require('../middlewares/adminAuth');
const { asyncHandler } = require('../utils/asyncHandler');
const { HttpError } = require('../utils/httpError');

const router = express.Router();

/**
 * Admin: list logs for an agent (latest first).
 */
router.get(
  '/admin/agents/:agentId/logs',
  adminAuth,
  asyncHandler(async (req, res) => {
    const agentId = Number(req.params.agentId);
    if (!Number.isFinite(agentId) || agentId <= 0) throw new HttpError(400, 'Invalid agentId');

    const limit = Math.min(Math.max(Number(req.query.limit || 100), 1), 500);
    const [rows] = await pool.query(
      'SELECT id, level, message, meta_json, created_at FROM agent_logs WHERE agent_id=? ORDER BY id DESC LIMIT ?',
      [agentId, limit]
    );
    res.json({ ok: true, data: rows });
  })
);

module.exports = { router };


