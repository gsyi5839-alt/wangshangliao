<template>
  <div class="announcements-view">
    <!-- Content Grid -->
    <div class="content-grid">
      <!-- Publish Panel -->
      <div class="panel publish-panel">
        <div class="panel-header">
          <div class="panel-title">
            <svg viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
              <path d="M11 4H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
              <path d="M18.5 2.5a2.121 2.121 0 0 1 3 3L12 15l-4 1 1-4 9.5-9.5z" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
            </svg>
            <span>发布公告</span>
          </div>
        </div>
        <div class="panel-body">
          <el-form :model="form" label-position="top" class="publish-form">
            <el-form-item label="公告标题">
              <el-input v-model="form.title" placeholder="输入公告标题..." />
            </el-form-item>
            <el-form-item label="公告内容">
              <el-input
                v-model="form.content"
                type="textarea"
                :rows="8"
                placeholder="输入公告内容..."
                resize="none"
              />
            </el-form-item>
            <el-form-item label="状态">
              <div class="status-switch">
                <el-switch
                  v-model="form.isEnabled"
                  inline-prompt
                  active-text="启用"
                  inactive-text="禁用"
                  style="--el-switch-on-color: var(--primary)"
                />
                <span class="status-hint">{{ form.isEnabled ? '公告将立即显示给用户' : '公告将不会显示' }}</span>
              </div>
            </el-form-item>
            <div class="form-actions">
              <el-button type="primary" :loading="loadingPublish" @click="publish" class="publish-btn">
                <svg viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
                  <line x1="22" y1="2" x2="11" y2="13" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
                  <polygon points="22 2 15 22 11 13 2 9 22 2" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
                </svg>
                发布公告
              </el-button>
              <el-button @click="clearForm" class="clear-btn">清空</el-button>
            </div>
          </el-form>
        </div>
      </div>

      <!-- List Panel -->
      <div class="panel list-panel">
        <div class="panel-header">
          <div class="panel-title">
            <svg viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
              <path d="M18 8A6 6 0 1 0 6 8c0 7-3 9-3 9h18s-3-2-3-9" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
              <path d="M13.73 21a2 2 0 0 1-3.46 0" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
            </svg>
            <span>公告列表</span>
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
          <el-table :data="rows" max-height="520" v-loading="loadingList" class="announcements-table">
            <el-table-column prop="id" label="ID" width="70" align="center" />
            <el-table-column prop="title" label="标题" min-width="180">
              <template #default="{ row }">
                <div class="title-cell">
                  <span class="title-text">{{ row.title }}</span>
                </div>
              </template>
            </el-table-column>
            <el-table-column prop="is_enabled" label="状态" width="100" align="center">
              <template #default="{ row }">
                <span :class="['status-badge', row.is_enabled ? 'active' : 'inactive']">
                  {{ row.is_enabled ? '启用' : '禁用' }}
                </span>
              </template>
            </el-table-column>
            <el-table-column prop="created_at" label="发布时间" width="160">
              <template #default="{ row }">
                <span class="time-text">{{ formatTime(row.created_at) }}</span>
              </template>
            </el-table-column>
            <el-table-column label="操作" width="100" align="center">
              <template #default="{ row }">
                <el-button type="primary" link size="small" @click="preview(row)">查看</el-button>
              </template>
            </el-table-column>
          </el-table>
        </div>
      </div>
    </div>

    <!-- Preview Dialog -->
    <el-dialog
      v-model="previewVisible"
      :title="previewData?.title"
      width="560px"
      class="preview-dialog"
    >
      <div class="preview-content">{{ previewData?.content }}</div>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
/**
 * Announcements Management View
 * Publish and manage system announcements
 */
import { onMounted, reactive, ref } from 'vue'
import { ElMessage } from 'element-plus'
import { http } from '../utils/http'

type Row = {
  id: number
  title: string
  content: string
  is_enabled: number
  created_at: string
}

// Form state
const form = reactive({
  title: '',
  content: '',
  isEnabled: true
})
const loadingPublish = ref(false)

// List state
const rows = ref<Row[]>([])
const loadingList = ref(false)

// Preview state
const previewVisible = ref(false)
const previewData = ref<Row | null>(null)

/**
 * Publish new announcement
 */
async function publish() {
  if (!form.title.trim() || !form.content.trim()) {
    return ElMessage.warning('请填写公告标题和内容')
  }
  
  loadingPublish.value = true
  try {
    await http.post('/admin/announcements', {
      title: form.title.trim(),
      content: form.content.trim(),
      isEnabled: form.isEnabled
    })
    ElMessage.success('公告发布成功')
    clearForm()
    await load()
  } finally {
    loadingPublish.value = false
  }
}

/**
 * Load announcements list
 */
async function load() {
  loadingList.value = true
  try {
    rows.value = await http.get<Row[]>('/admin/announcements?limit=50')
  } finally {
    loadingList.value = false
  }
}

/**
 * Clear form
 */
function clearForm() {
  form.title = ''
  form.content = ''
  form.isEnabled = true
}

/**
 * Preview announcement
 */
function preview(row: Row) {
  previewData.value = row
  previewVisible.value = true
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
.announcements-view {
  display: flex;
  flex-direction: column;
  gap: 24px;
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

/* Publish Form */
.publish-form :deep(.el-form-item) {
  margin-bottom: 20px;
}

.publish-form :deep(.el-form-item__label) {
  font-weight: 500;
  color: var(--text-secondary);
  padding-bottom: 8px;
}

.publish-form :deep(.el-textarea__inner) {
  font-family: inherit;
}

.status-switch {
  display: flex;
  align-items: center;
  gap: 12px;
}

.status-hint {
  font-size: 13px;
  color: var(--text-muted);
}

.form-actions {
  display: flex;
  gap: 12px;
  margin-top: 8px;
}

.publish-btn {
  display: flex;
  align-items: center;
  gap: 8px;
}

.publish-btn svg {
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

/* Table Styles */
.title-cell {
  display: flex;
  align-items: center;
}

.title-text {
  font-weight: 500;
  color: var(--text-primary);
}

.status-badge {
  display: inline-block;
  padding: 4px 10px;
  border-radius: 10px;
  font-size: 12px;
  font-weight: 600;
}

.status-badge.active {
  background: rgba(32, 165, 58, 0.15);
  color: var(--primary);
}

.status-badge.inactive {
  background: rgba(107, 107, 107, 0.15);
  color: var(--text-muted);
}

.time-text {
  color: var(--text-secondary);
  font-size: 13px;
}

/* Preview Dialog */
.preview-dialog :deep(.el-dialog) {
  background: var(--bg-card);
  border: 1px solid var(--border-subtle);
  border-radius: var(--radius-md);
}

.preview-dialog :deep(.el-dialog__header) {
  border-bottom: 1px solid var(--border-subtle);
}

.preview-dialog :deep(.el-dialog__title) {
  color: var(--text-primary);
  font-weight: 600;
}

.preview-content {
  color: var(--text-secondary);
  line-height: 1.8;
  white-space: pre-wrap;
}
</style>
