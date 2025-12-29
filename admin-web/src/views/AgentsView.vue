<template>
  <div class="agents-view">
    <!-- Content Grid -->
    <div class="content-grid">
      <!-- Create Agent Panel -->
      <div class="panel create-panel">
        <div class="panel-header">
          <div class="panel-title">
            <svg viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
              <rect x="4" y="4" width="16" height="16" rx="2" stroke="currentColor" stroke-width="2"/>
              <rect x="9" y="9" width="6" height="6" stroke="currentColor" stroke-width="2"/>
              <line x1="9" y1="1" x2="9" y2="4" stroke="currentColor" stroke-width="2"/>
              <line x1="15" y1="1" x2="15" y2="4" stroke="currentColor" stroke-width="2"/>
              <line x1="9" y1="20" x2="9" y2="23" stroke="currentColor" stroke-width="2"/>
              <line x1="15" y1="20" x2="15" y2="23" stroke="currentColor" stroke-width="2"/>
            </svg>
            <span>创建 Agent</span>
          </div>
        </div>
        <div class="panel-body">
          <el-form :model="form" label-position="top" class="create-form">
            <el-form-item label="Agent 名称">
              <el-input v-model="form.name" placeholder="输入 Agent 名称..." />
            </el-form-item>
            <el-form-item label="备注说明">
              <el-input v-model="form.description" placeholder="可选的备注信息..." />
            </el-form-item>
            <div class="form-actions">
              <el-button type="primary" :loading="loadingCreate" @click="create" class="create-btn">
                <svg viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
                  <line x1="12" y1="5" x2="12" y2="19" stroke="currentColor" stroke-width="2" stroke-linecap="round"/>
                  <line x1="5" y1="12" x2="19" y2="12" stroke="currentColor" stroke-width="2" stroke-linecap="round"/>
                </svg>
                创建 Agent
              </el-button>
            </div>
          </el-form>

          <!-- Created Agent Result -->
          <div v-if="created" class="created-result">
            <div class="result-header">
              <svg viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
                <path d="M22 11.08V12a10 10 0 1 1-5.93-9.14" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
                <polyline points="22 4 12 14.01 9 11.01" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
              </svg>
              <span>创建成功！请保存以下密钥</span>
            </div>
            <div class="key-display">
              <span class="key-label">Agent Key (X-Agent-Key)</span>
              <div class="key-value">
                <code>{{ created.agent_key }}</code>
                <el-button size="small" @click="copyKey" class="copy-btn">
                  <svg viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
                    <rect x="9" y="9" width="13" height="13" rx="2" stroke="currentColor" stroke-width="2"/>
                    <path d="M5 15H4a2 2 0 0 1-2-2V4a2 2 0 0 1 2-2h9a2 2 0 0 1 2 2v1" stroke="currentColor" stroke-width="2"/>
                  </svg>
                </el-button>
              </div>
              <span class="key-hint">此密钥仅显示一次，请妥善保存</span>
            </div>
          </div>
        </div>
      </div>

      <!-- Agents List Panel -->
      <div class="panel list-panel">
        <div class="panel-header">
          <div class="panel-title">
            <svg viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
              <path d="M8 6h13M8 12h13M8 18h13M3 6h.01M3 12h.01M3 18h.01" stroke="currentColor" stroke-width="2" stroke-linecap="round"/>
            </svg>
            <span>Agent 列表</span>
          </div>
          <el-button size="small" @click="load" class="refresh-btn">
            <svg viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
              <polyline points="23 4 23 10 17 10" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
              <path d="M20.49 15a9 9 0 1 1-2.12-9.36L23 10" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
            </svg>
            刷新
          </el-button>
        </div>
        <div class="panel-body">
          <el-table :data="rows" max-height="480" v-loading="loadingList" class="agents-table">
            <el-table-column prop="id" label="ID" width="70" align="center" />
            <el-table-column prop="name" label="名称" min-width="160">
              <template #default="{ row }">
                <div class="agent-name">
                  <div class="agent-avatar">
                    {{ row.name.charAt(0).toUpperCase() }}
                  </div>
                  <span>{{ row.name }}</span>
                </div>
              </template>
            </el-table-column>
            <el-table-column prop="status" label="状态" width="100" align="center">
              <template #default="{ row }">
                <span :class="['status-badge', row.status === 'online' ? 'online' : 'offline']">
                  <span class="status-dot"></span>
                  {{ row.status === 'online' ? '在线' : '离线' }}
                </span>
              </template>
            </el-table-column>
            <el-table-column prop="last_seen_at" label="最后在线" width="160">
              <template #default="{ row }">
                <span v-if="row.last_seen_at" class="time-text">{{ formatTime(row.last_seen_at) }}</span>
                <span v-else class="time-placeholder">从未连接</span>
              </template>
            </el-table-column>
            <el-table-column label="操作" width="100" align="center">
              <template #default="{ row }">
                <el-popconfirm title="确定删除此 Agent？" @confirm="remove(row.id)">
                  <template #reference>
                    <el-button type="danger" link size="small">删除</el-button>
                  </template>
                </el-popconfirm>
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
 * Agents Management View
 * Create and manage desktop agents
 */
