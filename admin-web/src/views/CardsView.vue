<template>
  <div class="cards-view">
    <!-- Stats Cards -->
    <div class="stats-grid">
      <div class="stat-card">
        <div class="stat-icon">
          <svg viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
            <rect x="3" y="5" width="18" height="14" rx="2" stroke="currentColor" stroke-width="2"/>
            <path d="M3 10H21" stroke="currentColor" stroke-width="2"/>
          </svg>
        </div>
        <div class="stat-info">
          <span class="stat-value">{{ totalCards }}</span>
          <span class="stat-label">总充值卡数</span>
        </div>
      </div>
      <div class="stat-card">
        <div class="stat-icon used">
          <svg viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
            <path d="M22 11.08V12a10 10 0 1 1-5.93-9.14" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
            <polyline points="22 4 12 14.01 9 11.01" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
          </svg>
        </div>
        <div class="stat-info">
          <span class="stat-value">{{ usedCards }}</span>
          <span class="stat-label">已使用</span>
        </div>
      </div>
      <div class="stat-card">
        <div class="stat-icon available">
          <svg viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
            <circle cx="12" cy="12" r="10" stroke="currentColor" stroke-width="2"/>
            <line x1="12" y1="8" x2="12" y2="16" stroke="currentColor" stroke-width="2" stroke-linecap="round"/>
            <line x1="8" y1="12" x2="16" y2="12" stroke="currentColor" stroke-width="2" stroke-linecap="round"/>
          </svg>
        </div>
        <div class="stat-info">
          <span class="stat-value">{{ availableCards }}</span>
          <span class="stat-label">可用</span>
        </div>
      </div>
    </div>

    <!-- Main Content Grid -->
    <div class="content-grid">
      <!-- Generate Cards Panel -->
      <div class="panel generate-panel">
        <div class="panel-header">
          <div class="panel-title">
            <svg viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
              <line x1="12" y1="5" x2="12" y2="19" stroke="currentColor" stroke-width="2" stroke-linecap="round"/>
              <line x1="5" y1="12" x2="19" y2="12" stroke="currentColor" stroke-width="2" stroke-linecap="round"/>
            </svg>
            <span>批量生成充值卡</span>
          </div>
        </div>
        <div class="panel-body">
          <el-form :model="create" label-position="top" class="generate-form">
            <div class="form-row">
              <el-form-item label="生成数量">
                <el-input-number v-model="create.count" :min="1" :max="500" :step="10" />
              </el-form-item>
              <el-form-item label="卡片天数">
                <el-select v-model="create.days" class="days-select">
                  <el-option :value="1" label="1 天" />
                  <el-option :value="3" label="3 天" />
                  <el-option :value="7" label="7 天" />
                  <el-option :value="15" label="15 天" />
                  <el-option :value="30" label="30 天" />
                </el-select>
              </el-form-item>
            </div>
            <div class="form-actions">
              <el-button type="primary" :loading="loadingCreate" @click="createCards" class="generate-btn">
                <svg viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
                  <path d="M12 2v4M12 18v4M4.93 4.93l2.83 2.83M16.24 16.24l2.83 2.83M2 12h4M18 12h4M4.93 19.07l2.83-2.83M16.24 7.76l2.83-2.83" stroke="currentColor" stroke-width="2" stroke-linecap="round"/>
                </svg>
                生成充值卡
              </el-button>
              <el-button :disabled="created.length === 0" @click="copyCreated" class="copy-btn">
                <svg viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
                  <rect x="9" y="9" width="13" height="13" rx="2" stroke="currentColor" stroke-width="2"/>
                  <path d="M5 15H4a2 2 0 0 1-2-2V4a2 2 0 0 1 2-2h9a2 2 0 0 1 2 2v1" stroke="currentColor" stroke-width="2"/>
                </svg>
                复制全部
              </el-button>
            </div>
          </el-form>

          <!-- Generated Cards Result -->
          <div v-if="created.length > 0" class="generated-result">
            <div class="result-header">
              <span class="result-title">已生成 {{ created.length }} 张</span>
              <span class="result-hint">点击复制全部可直接粘贴使用</span>
            </div>
            <div class="cards-table-wrapper">
              <el-table :data="created" size="small" max-height="240" class="cards-table">
                <el-table-column prop="code" label="卡号" min-width="240">
                  <template #default="{ row }">
                    <code class="card-code">{{ row.code }}</code>
                  </template>
                </el-table-column>
                <el-table-column prop="password" label="卡密" min-width="180">
                  <template #default="{ row }">
                    <code class="card-password">{{ row.password }}</code>
                  </template>
                </el-table-column>
                <el-table-column prop="days" label="天数" width="80" align="center">
                  <template #default="{ row }">
                    <span class="days-badge">{{ row.days }}天</span>
                  </template>
                </el-table-column>
              </el-table>
            </div>
          </div>
        </div>
      </div>

      <!-- Cards List Panel -->
      <div class="panel list-panel">
        <div class="panel-header">
          <div class="panel-title">
            <svg viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
              <path d="M8 6h13M8 12h13M8 18h13M3 6h.01M3 12h.01M3 18h.01" stroke="currentColor" stroke-width="2" stroke-linecap="round"/>
            </svg>
            <span>充值卡列表</span>
          </div>
          <el-button size="small" @click="loadCards" class="refresh-btn">
            <svg viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
              <polyline points="23 4 23 10 17 10" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
              <path d="M20.49 15a9 9 0 1 1-2.12-9.36L23 10" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
            </svg>
            刷新
          </el-button>
        </div>
        <div class="panel-body">
          <el-table :data="cards" max-height="520" v-loading="loadingList" class="cards-table">
            <el-table-column prop="card_code" label="卡号" min-width="240">
              <template #default="{ row }">
                <code class="card-code">{{ row.card_code }}</code>
              </template>
            </el-table-column>
            <el-table-column prop="days" label="天数" width="80" align="center">
              <template #default="{ row }">
                <span class="days-badge">{{ row.days }}天</span>
              </template>
            </el-table-column>
            <el-table-column prop="used_by" label="使用者" width="140">
              <template #default="{ row }">
                <span v-if="row.used_by" class="user-tag">{{ row.used_by }}</span>
                <span v-else class="unused-tag">未使用</span>
              </template>
            </el-table-column>
            <el-table-column prop="used_at" label="使用时间" width="180">
              <template #default="{ row }">
                <span v-if="row.used_at" class="time-text">{{ formatTime(row.used_at) }}</span>
                <span v-else class="time-placeholder">-</span>
              </template>
            </el-table-column>
          </el-table>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
