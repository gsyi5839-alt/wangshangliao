const { HttpError } = require('../utils/httpError');

/**
 * Express error handler.
 * Never leak internal details in production.
 */
function errorHandler(err, req, res, next) {
  // eslint-disable-next-line no-unused-vars
  void next;
  const isHttpError = err instanceof HttpError;

  const status = isHttpError ? err.status : 500;
  const message = isHttpError ? err.message : 'Internal Server Error';

  res.status(status).json({
    ok: false,
    error: {
      message,
      ...(isHttpError && err.details !== undefined ? { details: err.details } : {})
    }
  });
}

module.exports = { errorHandler };


