const fs = require('fs');
const path = require('path');
const { pool } = require('./pool');

/**
 * Apply .sql migrations in lexicographic order and record them in schema_migrations.
 * This is designed for MySQL 5.7+ with InnoDB.
 */
async function migrate() {
  const migrationsDir = path.join(__dirname, 'migrations');
  const entries = fs.readdirSync(migrationsDir).filter((f) => f.endsWith('.sql')).sort();

  // Ensure migration table exists first (safe to run repeatedly).
  await pool.query(`
    CREATE TABLE IF NOT EXISTS schema_migrations (
      id INT UNSIGNED NOT NULL AUTO_INCREMENT,
      filename VARCHAR(255) NOT NULL,
      applied_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
      PRIMARY KEY (id),
      UNIQUE KEY uq_schema_migrations_filename (filename)
    ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
  `);

  const [appliedRows] = await pool.query('SELECT filename FROM schema_migrations');
  const applied = new Set(appliedRows.map((r) => r.filename));

  for (const filename of entries) {
    if (applied.has(filename)) continue;

    const fullPath = path.join(migrationsDir, filename);
    const sql = fs.readFileSync(fullPath, 'utf8');

    const conn = await pool.getConnection();
    try {
      await conn.beginTransaction();
      // NOTE: mysql2 doesn't support multiple statements by default in query(),
      // but execute() will run as-is; SQL file may contain multiple statements separated by ';'.
      // We split naively on ';' to keep it simple and predictable.
      const statements = sql
        .split(/;\s*[\r\n]+/g)
        .map((s) => s.trim())
        .filter(Boolean);

      for (const stmt of statements) {
        await conn.query(stmt);
      }
      await conn.query('INSERT INTO schema_migrations (filename) VALUES (?)', [filename]);
      await conn.commit();
      // eslint-disable-next-line no-console
      console.log(`[migrate] applied ${filename}`);
    } catch (err) {
      await conn.rollback();
      // eslint-disable-next-line no-console
      console.error(`[migrate] failed ${filename}:`, err);
      process.exitCode = 1;
      return;
    } finally {
      conn.release();
    }
  }

  // eslint-disable-next-line no-console
  console.log('[migrate] done');
}

migrate()
  .then(() => process.exit(0))
  .catch((err) => {
    // eslint-disable-next-line no-console
    console.error('[migrate] fatal:', err);
    process.exit(1);
  });


