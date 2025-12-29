const express = require('express');
const { z } = require('zod');
const { pool } = require('../db/pool');
const { agentAuth } = require('../middlewares/agentAuth');
const { asyncHandler } = require('../utils/asyncHandler');

const router = express.Router();

/**
 * Agent: heartbeat.
 * Updates last_seen and returns minimal configuration pointers.
 */
router.post(
  '/agent/heartbeat',
  agentAuth,
  asyncHandler(async (req, res) => {
    const schema = z.object({
      status: z.string().min(1).max(32).optional()
    });
    const body = schema.parse(req.body || {});

    const ip =
      String(req.headers['x-forwarded-for'] || '').split(',')[0].trim() ||
      String(req.socket.remoteAddress || '');

    await pool.query(
      'UPDATE agents SET last_seen_at=NOW(), last_ip=?, status=? WHERE id=?',
      [ip, body.status || 'online', req.agent.id]
    );

    res.json({ ok: true, data: { agentId: req.agent.id } });
  })
);

/**
 * Agent: fetch configuration (rules + pending tasks).
 * The desktop bot should poll this endpoint.
 */
router.get(
  '/agent/config',
  agentAuth,
  asyncHandler(async (req, res) => {
    const [rules] = await pool.query(
      'SELECT id, group_cloud_id, enabled, match_type, match_text, reply_text, priority, updated_at FROM auto_reply_rules WHERE agent_id=? ORDER BY priority ASC, id ASC',
      [req.agent.id]
    );

    const [tasks] = await pool.query(
      "SELECT id, group_cloud_id, scene, to_id, text, status, scheduled_at, created_at FROM broadcast_tasks WHERE agent_id=? AND status='pending' ORDER BY created_at ASC LIMIT 50",
      [req.agent.id]
    );

    res.json({ ok: true, data: { rules, tasks } });
  })
);

/**
 * Agent: report a task result.
 */
router.post(
  '/agent/tasks/:id/result',
  agentAuth,
  asyncHandler(async (req, res) => {
    const id = Number(req.params.id);
    const schema = z.object({
      status: z.enum(['done', 'failed']),
      result: z.any().optional()
    });
    const body = schema.parse(req.body || {});

    await pool.query(
      'UPDATE broadcast_tasks SET status=?, finished_at=NOW(), result_json=? WHERE id=? AND agent_id=?',
      [body.status, body.result === undefined ? null : JSON.stringify(body.result), id, req.agent.id]
    );

    res.json({ ok: true });
  })
);

/**
 * Agent: append logs.
 */
router.post(
  '/agent/logs',
  agentAuth,
  asyncHandler(async (req, res) => {
    const schema = z.object({
      level: z.enum(['debug', 'info', 'warn', 'error']).default('info'),
      message: z.string().min(1).max(255),
      meta: z.any().optional()
    });
    const body = schema.parse(req.body || {});

    await pool.query('INSERT INTO agent_logs (agent_id, level, message, meta_json) VALUES (?, ?, ?, ?)', [
      req.agent.id,
      body.level,
      body.message,
      body.meta === undefined ? null : JSON.stringify(body.meta)
    ]);

    res.json({ ok: true });
  })
);

module.exports = { router };


