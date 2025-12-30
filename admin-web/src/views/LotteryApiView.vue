<template>
  <div class="lottery-api-view">
    <!-- Header with Add Button -->
    <div class="page-header">
      <div class="header-left">
        <h1 class="page-title">
          <svg viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
            <path d="M12 2L2 7l10 5 10-5-10-5z" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
            <path d="M2 17l10 5 10-5" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
            <path d="M2 12l10 5 10-5" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
          </svg>
          彩票接口管理
        </h1>
        <span class="api-count">{{ apis.length }} 个接口</span>
      </div>
      <el-button type="primary" @click="openAddDialog" class="add-btn">
        <svg viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
          <line x1="12" y1="5" x2="12" y2="19" stroke="currentColor" stroke-width="2" stroke-linecap="round"/>
          <line x1="5" y1="12" x2="19" y2="12" stroke="currentColor" stroke-width="2" stroke-linecap="round"/>
        </svg>
        添加接口
      </el-button>
    </div>

    <!-- API Cards Grid -->
    <div class="api-grid" v-loading="loading">
      <div v-for="api in apis" :key="api.id" :class="['api-card', { disabled: !api.is_enabled }]">
        <div class="card-header">
          <div class="card-title">
            <span class="api-name">{{ api.name }}</span>
            <span class="api-code">{{ api.code }}</span>
          </div>
          <div class="card-status">
            <el-switch
              v-model="api.is_enabled"
              :loading="api.updating"
              @change="(val: boolean) => toggleEnabled(api, val)"
              size="small"
            />
          </div>
        </div>

        <div class="card-body">
          <div class="info-row">
            <span class="info-label">Token:</span>
            <code class="token-value">{{ maskToken(api.token) }}</code>
          </div>
          <div class="info-row">
            <span class="info-label">主接口:</span>
            <span class="url-value">{{ truncateUrl(api.api_url) }}</span>
          </div>
          <div class="info-row" v-if="api.backup_url">
            <span class="info-label">备用接口:</span>
            <span class="url-value backup">{{ truncateUrl(api.backup_url) }}</span>
          </div>
          <div class="info-row">
            <span class="info-label">请求配置:</span>
            <span class="config-tags">
              <span class="tag">{{ api.format_type.toUpperCase() }}</span>
              <span class="tag">{{ api.rows_count }}条</span>
              <span class="tag">{{ api.request_interval }}ms</span>
              <span class="tag">{{ api.max_requests_per_30s }}次/30s</span>
            </span>
          </div>
        </div>

        <div class="card-actions">
          <el-button size="small" @click="testApi(api)" :loading="api.testing" class="test-btn">
            <svg viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
              <circle cx="12" cy="12" r="10" stroke="currentColor" stroke-width="2"/>
              <polygon points="10,8 16,12 10,16" fill="currentColor"/>
            </svg>
            测试
          </el-button>
          <el-button size="small" @click="openEditDialog(api)" class="edit-btn">
            <svg viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
              <path d="M11 4H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7" stroke="currentColor" stroke-width="2" stroke-linecap="round"/>
              <path d="M18.5 2.5a2.121 2.121 0 0 1 3 3L12 15l-4 1 1-4 9.5-9.5z" stroke="currentColor" stroke-width="2" stroke-linecap="round"/>
            </svg>
            编辑
          </el-button>
          <el-button size="small" type="danger" @click="confirmDelete(api)" class="delete-btn">
            <svg viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
              <polyline points="3 6 5 6 21 6" stroke="currentColor" stroke-width="2" stroke-linecap="round"/>
              <path d="M19 6v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6m3 0V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2" stroke="currentColor" stroke-width="2" stroke-linecap="round"/>
            </svg>
            删除
          </el-button>
        </div>
      </div>

      <!-- Empty State -->
      <div v-if="!loading && apis.length === 0" class="empty-state">
        <svg viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
          <rect x="3" y="3" width="18" height="18" rx="2" stroke="currentColor" stroke-width="2"/>
          <line x1="12" y1="8" x2="12" y2="16" stroke="currentColor" stroke-width="2" stroke-linecap="round"/>
          <line x1="8" y1="12" x2="16" y2="12" stroke="currentColor" stroke-width="2" stroke-linecap="round"/>
        </svg>
        <p>暂无彩票接口，点击上方按钮添加</p>
      </div>
    </div>

    <!-- Add/Edit Dialog -->
    <el-dialog
      v-model="dialogVisible"
      :title="editingApi ? '编辑接口' : '添加接口'"
      width="600px"
      class="api-dialog"
    >
      <el-form :model="formData" label-width="100px" class="api-form">
        <el-form-item label="名称" required>
          <el-input v-model="formData.name" placeholder="例如：加拿大28" />
        </el-form-item>
        <el-form-item label="彩票代码" required>
          <el-input v-model="formData.code" placeholder="例如：jnd28" />
        </el-form-item>
        <el-form-item label="Token" required>
          <el-input v-model="formData.token" placeholder="bcapi.cn 的 token" />
        </el-form-item>
        <el-form-item label="主接口 URL" required>
          <el-input
            v-model="formData.api_url"
            placeholder="https://bcapi.cn/token/{token}/code/{code}/rows/{rows}.{format}"
          />
          <div class="form-hint">支持变量: {token}, {code}, {rows}, {format}</div>
        </el-form-item>
        <el-form-item label="备用接口 URL">
          <el-input
            v-model="formData.backup_url"
            placeholder="备用接口地址（可选）"
          />
        </el-form-item>
        <el-form-item label="返回格式">
          <el-radio-group v-model="formData.format_type">
            <el-radio value="json">JSON</el-radio>
            <el-radio value="jsonp">JSONP</el-radio>
            <el-radio value="xml">XML</el-radio>
          </el-radio-group>
        </el-form-item>
        <el-form-item label="JSONP 回调" v-if="formData.format_type === 'jsonp'">
          <el-input v-model="formData.callback_name" placeholder="jsonpReturn" />
        </el-form-item>
        <el-form-item label="请求行数">
          <el-input-number v-model="formData.rows_count" :min="1" :max="20" />
        </el-form-item>
        <el-form-item label="请求间隔">
          <el-input-number v-model="formData.request_interval" :min="100" :max="60000" :step="100" />
          <span class="unit">毫秒</span>
        </el-form-item>
        <el-form-item label="30秒最大请求">
          <el-input-number v-model="formData.max_requests_per_30s" :min="1" :max="100" />
          <span class="unit">次</span>
        </el-form-item>
        <el-form-item label="备注">
          <el-input v-model="formData.remark" type="textarea" :rows="2" placeholder="接口说明（可选）" />
        </el-form-item>
      </el-form>

      <template #footer>
        <el-button @click="dialogVisible = false">取消</el-button>
        <el-button type="primary" @click="saveApi" :loading="saving">
          {{ editingApi ? '保存修改' : '添加接口' }}
        </el-button>
      </template>
    </el-dialog>

    <!-- Test Result Dialog -->
    <el-dialog v-model="testDialogVisible" title="接口测试结果" width="700px" class="test-dialog">
      <div v-if="testResult" class="test-result">
        <div :class="['test-status', testResult.success ? 'success' : 'failed']">
          <svg v-if="testResult.success" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
            <circle cx="12" cy="12" r="10" stroke="currentColor" stroke-width="2"/>
            <polyline points="8 12 11 15 16 9" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
          </svg>
          <svg v-else viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
            <circle cx="12" cy="12" r="10" stroke="currentColor" stroke-width="2"/>
            <line x1="15" y1="9" x2="9" y2="15" stroke="currentColor" stroke-width="2" stroke-linecap="round"/>
            <line x1="9" y1="9" x2="15" y2="15" stroke="currentColor" stroke-width="2" stroke-linecap="round"/>
          </svg>
          <span>{{ testResult.success ? '连接成功' : '连接失败' }}</span>
        </div>
        <div class="test-url">
          <span class="label">请求URL:</span>
          <code>{{ testResult.url }}</code>
        </div>
        <div v-if="testResult.error" class="test-error">
          <span class="label">错误信息:</span>
          <code>{{ testResult.error }}</code>
        </div>
        <div v-if="testResult.response" class="test-response">
          <span class="label">返回数据:</span>
          <pre>{{ JSON.stringify(testResult.response, null, 2) }}</pre>
        </div>
      </div>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