/**
 * Cards Management View
 * Generate and manage recharge cards
 */
import { computed, onMounted, reactive, ref } from 'vue'
import { ElMessage } from 'element-plus'
import { http } from '../utils/http'

type CardRow = {
  id: number
  card_code: string
  days: number
  used_by: string | null
  used_at: string | null
}

type GeneratedCard = {
  code: string
  password: string
  days: number
}

// Form state
const create = reactive({ count: 10, days: 30 })
const created = ref<GeneratedCard[]>([])
const loadingCreate = ref(false)

// List state
const cards = ref<CardRow[]>([])
const loadingList = ref(false)

// Computed stats
const totalCards = computed(() => cards.value.length)
const usedCards = computed(() => cards.value.filter(c => c.used_by).length)
const availableCards = computed(() => cards.value.filter(c => !c.used_by).length)

/**
 * Generate new recharge cards
 */
async function createCards() {
  loadingCreate.value = true
  try {
    const data = await http.post<GeneratedCard[]>('/admin/recharge-cards', {
      count: create.count,
      days: create.days
    })
    created.value = data
    ElMessage.success(`成功生成 ${data.length} 张充值卡`)
    await loadCards()
  } finally {
    loadingCreate.value = false
  }
}

/**
 * Load cards list from server
 */
async function loadCards() {
  loadingList.value = true
  try {
    cards.value = await http.get<CardRow[]>('/admin/recharge-cards?limit=200')
  } finally {
    loadingList.value = false
  }
}

/**
 * Copy generated cards to clipboard
 */
async function copyCreated() {
  const txt = created.value.map(c => `${c.code} ${c.password}`).join('\n')
  await navigator.clipboard.writeText(txt)
  ElMessage.success('已复制到剪贴板')
}

/**
 * Format datetime string
 */
function formatTime(dateStr: string): string {
  const date = new Date(dateStr)
  return date.toLocaleString('zh-CN', {
    month: '2-digit',
    day: '2-digit',
    hour: '2-digit',
    minute: '2-digit'
  })
}

onMounted(loadCards)
</script>

<style scoped>
.cards-view {
  display: flex;
  flex-direction: column;
  gap: 24px;
}

/* Stats Grid */
.stats-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
  gap: 16px;
}

