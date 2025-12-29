const mysql = require('mysql2/promise');
const { DB_HOST, DB_PORT, DB_USER, DB_PASSWORD, DB_NAME } = require('../config/env');

/**
 * MySQL connection pool for the backend.
 * NOTE: Keep connections short-lived; use pool.query / pool.execute.
 */
const pool = mysql.createPool({
  host: DB_HOST,
  port: DB_PORT,
  user: DB_USER,
  password: DB_PASSWORD,
  database: DB_NAME,
  waitForConnections: true,
  connectionLimit: 10,
  queueLimit: 0,
  timezone: 'Z'
});

module.exports = { pool };


