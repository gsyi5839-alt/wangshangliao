const express = require('express');
const cors = require('cors');
const helmet = require('helmet');
const morgan = require('morgan');

const { PORT, NODE_ENV } = require('./config/env');
const { errorHandler } = require('./middlewares/errorHandler');
const { router: healthRouter } = require('./routes/health');
const { router: authRouter } = require('./routes/auth');
const { router: adminsRouter } = require('./routes/admins');
const { router: agentsRouter } = require('./routes/agents');
const { router: rulesRouter } = require('./routes/rules');
const { router: tasksRouter } = require('./routes/tasks');
const { router: logsRouter } = require('./routes/logs');
const { router: agentRuntimeRouter } = require('./routes/agentRuntime');
const { router: clientAuthRouter } = require('./routes/clientAuth');
const { router: clientContentRouter } = require('./routes/clientContent');
const { router: adminContentRouter } = require('./routes/adminContent');
const { router: usersRouter } = require('./routes/users');
const { router: lotteryApiRouter } = require('./routes/lotteryApi');
const { ensureBootstrapAdmin } = require('./services/seedAdmin');

async function main() {
  const app = express();

  app.disable('x-powered-by');
  app.use(helmet());
  app.use(
    cors({
      origin: '*',
      methods: ['GET', 'POST', 'PUT', 'DELETE', 'OPTIONS'],
      allowedHeaders: ['Content-Type', 'Authorization', 'X-Agent-Key']
    })
  );
  app.use(express.json({ limit: '1mb' }));
  app.use(morgan(NODE_ENV === 'production' ? 'combined' : 'dev'));

  // Routes
  app.use('/api', healthRouter);
  app.use('/api', authRouter);
  app.use('/api', adminsRouter);
  app.use('/api', agentsRouter);
  app.use('/api', rulesRouter);
  app.use('/api', tasksRouter);
  app.use('/api', logsRouter);
  app.use('/api', agentRuntimeRouter);
  app.use('/api', clientAuthRouter);
  app.use('/api', clientContentRouter);
  app.use('/api', adminContentRouter);
  app.use('/api', usersRouter);
  app.use('/api', lotteryApiRouter);

  // Error handler (must be last).
  app.use(errorHandler);

  await ensureBootstrapAdmin();

  app.listen(PORT, () => {
    // eslint-disable-next-line no-console
    console.log(`[server] listening on :${PORT}`);
  });
}

main().catch((err) => {
  // eslint-disable-next-line no-console
  console.error('[server] fatal:', err);
  process.exit(1);
});


