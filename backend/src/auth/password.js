const bcrypt = require('bcryptjs');

/**
 * Hash a plain password.
 * @param {string} plain
 * @returns {Promise<string>}
 */
async function hashPassword(plain) {
  const saltRounds = 10;
  return bcrypt.hash(plain, saltRounds);
}

/**
 * Verify a plain password against a bcrypt hash.
 * @param {string} plain
 * @param {string} hash
 * @returns {Promise<boolean>}
 */
async function verifyPassword(plain, hash) {
  return bcrypt.compare(plain, hash);
}

module.exports = { hashPassword, verifyPassword };


