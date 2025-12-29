const crypto = require('crypto');
const express = require('express');
const { z } = require('zod');
const { pool } = require('../db/pool');
const { adminAuth } = require('../middlewares/adminAuth');
const { asyncHandler } = require('../utils/asyncHandler');
const { HttpError } = require('../utils/httpError');

const router = express.Router();

/**
 * Admin: list agents.
 */
router.get(
  '/admin/agents',
  adminAuth,
  asyncHandler(async (req, res) => {
    const [rows] = await pool.query(
      'SELECT id, name, description, last_seen_at, last_ip, status, created_at, updated_at FROM agents ORDER BY id DESC'
    );
    res.json({ ok: true, data: rows });
  })
);

/**
 * Admin: create agent (generates agent_key).
 */
router.post(
  '/admin/agents',
  adminAuth,
  asyncHandler(async (req, res) => {
    const schema = z.object({
      name: z.string().min(1).max(128),
      description: z.string().max(255).optional().nullable()
    });
    const body = schema.parse(req.body);

    const agentKey = crypto.randomBytes(24).toString('base64url');
    await pool.query('INSERT INTO agents (name, agent_key, description, status) VALUES (?, ?, ?, ?)', [
      body.name,
      agentKey,
      body.description ?? null,
      'offline'
    ]);

    const [[created]] = await pool.query(
      'SELECT id, name, agent_key, description, created_at FROM agents WHERE agent_key=?',
      [agentKey]
    );
    res.json({ ok: true, data: created });
  })
);

/**
 * Admin: delete agent.
 */
router.delete(
  '/admin/agents/:id',
  adminAuth,
  asyncHandler(async (req, res) => {
    const id = Number(req.params.id);
    if (!Number.isFinite(id) || id <= 0) throw new HttpError(400, 'Invalid id');
    await pool.query('DELETE FROM agents WHERE id=?', [id]);
    res.json({ ok: true });
  })
);

module.exports = { router };


