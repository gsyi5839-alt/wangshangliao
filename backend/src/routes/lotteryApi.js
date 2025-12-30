/**
 * Lottery API Management Routes
 * Admin can manage lottery API endpoints (bcapi.cn)
 */
const express = require('express');
const { z } = require('zod');
const { pool } = require('../db/pool');
const { adminAuth } = require('../middlewares/adminAuth');
const { asyncHandler } = require('../utils/asyncHandler');
const { HttpError } = require('../utils/httpError');

const router = express.Router();

/**
 * Admin: list all lottery APIs
 */
router.get(
  '/admin/lottery-apis',
  adminAuth,
  asyncHandler(async (req, res) => {
    const [rows] = await pool.query(
      `SELECT id, name, code, token, api_url, backup_url, format_type, callback_name,
              rows_count, request_interval, max_requests_per_30s, is_enabled, remark,
              created_at, updated_at
       FROM lottery_apis
       ORDER BY id ASC`
    );
    res.json({ ok: true, data: rows });
  })
);

/**
 * Admin: get single lottery API by id
 */
router.get(
  '/admin/lottery-apis/:id',
  adminAuth,
  asyncHandler(async (req, res) => {
    const id = Number(req.params.id);
    if (!Number.isFinite(id) || id <= 0) throw new HttpError(400, 'Invalid id');

    const [[row]] = await pool.query('SELECT * FROM lottery_apis WHERE id = ?', [id]);
    if (!row) throw new HttpError(404, 'Lottery API not found');

    res.json({ ok: true, data: row });
  })
);

/**
 * Admin: create a new lottery API
 */
router.post(
  '/admin/lottery-apis',
  adminAuth,
  asyncHandler(async (req, res) => {
    const schema = z.object({
      name: z.string().min(1).max(64),
      code: z.string().min(1).max(32),
      token: z.string().min(1).max(128),
      api_url: z.string().min(1).max(512),
      backup_url: z.string().max(512).optional().nullable(),
      format_type: z.enum(['json', 'jsonp', 'xml']).default('json'),
      callback_name: z.string().max(64).optional().nullable(),
      rows_count: z.number().int().min(1).max(20).default(1),
      request_interval: z.number().int().min(100).max(60000).default(1000),
      max_requests_per_30s: z.number().int().min(1).max(100).default(40),
      is_enabled: z.boolean().default(true),
      remark: z.string().optional().nullable()
    });
    const body = schema.parse(req.body);

    // Check if code already exists
    const [[existing]] = await pool.query('SELECT id FROM lottery_apis WHERE code = ?', [body.code]);
    if (existing) throw new HttpError(400, `彩票代码 "${body.code}" 已存在`);

    const [result] = await pool.query(
      `INSERT INTO lottery_apis (name, code, token, api_url, backup_url, format_type, callback_name,
                                  rows_count, request_interval, max_requests_per_30s, is_enabled, remark)
       VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)`,
      [
        body.name,
        body.code,
        body.token,
        body.api_url,
        body.backup_url || null,
        body.format_type,
        body.callback_name || 'jsonpReturn',
        body.rows_count,
        body.request_interval,
        body.max_requests_per_30s,
        body.is_enabled ? 1 : 0,
        body.remark || null
      ]
    );

    const [[row]] = await pool.query('SELECT * FROM lottery_apis WHERE id = ?', [result.insertId]);
    res.json({ ok: true, data: row });
  })
);

/**
 * Admin: update a lottery API
 */
