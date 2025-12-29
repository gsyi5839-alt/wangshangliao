const express = require('express');
const { z } = require('zod');
const { pool } = require('../db/pool');
const { adminAuth } = require('../middlewares/adminAuth');
const { asyncHandler } = require('../utils/asyncHandler');
const { HttpError } = require('../utils/httpError');

const router = express.Router();

/**
 * Admin: list auto-reply rules for an agent.
 */
router.get(
  '/admin/agents/:agentId/rules',
  adminAuth,
  asyncHandler(async (req, res) => {
    const agentId = Number(req.params.agentId);
    if (!Number.isFinite(agentId) || agentId <= 0) throw new HttpError(400, 'Invalid agentId');

    const [rows] = await pool.query(
      'SELECT id, group_cloud_id, enabled, match_type, match_text, reply_text, priority, created_at, updated_at FROM auto_reply_rules WHERE agent_id=? ORDER BY priority ASC, id ASC',
      [agentId]
    );
    res.json({ ok: true, data: rows });
  })
);

/**
 * Admin: create a rule for an agent.
 */
router.post(
  '/admin/agents/:agentId/rules',
  adminAuth,
  asyncHandler(async (req, res) => {
    const agentId = Number(req.params.agentId);
    if (!Number.isFinite(agentId) || agentId <= 0) throw new HttpError(400, 'Invalid agentId');

    const schema = z.object({
      groupCloudId: z.string().max(64).optional().nullable(),
      enabled: z.boolean().default(true),
      matchType: z.enum(['contains', 'regex', 'equals']).default('contains'),
      matchText: z.string().min(1).max(255),
      replyText: z.string().min(1),
      priority: z.number().int().min(0).max(100000).default(100)
    });
    const body = schema.parse(req.body);

    await pool.query(
      'INSERT INTO auto_reply_rules (agent_id, group_cloud_id, enabled, match_type, match_text, reply_text, priority) VALUES (?, ?, ?, ?, ?, ?, ?)',
      [
        agentId,
        body.groupCloudId ?? null,
        body.enabled ? 1 : 0,
        body.matchType,
        body.matchText,
        body.replyText,
        body.priority
      ]
    );

    const [[row]] = await pool.query(
      'SELECT id, group_cloud_id, enabled, match_type, match_text, reply_text, priority, created_at, updated_at FROM auto_reply_rules WHERE agent_id=? ORDER BY id DESC LIMIT 1',
      [agentId]
    );
    res.json({ ok: true, data: row });
  })
);

/**
 * Admin: update a rule.
 */
router.put(
  '/admin/rules/:id',
  adminAuth,
  asyncHandler(async (req, res) => {
    const id = Number(req.params.id);
    if (!Number.isFinite(id) || id <= 0) throw new HttpError(400, 'Invalid id');

    const schema = z.object({
      groupCloudId: z.string().max(64).optional().nullable(),
      enabled: z.boolean().optional(),
      matchType: z.enum(['contains', 'regex', 'equals']).optional(),
      matchText: z.string().min(1).max(255).optional(),
      replyText: z.string().min(1).optional(),
      priority: z.number().int().min(0).max(100000).optional()
    });
    const body = schema.parse(req.body || {});

    // Build partial update so we can distinguish:
    // - "field not provided" (no change)
    // - "field provided as null" (set NULL)
    const sets = [];
    const params = [];
    if (body.groupCloudId !== undefined) {
      sets.push('group_cloud_id=?');
      params.push(body.groupCloudId);
    }
    if (body.enabled !== undefined) {
      sets.push('enabled=?');
      params.push(body.enabled ? 1 : 0);
    }
    if (body.matchType !== undefined) {
      sets.push('match_type=?');
      params.push(body.matchType);
    }
    if (body.matchText !== undefined) {
      sets.push('match_text=?');
      params.push(body.matchText);
    }
    if (body.replyText !== undefined) {
      sets.push('reply_text=?');
      params.push(body.replyText);
    }
    if (body.priority !== undefined) {
      sets.push('priority=?');
      params.push(body.priority);
    }
    if (sets.length === 0) throw new HttpError(400, 'No fields to update');

    params.push(id);
    await pool.query(`UPDATE auto_reply_rules SET ${sets.join(', ')} WHERE id=?`, params);

    const [[row]] = await pool.query(
      'SELECT id, agent_id, group_cloud_id, enabled, match_type, match_text, reply_text, priority, created_at, updated_at FROM auto_reply_rules WHERE id=?',
      [id]
    );
    if (!row) throw new HttpError(404, 'Not found');
    res.json({ ok: true, data: row });
  })
);

/**
 * Admin: delete a rule.
 */
router.delete(
  '/admin/rules/:id',
  adminAuth,
  asyncHandler(async (req, res) => {
    const id = Number(req.params.id);
    if (!Number.isFinite(id) || id <= 0) throw new HttpError(400, 'Invalid id');
    await pool.query('DELETE FROM auto_reply_rules WHERE id=?', [id]);
    res.json({ ok: true });
  })
);

module.exports = { router };


