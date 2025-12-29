const { verifyAdminToken } = require('../auth/jwt');
const { HttpError } = require('../utils/httpError');

/**
 * Require admin JWT via Authorization: Bearer <token>
 */
function adminAuth(req, res, next) {
  const header = req.headers.authorization || '';
  const [scheme, token] = header.split(' ');
  if (scheme !== 'Bearer' || !token) return next(new HttpError(401, 'Unauthorized'));

  try {
    const decoded = verifyAdminToken(token);
    req.admin = { id: decoded.sub, username: decoded.username };
    return next();
  } catch (err) {
    return next(err);
  }
}

module.exports = { adminAuth };