.stat-card {
  background: var(--bg-card);
  border: 1px solid var(--border-subtle);
  border-radius: var(--radius-md);
  padding: 20px;
  display: flex;
  align-items: center;
  gap: 16px;
  transition: all var(--transition-fast);
}

.stat-card:hover {
  border-color: var(--primary);
  box-shadow: 0 0 20px var(--primary-glow);
}

.stat-icon {
  width: 48px;
  height: 48px;
  background: rgba(32, 165, 58, 0.15);
  border-radius: var(--radius-sm);
  display: flex;
  align-items: center;
  justify-content: center;
  color: var(--primary);
}

.stat-icon.used {
  background: rgba(229, 57, 53, 0.15);
  color: var(--danger);
}

.stat-icon.available {
  background: rgba(38, 166, 154, 0.15);
  color: var(--info);
}

.stat-icon svg {
  width: 24px;
  height: 24px;
}

.stat-info {
  display: flex;
  flex-direction: column;
}

.stat-value {
  font-size: 28px;
  font-weight: 700;
  color: var(--text-primary);
  line-height: 1;
}

.stat-label {
  font-size: 13px;
  color: var(--text-muted);
  margin-top: 4px;
}

/* Content Grid */
.content-grid {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 24px;
}

@media (max-width: 1200px) {
  .content-grid {
    grid-template-columns: 1fr;
  }
}

/* Panel Styles */
.panel {
  background: var(--bg-card);
  border: 1px solid var(--border-subtle);
  border-radius: var(--radius-md);
  overflow: hidden;
}

.panel-header {
  padding: 16px 20px;
  border-bottom: 1px solid var(--border-subtle);
  display: flex;
  align-items: center;
  justify-content: space-between;
  background: var(--bg-elevated);
}

.panel-title {
  display: flex;
  align-items: center;
  gap: 10px;
  font-weight: 600;
  color: var(--text-primary);
}

.panel-title svg {
  width: 18px;
  height: 18px;
  color: var(--primary);
}

.panel-body {
  padding: 20px;
}

/* Generate Form */
.generate-form {
  display: flex;
  flex-direction: column;
  gap: 20px;
}

.form-row {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 16px;
}

.form-row :deep(.el-form-item) {
  margin-bottom: 0;
}

.form-row :deep(.el-form-item__label) {
  font-weight: 500;
  color: var(--text-secondary);
  padding-bottom: 8px;
}

.days-select {
  width: 100%;
}

.form-actions {
  display: flex;
  gap: 12px;
}

.generate-btn,
.copy-btn {
  display: flex;
  align-items: center;
  gap: 8px;
}

.generate-btn svg,
.copy-btn svg {
  width: 16px;
  height: 16px;
}

.refresh-btn {
  display: flex;
  align-items: center;
  gap: 6px;
}

.refresh-btn svg {
  width: 14px;
  height: 14px;
}

/* Generated Result */
.generated-result {
  margin-top: 24px;
  padding-top: 24px;
  border-top: 1px solid var(--border-subtle);
}

.result-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 16px;
}

.result-title {
  font-weight: 600;
  color: var(--primary);
}

.result-hint {
  font-size: 12px;
  color: var(--text-muted);
}

/* Table Styles */
.cards-table-wrapper {
  border: 1px solid var(--border-subtle);
  border-radius: var(--radius-sm);
  overflow: hidden;
}

.cards-table {
  --el-table-border-color: var(--border-subtle);
}

.card-code {
  font-family: 'SF Mono', 'Consolas', monospace;
  font-size: 12px;
  color: var(--text-primary);
  background: var(--bg-input);
  padding: 4px 8px;
  border-radius: 4px;
}

.card-password {
  font-family: 'SF Mono', 'Consolas', monospace;
  font-size: 12px;
  color: var(--primary);
  background: rgba(32, 165, 58, 0.1);
  padding: 4px 8px;
  border-radius: 4px;
}

.days-badge {
  display: inline-block;
  padding: 2px 8px;
  background: rgba(32, 165, 58, 0.15);
  color: var(--primary);
  border-radius: 10px;
  font-size: 12px;
  font-weight: 600;
}

.user-tag {
  color: var(--text-primary);
  font-weight: 500;
}

.unused-tag {
  color: var(--text-muted);
  font-style: italic;
}

.time-text {
  color: var(--text-secondary);
  font-size: 13px;
}

.time-placeholder {
  color: var(--text-disabled);
}
</style>
