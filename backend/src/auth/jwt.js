const jwt = require('jsonwebtoken');
const { JWT_SECRET, JWT_EXPIRES_IN } = require('../config/env');
const { HttpError } = require('../utils/httpError');

/**
 * Sign a JWT for an admin.
 * @param {{ adminId: string|number, username: string }} payload
 * @returns {string}
 */
function signAdminToken(payload) {
  if (!JWT_SECRET) throw new Error('JWT_SECRET is required');
  return jwt.sign(
    {
      sub: String(payload.adminId),
      username: payload.username,
      typ: 'admin'
    },
    JWT_SECRET,
    { expiresIn: JWT_EXPIRES_IN }
  );
}

/**
 * Sign a JWT for a client user (desktop bot login).
 * @param {{ userId: string|number, username: string }} payload
 * @returns {string}
 */
function signUserToken(payload) {
  if (!JWT_SECRET) throw new Error('JWT_SECRET is required');
  return jwt.sign(
    {
      sub: String(payload.userId),
      username: payload.username,
      typ: 'user'
    },
    JWT_SECRET,
    { expiresIn: JWT_EXPIRES_IN }
  );
}

/**
 * Verify an admin JWT and return decoded payload.
 * @param {string} token
 * @returns {{ sub: string, username: string, typ: string }}
 */
function verifyAdminToken(token) {
  if (!JWT_SECRET) throw new Error('JWT_SECRET is required');
  try {
    const decoded = jwt.verify(token, JWT_SECRET);
    if (!decoded || decoded.typ !== 'admin') throw new HttpError(401, 'Invalid token');
    return decoded;
  } catch (err) {
    throw new HttpError(401, 'Unauthorized');
  }
}

/**
 * Verify a user JWT and return decoded payload.
 * @param {string} token
 * @returns {{ sub: string, username: string, typ: string }}
 */
function verifyUserToken(token) {
  if (!JWT_SECRET) throw new Error('JWT_SECRET is required');
  try {
    const decoded = jwt.verify(token, JWT_SECRET);
    if (!decoded || decoded.typ !== 'user') throw new HttpError(401, 'Invalid token');
    return decoded;
  } catch (err) {
    throw new HttpError(401, 'Unauthorized');
  }
}

module.exports = { signAdminToken, verifyAdminToken, signUserToken, verifyUserToken };