router.put(
  '/admin/lottery-apis/:id',
  adminAuth,
  asyncHandler(async (req, res) => {
    const id = Number(req.params.id);
    if (!Number.isFinite(id) || id <= 0) throw new HttpError(400, 'Invalid id');

    const schema = z.object({
      name: z.string().min(1).max(64).optional(),
      code: z.string().min(1).max(32).optional(),
      token: z.string().min(1).max(128).optional(),
      api_url: z.string().min(1).max(512).optional(),
      backup_url: z.string().max(512).optional().nullable(),
      format_type: z.enum(['json', 'jsonp', 'xml']).optional(),
      callback_name: z.string().max(64).optional().nullable(),
      rows_count: z.number().int().min(1).max(20).optional(),
      request_interval: z.number().int().min(100).max(60000).optional(),
      max_requests_per_30s: z.number().int().min(1).max(100).optional(),
      is_enabled: z.boolean().optional(),
      remark: z.string().optional().nullable()
    });
    const body = schema.parse(req.body);

    // Check if record exists
    const [[existing]] = await pool.query('SELECT id FROM lottery_apis WHERE id = ?', [id]);
    if (!existing) throw new HttpError(404, 'Lottery API not found');

    // Check if code conflicts with another record
    if (body.code) {
      const [[conflict]] = await pool.query('SELECT id FROM lottery_apis WHERE code = ? AND id != ?', [body.code, id]);
      if (conflict) throw new HttpError(400, `彩票代码 "${body.code}" 已被其他接口使用`);
    }

    // Build update query
    const updates = [];
    const params = [];

    if (body.name !== undefined) { updates.push('name = ?'); params.push(body.name); }
    if (body.code !== undefined) { updates.push('code = ?'); params.push(body.code); }
    if (body.token !== undefined) { updates.push('token = ?'); params.push(body.token); }
    if (body.api_url !== undefined) { updates.push('api_url = ?'); params.push(body.api_url); }
    if (body.backup_url !== undefined) { updates.push('backup_url = ?'); params.push(body.backup_url); }
    if (body.format_type !== undefined) { updates.push('format_type = ?'); params.push(body.format_type); }
    if (body.callback_name !== undefined) { updates.push('callback_name = ?'); params.push(body.callback_name); }
    if (body.rows_count !== undefined) { updates.push('rows_count = ?'); params.push(body.rows_count); }
    if (body.request_interval !== undefined) { updates.push('request_interval = ?'); params.push(body.request_interval); }
    if (body.max_requests_per_30s !== undefined) { updates.push('max_requests_per_30s = ?'); params.push(body.max_requests_per_30s); }
    if (body.is_enabled !== undefined) { updates.push('is_enabled = ?'); params.push(body.is_enabled ? 1 : 0); }
    if (body.remark !== undefined) { updates.push('remark = ?'); params.push(body.remark); }

    if (updates.length === 0) throw new HttpError(400, 'No fields to update');

    params.push(id);
    await pool.query(`UPDATE lottery_apis SET ${updates.join(', ')} WHERE id = ?`, params);

    const [[row]] = await pool.query('SELECT * FROM lottery_apis WHERE id = ?', [id]);
    res.json({ ok: true, data: row });
  })
);

/**
 * Admin: delete a lottery API
 */
router.delete(
  '/admin/lottery-apis/:id',
  adminAuth,
  asyncHandler(async (req, res) => {
    const id = Number(req.params.id);
    if (!Number.isFinite(id) || id <= 0) throw new HttpError(400, 'Invalid id');

    const [result] = await pool.query('DELETE FROM lottery_apis WHERE id = ?', [id]);
    if (result.affectedRows === 0) throw new HttpError(404, 'Lottery API not found');

    res.json({ ok: true, message: '删除成功' });
  })
);

/**
 * Admin: test lottery API connection
 */
router.post(
  '/admin/lottery-apis/:id/test',
  adminAuth,
  asyncHandler(async (req, res) => {
    const id = Number(req.params.id);
    if (!Number.isFinite(id) || id <= 0) throw new HttpError(400, 'Invalid id');

    const [[api]] = await pool.query('SELECT * FROM lottery_apis WHERE id = ?', [id]);
    if (!api) throw new HttpError(404, 'Lottery API not found');

    // Build the actual URL
    const url = api.api_url
      .replace('{token}', api.token)
      .replace('{code}', api.code)
      .replace('{rows}', String(api.rows_count))
      .replace('{format}', api.format_type);

    try {
      const controller = new AbortController();
      const timeout = setTimeout(() => controller.abort(), 10000);

      const response = await fetch(url, {
        method: 'GET',
        signal: controller.signal,
        headers: { 'User-Agent': 'BocailBot/1.0' }
      });
      clearTimeout(timeout);

      if (!response.ok) {
        throw new Error(`HTTP ${response.status}`);
      }

      const data = await response.json();
      res.json({
        ok: true,
        data: {
          success: true,
          url,
          response: data
        }
      });
    } catch (err) {
      res.json({
        ok: true,
        data: {
          success: false,
          url,
          error: err.message
        }
      });
    }
  })
);

/**
 * Client: get enabled lottery APIs (for desktop client)
 * This endpoint doesn't require admin auth, only client token
 */
router.get(
  '/client/lottery-apis',
  asyncHandler(async (req, res) => {
    const [rows] = await pool.query(
      `SELECT id, name, code, token, api_url, backup_url, format_type, callback_name,
              rows_count, request_interval, max_requests_per_30s
       FROM lottery_apis
       WHERE is_enabled = 1
       ORDER BY id ASC`
    );
    res.json({ ok: true, data: rows });
  })
);

module.exports = { router };