/**
 * Lottery API Management View
 * Manage bcapi.cn lottery data endpoints
 */
import { onMounted, reactive, ref } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { http } from '../utils/http'

interface LotteryApi {
  id: number
  name: string
  code: string
  token: string
  api_url: string
  backup_url: string | null
  format_type: 'json' | 'jsonp' | 'xml'
  callback_name: string | null
  rows_count: number
  request_interval: number
  max_requests_per_30s: number
  is_enabled: boolean
  remark: string | null
  created_at: string
  updated_at: string | null
  // Local UI states
  testing?: boolean
  updating?: boolean
}

interface TestResult {
  success: boolean
  url: string
  error?: string
  response?: unknown
}

// State
const apis = ref<LotteryApi[]>([])
const loading = ref(false)
const dialogVisible = ref(false)
const testDialogVisible = ref(false)
const editingApi = ref<LotteryApi | null>(null)
const saving = ref(false)
const testResult = ref<TestResult | null>(null)

// Form data - 默认值符合官方限制：30秒40次，间隔>=1秒
const defaultForm = {
  name: '',
  code: '',
  token: '',
  api_url: 'https://bcapi.cn/token/{token}/code/{code}/rows/{rows}.{format}',
  backup_url: '',
  format_type: 'json' as const,
  callback_name: 'jsonpReturn',
  rows_count: 1,
  request_interval: 1000,
  max_requests_per_30s: 40,
  remark: ''
}
const formData = reactive({ ...defaultForm })

