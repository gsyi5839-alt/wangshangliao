const express = require('express');

const router = express.Router();

// Simple liveness endpoint for Nginx/BT checks.
router.get('/health', (req, res) => {
  res.json({ ok: true, ts: new Date().toISOString() });
});

module.exports = { router };


