const { pool } = require('../db/pool');
const { HttpError } = require('../utils/httpError');

/**
 * Require agent_key via header: X-Agent-Key
 * Attaches req.agent = { id, name, agentKey }.
 */
async function agentAuth(req, res, next) {
  try {
    const agentKey = String(req.headers['x-agent-key'] || '').trim();
    if (!agentKey) return next(new HttpError(401, 'Unauthorized'));

    const [rows] = await pool.query('SELECT id, name, agent_key FROM agents WHERE agent_key=?', [
      agentKey
    ]);
    if (!rows.length) return next(new HttpError(401, 'Unauthorized'));

    req.agent = { id: rows[0].id, name: rows[0].name, agentKey: rows[0].agent_key };
    return next();
  } catch (err) {
    return next(err);
  }
}

module.exports = { agentAuth };


