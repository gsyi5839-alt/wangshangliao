const express = require('express');
const { z } = require('zod');
const { pool } = require('../db/pool');
const { verifyPassword } = require('../auth/password');
const { signAdminToken } = require('../auth/jwt');
const { asyncHandler } = require('../utils/asyncHandler');
const { HttpError } = require('../utils/httpError');

const router = express.Router();

router.post(
  '/auth/login',
  asyncHandler(async (req, res) => {
    const schema = z.object({
      username: z.string().min(1).max(64),
      password: z.string().min(1).max(128)
    });
    const body = schema.parse(req.body);

    const [rows] = await pool.query('SELECT id, username, password_hash FROM admins WHERE username=?', [
      body.username
    ]);
    if (!rows.length) throw new HttpError(401, 'Invalid username or password');

    const admin = rows[0];
    const ok = await verifyPassword(body.password, admin.password_hash);
    if (!ok) throw new HttpError(401, 'Invalid username or password');

    const token = signAdminToken({ adminId: admin.id, username: admin.username });
    res.json({ ok: true, data: { token } });
  })
);

module.exports = { router };


