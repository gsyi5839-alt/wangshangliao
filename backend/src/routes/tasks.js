const express = require('express');
const { z } = require('zod');
const { pool } = require('../db/pool');
const { adminAuth } = require('../middlewares/adminAuth');
const { asyncHandler } = require('../utils/asyncHandler');
const { HttpError } = require('../utils/httpError');

const router = express.Router();

/**
 * Admin: list tasks for an agent.
 */
router.get(
  '/admin/agents/:agentId/tasks',
  adminAuth,
  asyncHandler(async (req, res) => {
    const agentId = Number(req.params.agentId);
    if (!Number.isFinite(agentId) || agentId <= 0) throw new HttpError(400, 'Invalid agentId');

    const [rows] = await pool.query(
      'SELECT id, group_cloud_id, scene, to_id, text, status, scheduled_at, started_at, finished_at, result_json, created_at, updated_at FROM broadcast_tasks WHERE agent_id=? ORDER BY id DESC LIMIT 200',
      [agentId]
    );
    res.json({ ok: true, data: rows });
  })
);

/**
 * Admin: create a broadcast task for an agent.
 */
router.post(
  '/admin/agents/:agentId/tasks',
  adminAuth,
  asyncHandler(async (req, res) => {
    const agentId = Number(req.params.agentId);
    if (!Number.isFinite(agentId) || agentId <= 0) throw new HttpError(400, 'Invalid agentId');

    const schema = z.object({
      scene: z.enum(['team', 'p2p']).default('team'),
      groupCloudId: z.string().max(64).optional().nullable(),
      toId: z.string().max(64).optional().nullable(),
      text: z.string().min(1),
      scheduledAt: z.string().datetime().optional().nullable()
    });
    const body = schema.parse(req.body);

    await pool.query(
      'INSERT INTO broadcast_tasks (agent_id, group_cloud_id, scene, to_id, text, status, scheduled_at) VALUES (?, ?, ?, ?, ?, ?, ?)',
      [
        agentId,
        body.groupCloudId ?? null,
        body.scene,
        body.toId ?? null,
        body.text,
        'pending',
        body.scheduledAt ? new Date(body.scheduledAt) : null
      ]
    );

    const [[row]] = await pool.query(
      'SELECT id, group_cloud_id, scene, to_id, text, status, scheduled_at, created_at FROM broadcast_tasks WHERE agent_id=? ORDER BY id DESC LIMIT 1',
      [agentId]
    );
    res.json({ ok: true, data: row });
  })
);

/**
 * Admin: cancel a task (only if pending).
 */
router.post(
  '/admin/tasks/:id/cancel',
  adminAuth,
  asyncHandler(async (req, res) => {
    const id = Number(req.params.id);
    if (!Number.isFinite(id) || id <= 0) throw new HttpError(400, 'Invalid id');

    await pool.query("UPDATE broadcast_tasks SET status='cancelled', finished_at=NOW() WHERE id=? AND status='pending'", [
      id
    ]);
    res.json({ ok: true });
  })
);

module.exports = { router };


