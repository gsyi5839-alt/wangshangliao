module.exports = {
  apps: [
    {
      name: 'bocail-backend',
      cwd: '/www/wwwroot/bocail.com/backend',
      script: 'src/index.js',
      exec_mode: 'fork',
      instances: 1,
      autorestart: true,
      watch: false,
      max_memory_restart: '300M',
      env: {
        NODE_ENV: 'production'
      },
      output: '/www/wwwroot/bocail.com/backend/pm2-out.log',
      error: '/www/wwwroot/bocail.com/backend/pm2-err.log',
      log_date_format: 'YYYY-MM-DD HH:mm:ss'
    }
  ]
};


