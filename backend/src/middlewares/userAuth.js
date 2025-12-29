const { verifyUserToken } = require('../auth/jwt');
const { HttpError } = require('../utils/httpError');

/**
 * Require user JWT via Authorization: Bearer <token>
 * Attaches req.user = { id, username }.
 */
function userAuth(req, res, next) {
  const header = req.headers.authorization || '';
  const [scheme, token] = header.split(' ');
  if (scheme !== 'Bearer' || !token) return next(new HttpError(401, 'Unauthorized'));

  try {
    const decoded = verifyUserToken(token);
    req.user = { id: decoded.sub, username: decoded.username };
    return next();
  } catch (err) {
    return next(err);
  }
}

module.exports = { userAuth };


