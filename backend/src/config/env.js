const dotenv = require('dotenv');

// Load environment variables from ".env" if present.
dotenv.config();

/**
 * Read environment variable with an optional default.
 * @param {string} key
 * @param {string | undefined} defaultValue
 * @returns {string}
 */
function env(key, defaultValue) {
  const value = process.env[key];
  if (value === undefined || value === '') return defaultValue ?? '';
  return value;
}

module.exports = {
  env,
  NODE_ENV: env('NODE_ENV', 'production'),
  PORT: Number(env('PORT', '3001')),
  DB_HOST: env('DB_HOST', '127.0.0.1'),
  DB_PORT: Number(env('DB_PORT', '3306')),
  DB_USER: env('DB_USER', 'root'),
  DB_PASSWORD: env('DB_PASSWORD', ''),
  DB_NAME: env('DB_NAME', 'bocail'),
  JWT_SECRET: env('JWT_SECRET', ''),
  JWT_EXPIRES_IN: env('JWT_EXPIRES_IN', '7d'),
  ADMIN_BOOTSTRAP_USER: env('ADMIN_BOOTSTRAP_USER', 'admin'),
  ADMIN_BOOTSTRAP_PASS: env('ADMIN_BOOTSTRAP_PASS', '')
};


