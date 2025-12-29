const express = require('express');
const { z } = require('zod');
const { pool } = require('../db/pool');
const { hashPassword, verifyPassword } = require('../auth/password');
const { signUserToken } = require('../auth/jwt');
const { asyncHandler } = require('../utils/asyncHandler');
const { HttpError } = require('../utils/httpError');

const router = express.Router();

const CARD_CODE_RE = /^[A-Za-z0-9]{26}$/;
const CARD_PASS_RE = /^[A-Za-z0-9]{18}$/;

function computeDaysLeft(expireAt) {
  if (!expireAt) return null;
  const ms = new Date(expireAt).getTime() - Date.now();
  return Math.floor(ms / (24 * 3600 * 1000));
}

/**
 * Client: login
 */
router.post(
  '/client/login',
  asyncHandler(async (req, res) => {
    const schema = z.object({
      username: z.string().min(1).max(64),
      password: z.string().min(1).max(128)
    });
    const body = schema.parse(req.body);

    const [rows] = await pool.query(
      'SELECT id, username, password_hash, expire_at FROM users WHERE username=?',
      [body.username]
    );
    if (!rows.length) throw new HttpError(401, 'Invalid username or password');

    const user = rows[0];
    const ok = await verifyPassword(body.password, user.password_hash);
    if (!ok) throw new HttpError(401, 'Invalid username or password');

    const token = signUserToken({ userId: user.id, username: user.username });
    res.json({
      ok: true,
      data: {
        token,
        expireAt: user.expire_at,
        daysLeft: computeDaysLeft(user.expire_at)
      }
    });
  })
);

/**
 * Client: register (requires recharge card + super password).
 * Notes:
 * - Only accepts a single card code.
 * - The card must exist and be unused.
 */
router.post(
  '/client/register',
  asyncHandler(async (req, res) => {
    const schema = z.object({
      username: z.string().min(3).max(64),
      password: z.string().min(6).max(128),
      superPassword: z.string().min(6).max(128),
      cardCode: z.string().min(1).max(64),
      cardPassword: z.string().min(1).max(64),
      boundInfo: z.string().max(255).optional().nullable(),
      promoterUsername: z.string().max(64).optional().nullable()
    });
    const body = schema.parse(req.body);

    if (!CARD_CODE_RE.test(body.cardCode)) throw new HttpError(400, 'Invalid card code');
    if (!CARD_PASS_RE.test(body.cardPassword)) throw new HttpError(400, 'Invalid card password');

    const conn = await pool.getConnection();
    try {
      await conn.beginTransaction();

      const [cardRows] = await conn.query(
        'SELECT id, days, used_by_user_id, card_password_hash FROM recharge_cards WHERE card_code=? FOR UPDATE',
        [body.cardCode]
      );
      if (!cardRows.length) throw new HttpError(400, 'Invalid recharge card');
      if (cardRows[0].used_by_user_id) throw new HttpError(400, 'Recharge card already used');
      if (!cardRows[0].card_password_hash) throw new HttpError(400, 'Recharge card not activated');
      {
        const ok = await verifyPassword(body.cardPassword, cardRows[0].card_password_hash);
        if (!ok) throw new HttpError(400, 'Invalid recharge card');
      }

      const passwordHash = await hashPassword(body.password);
      const superHash = await hashPassword(body.superPassword);

      // Create user
      await conn.query(
        'INSERT INTO users (username, password_hash, super_password_hash, bound_info, promoter_username, expire_at) VALUES (?, ?, ?, ?, ?, DATE_ADD(NOW(), INTERVAL ? DAY))',
        [
          body.username,
          passwordHash,
          superHash,
          body.boundInfo ?? null,
          body.promoterUsername ?? null,
          Number(cardRows[0].days || 30)
        ]
      );

      const [[user]] = await conn.query('SELECT id, username, expire_at FROM users WHERE username=?', [
        body.username
      ]);

      // Consume card
      await conn.query(
        'UPDATE recharge_cards SET used_by_user_id=?, used_at=NOW() WHERE id=?',
        [user.id, cardRows[0].id]
      );

      await conn.commit();

      const token = signUserToken({ userId: user.id, username: user.username });
      res.json({ ok: true, data: { token, expireAt: user.expire_at, daysLeft: computeDaysLeft(user.expire_at) } });
    } catch (err) {
      await conn.rollback();
      throw err;
    } finally {
      conn.release();
    }
  })
);

/**
 * Client: recharge by card.
 */
router.post(
  '/client/recharge',
  asyncHandler(async (req, res) => {
    const schema = z.object({
      username: z.string().min(1).max(64),
      cardCode: z.string().min(1).max(64),
      cardPassword: z.string().min(1).max(64)
    });
    const body = schema.parse(req.body);

    if (!CARD_CODE_RE.test(body.cardCode)) throw new HttpError(400, 'Invalid card code');
    if (!CARD_PASS_RE.test(body.cardPassword)) throw new HttpError(400, 'Invalid card password');

    const conn = await pool.getConnection();
    try {
      await conn.beginTransaction();

      const [userRows] = await conn.query('SELECT id, expire_at FROM users WHERE username=? FOR UPDATE', [
        body.username
      ]);
      if (!userRows.length) throw new HttpError(400, 'User not found');
      const user = userRows[0];

      const [cardRows] = await conn.query(
        'SELECT id, days, used_by_user_id, card_password_hash FROM recharge_cards WHERE card_code=? FOR UPDATE',
        [body.cardCode]
      );
      if (!cardRows.length) throw new HttpError(400, 'Invalid recharge card');
      if (cardRows[0].used_by_user_id) throw new HttpError(400, 'Recharge card already used');
      if (!cardRows[0].card_password_hash) throw new HttpError(400, 'Recharge card not activated');
      {
        const ok = await verifyPassword(body.cardPassword, cardRows[0].card_password_hash);
        if (!ok) throw new HttpError(400, 'Invalid recharge card');
      }

      const days = Number(cardRows[0].days || 30);
      // Extend from max(now, expire_at)
      await conn.query(
        'UPDATE users SET expire_at = DATE_ADD(GREATEST(IFNULL(expire_at, NOW()), NOW()), INTERVAL ? DAY) WHERE id=?',
        [days, user.id]
      );

      await conn.query('UPDATE recharge_cards SET used_by_user_id=?, used_at=NOW() WHERE id=?', [
        user.id,
        cardRows[0].id
      ]);

      const [[updated]] = await conn.query('SELECT expire_at FROM users WHERE id=?', [user.id]);
      await conn.commit();

      res.json({ ok: true, data: { expireAt: updated.expire_at, daysLeft: computeDaysLeft(updated.expire_at) } });
    } catch (err) {
      await conn.rollback();
      throw err;
    } finally {
      conn.release();
    }
  })
);

/**
 * Client: change password by super password.
 */
router.post(
  '/client/change-password',
  asyncHandler(async (req, res) => {
    const schema = z.object({
      username: z.string().min(1).max(64),
      superPassword: z.string().min(1).max(128),
      newPassword: z.string().min(6).max(128)
    });
    const body = schema.parse(req.body);

    const [rows] = await pool.query('SELECT id, super_password_hash FROM users WHERE username=?', [
      body.username
    ]);
    if (!rows.length) throw new HttpError(400, 'User not found');

    const ok = await verifyPassword(body.superPassword, rows[0].super_password_hash);
    if (!ok) throw new HttpError(401, 'Invalid super password');

    const newHash = await hashPassword(body.newPassword);
    await pool.query('UPDATE users SET password_hash=? WHERE id=?', [newHash, rows[0].id]);
    res.json({ ok: true });
  })
);

module.exports = { router };