import { onMounted, reactive, ref } from 'vue'
import { ElMessage } from 'element-plus'
import { http } from '../utils/http'

type Agent = {
  id: number
  name: string
  agent_key?: string
  description?: string | null
  last_seen_at?: string | null
  status?: string
}

// Form state
const form = reactive({
  name: '',
  description: ''
})
const created = ref<Agent | null>(null)
const loadingCreate = ref(false)

// List state
const rows = ref<Agent[]>([])
const loadingList = ref(false)

/**
 * Create new agent
 */
async function create() {
  if (!form.name.trim()) {
    return ElMessage.warning('请输入 Agent 名称')
  }
  
  loadingCreate.value = true
  try {
    created.value = await http.post<Agent>('/admin/agents', {
      name: form.name.trim(),
      description: form.description.trim() || null
    })
    ElMessage.success('Agent 创建成功')
    form.name = ''
    form.description = ''
    await load()
  } finally {
    loadingCreate.value = false
  }
}

/**
 * Load agents list
 */
async function load() {
  loadingList.value = true
  try {
    rows.value = await http.get<Agent[]>('/admin/agents')
  } finally {
    loadingList.value = false
  }
}

/**
 * Remove agent
 */
async function remove(id: number) {
  await http.del(`/admin/agents/${id}`)
  ElMessage.success('Agent 已删除')
  await load()
}

/**
 * Copy agent key to clipboard
 */
async function copyKey() {
  if (!created.value?.agent_key) return
  await navigator.clipboard.writeText(created.value.agent_key)
  ElMessage.success('已复制到剪贴板')
}

/**
 * Format datetime
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

onMounted(load)
</script>

<style scoped>
.agents-view {
  display: flex;
  flex-direction: column;
  gap: 24px;
}

/* Content Grid */
.content-grid {
  display: grid;
  grid-template-columns: minmax(320px, 400px) 1fr;
  gap: 24px;
}

@media (max-width: 1024px) {
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

/* Create Form */
.create-form :deep(.el-form-item) {
  margin-bottom: 20px;
}

.create-form :deep(.el-form-item__label) {
  font-weight: 500;
  color: var(--text-secondary);
  padding-bottom: 8px;
}

.form-actions {
  margin-top: 8px;
}

.create-btn {
  display: flex;
  align-items: center;
  gap: 8px;
}

.create-btn svg {
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

/* Created Result */
.created-result {
  margin-top: 24px;
  padding: 20px;
  background: rgba(32, 165, 58, 0.08);
  border: 1px solid rgba(32, 165, 58, 0.3);
  border-radius: var(--radius-sm);
}

.result-header {
  display: flex;
  align-items: center;
  gap: 10px;
  color: var(--primary);
  font-weight: 600;
  margin-bottom: 16px;
}

.result-header svg {
  width: 20px;
  height: 20px;
}

.key-display {
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.key-label {
  font-size: 12px;
  font-weight: 600;
  color: var(--text-muted);
  text-transform: uppercase;
  letter-spacing: 0.5px;
}

.key-value {
  display: flex;
  align-items: center;
  gap: 12px;
}

.key-value code {
  flex: 1;
  padding: 12px;
  background: var(--bg-input);
  border: 1px solid var(--border-default);
  border-radius: var(--radius-sm);
  font-family: 'SF Mono', 'Consolas', monospace;
  font-size: 13px;
  color: var(--text-primary);
  word-break: break-all;
}

.copy-btn {
  flex-shrink: 0;
}

.copy-btn svg {
  width: 16px;
  height: 16px;
}

.key-hint {
  font-size: 12px;
  color: var(--danger);
}

/* Table Styles */
.agent-name {
  display: flex;
  align-items: center;
  gap: 10px;
}

.agent-avatar {
  width: 32px;
  height: 32px;
  background: linear-gradient(135deg, var(--primary) 0%, var(--primary-dark) 100%);
  border-radius: 8px;
  display: flex;
  align-items: center;
  justify-content: center;
  font-weight: 700;
  font-size: 14px;
  color: white;
}

.status-badge {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  padding: 4px 10px;
  border-radius: 10px;
  font-size: 12px;
  font-weight: 600;
}

.status-badge.online {
  background: rgba(32, 165, 58, 0.15);
  color: var(--primary);
}

.status-badge.offline {
  background: rgba(107, 107, 107, 0.15);
  color: var(--text-muted);
}

.status-dot {
  width: 6px;
  height: 6px;
  border-radius: 50%;
  background: currentColor;
}

.status-badge.online .status-dot {
  animation: pulse 2s infinite;
}

@keyframes pulse {
  0% { box-shadow: 0 0 0 0 rgba(32, 165, 58, 0.6); }
  70% { box-shadow: 0 0 0 6px rgba(32, 165, 58, 0); }
  100% { box-shadow: 0 0 0 0 rgba(32, 165, 58, 0); }
}

.time-text {
  color: var(--text-secondary);
  font-size: 13px;
}

.time-placeholder {
  color: var(--text-disabled);
  font-style: italic;
}
</style>