/**
 * Load all lottery APIs
 */
async function loadApis() {
  loading.value = true
  try {
    apis.value = await http.get<LotteryApi[]>('/admin/lottery-apis')
  } finally {
    loading.value = false
  }
}

/**
 * Open add dialog
 */
function openAddDialog() {
  editingApi.value = null
  Object.assign(formData, defaultForm)
  dialogVisible.value = true
}

/**
 * Open edit dialog
 */
function openEditDialog(api: LotteryApi) {
  editingApi.value = api
  Object.assign(formData, {
    name: api.name,
    code: api.code,
    token: api.token,
    api_url: api.api_url,
    backup_url: api.backup_url || '',
    format_type: api.format_type,
    callback_name: api.callback_name || 'jsonpReturn',
    rows_count: api.rows_count,
    request_interval: api.request_interval,
    max_requests_per_30s: api.max_requests_per_30s,
    remark: api.remark || ''
  })
  dialogVisible.value = true
}

/**
 * Save API (create or update)
 */
async function saveApi() {
  if (!formData.name || !formData.code || !formData.token || !formData.api_url) {
    ElMessage.warning('请填写必填字段')
    return
  }

  saving.value = true
  try {
    const payload = {
      name: formData.name,
      code: formData.code,
      token: formData.token,
      api_url: formData.api_url,
      backup_url: formData.backup_url || null,
      format_type: formData.format_type,
      callback_name: formData.callback_name || null,
      rows_count: formData.rows_count,
      request_interval: formData.request_interval,
      max_requests_per_30s: formData.max_requests_per_30s,
      remark: formData.remark || null
    }

    if (editingApi.value) {
      await http.put(`/admin/lottery-apis/${editingApi.value.id}`, payload)
      ElMessage.success('接口已更新')
    } else {
      await http.post('/admin/lottery-apis', { ...payload, is_enabled: true })
      ElMessage.success('接口已添加')
    }

    dialogVisible.value = false
    await loadApis()
  } finally {
    saving.value = false
  }
}

/**
 * Toggle API enabled status
 */
async function toggleEnabled(api: LotteryApi, enabled: boolean) {
  api.updating = true
  try {
    await http.put(`/admin/lottery-apis/${api.id}`, { is_enabled: enabled })
    ElMessage.success(enabled ? '已启用' : '已禁用')
  } catch {
    api.is_enabled = !enabled // Revert on error
  } finally {
    api.updating = false
  }
}

/**
 * Test API connection
 */
async function testApi(api: LotteryApi) {
  api.testing = true
  try {
    const result = await http.post<TestResult>(`/admin/lottery-apis/${api.id}/test`, {})
    testResult.value = result
    testDialogVisible.value = true
  } finally {
    api.testing = false
  }
}

/**
 * Confirm and delete API
 */
async function confirmDelete(api: LotteryApi) {
  try {
    await ElMessageBox.confirm(
      `确定要删除接口 "${api.name}" 吗？此操作不可恢复。`,
      '确认删除',
      { type: 'warning' }
    )
    await http.delete(`/admin/lottery-apis/${api.id}`)
    ElMessage.success('已删除')
    await loadApis()
  } catch {
    // User cancelled
  }
}

/**
 * Mask token for display
 */
function maskToken(token: string): string {
  if (token.length <= 8) return token
  return token.slice(0, 4) + '****' + token.slice(-4)
}

/**
 * Truncate URL for display
 */
function truncateUrl(url: string): string {
  if (url.length <= 50) return url
  return url.slice(0, 47) + '...'
}

onMounted(loadApis)
</script>

<style scoped>
.lottery-api-view {
  display: flex;
  flex-direction: column;
  gap: 24px;
}

/* Page Header */
.page-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding-bottom: 16px;
  border-bottom: 1px solid var(--border-subtle);
}

.header-left {
  display: flex;
  align-items: center;
  gap: 16px;
}

.page-title {
  display: flex;
  align-items: center;
  gap: 10px;
  font-size: 20px;
  font-weight: 700;
  color: var(--text-primary);
  margin: 0;
}

.page-title svg {
  width: 24px;
  height: 24px;
  color: var(--primary);
}

