const express = require('express');
const { adminAuth } = require('../middlewares/adminAuth');

const router = express.Router();

router.get('/admins/me', adminAuth, (req, res) => {
  res.json({ ok: true, data: { id: req.admin.id, username: req.admin.username } });
});

module.exports = { router };