.api-count {
  font-size: 13px;
  font-weight: 500;
  color: var(--text-muted);
  background: var(--bg-input);
  padding: 4px 12px;
  border-radius: 12px;
}

.add-btn {
  display: flex;
  align-items: center;
  gap: 8px;
}

.add-btn svg {
  width: 16px;
  height: 16px;
}

/* API Grid */
.api-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(400px, 1fr));
  gap: 20px;
}

/* API Card */
.api-card {
  background: var(--bg-card);
  border: 1px solid var(--border-subtle);
  border-radius: var(--radius-md);
  overflow: hidden;
  transition: all var(--transition-fast);
}

.api-card:hover {
  border-color: var(--primary);
  box-shadow: 0 0 20px var(--primary-glow);
}

.api-card.disabled {
  opacity: 0.6;
}

.api-card.disabled .card-header {
  background: var(--bg-input);
}

.card-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 16px 20px;
  background: var(--bg-elevated);
  border-bottom: 1px solid var(--border-subtle);
}

.card-title {
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.api-name {
  font-weight: 600;
  color: var(--text-primary);
  font-size: 15px;
}

.api-code {
  font-family: 'SF Mono', 'Consolas', monospace;
  font-size: 12px;
  color: var(--primary);
  background: rgba(32, 165, 58, 0.1);
  padding: 2px 6px;
  border-radius: 4px;
  display: inline-block;
}

.card-body {
  padding: 16px 20px;
  display: flex;
  flex-direction: column;
  gap: 10px;
}

.info-row {
  display: flex;
  align-items: flex-start;
  gap: 10px;
}

.info-label {
  font-size: 12px;
  color: var(--text-muted);
  min-width: 60px;
  flex-shrink: 0;
}

.token-value {
  font-family: 'SF Mono', 'Consolas', monospace;
  font-size: 12px;
  color: var(--text-secondary);
  background: var(--bg-input);
  padding: 2px 6px;
  border-radius: 4px;
}

.url-value {
  font-size: 12px;
  color: var(--text-secondary);
  word-break: break-all;
}

.url-value.backup {
  color: var(--text-muted);
}

.config-tags {
  display: flex;
  flex-wrap: wrap;
  gap: 6px;
}

.tag {
  font-size: 11px;
  padding: 2px 8px;
  background: var(--bg-input);
  color: var(--text-secondary);
  border-radius: 10px;
}

.card-actions {
  display: flex;
  gap: 8px;
  padding: 12px 20px;
  border-top: 1px solid var(--border-subtle);
  background: var(--bg-elevated);
}

.card-actions .el-button {
  display: flex;
  align-items: center;
  gap: 4px;
}

.card-actions svg {
  width: 14px;
  height: 14px;
}

/* Empty State */
.empty-state {
  grid-column: 1 / -1;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: 60px 20px;
  color: var(--text-muted);
}

.empty-state svg {
  width: 48px;
  height: 48px;
  margin-bottom: 16px;
  opacity: 0.5;
}

/* Dialog Styles */
.api-dialog :deep(.el-dialog__body) {
  padding: 20px 24px;
}

.api-form .form-hint {
  font-size: 12px;
  color: var(--text-muted);
  margin-top: 4px;
}

.api-form .unit {
  margin-left: 8px;
  color: var(--text-muted);
  font-size: 13px;
}

/* Test Dialog */
.test-result {
  display: flex;
  flex-direction: column;
  gap: 16px;
}

.test-status {
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 12px 16px;
  border-radius: 8px;
  font-weight: 600;
}

.test-status svg {
  width: 24px;
  height: 24px;
}

.test-status.success {
  background: rgba(32, 165, 58, 0.15);
  color: var(--primary);
}

.test-status.failed {
  background: rgba(229, 57, 53, 0.15);
  color: var(--danger);
}

.test-url,
.test-error {
  display: flex;
  flex-direction: column;
  gap: 6px;
}

.test-url .label,
.test-error .label,
.test-response .label {
  font-size: 12px;
  font-weight: 600;
  color: var(--text-muted);
}

.test-url code,
.test-error code {
  font-family: 'SF Mono', 'Consolas', monospace;
  font-size: 12px;
  background: var(--bg-input);
  padding: 8px 12px;
  border-radius: 6px;
  word-break: break-all;
}

.test-error code {
  color: var(--danger);
}

.test-response {
  display: flex;
  flex-direction: column;
  gap: 6px;
}

.test-response pre {
  font-family: 'SF Mono', 'Consolas', monospace;
  font-size: 12px;
  background: var(--bg-input);
  padding: 12px;
  border-radius: 6px;
  max-height: 300px;
  overflow: auto;
  margin: 0;
  white-space: pre-wrap;
  word-break: break-all;
}
</style>

