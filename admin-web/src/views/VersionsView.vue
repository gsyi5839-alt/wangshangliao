<template>
  <div class="versions-view">
    <!-- Content Grid -->
    <div class="content-grid">
      <!-- Add Version Panel -->
      <div class="panel add-panel">
        <div class="panel-header">
          <div class="panel-title">
            <svg viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
              <path d="M12 20h9M16.5 3.5a2.121 2.121 0 0 1 3 3L7 19l-4 1 1-4L16.5 3.5z" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
            </svg>
            <span>新增版本</span>
          </div>
        </div>
        <div class="panel-body">
          <el-form :model="form" label-position="top" class="version-form">
            <el-form-item label="版本号">
              <el-input v-model="form.version" placeholder="例如 4.29" />
            </el-form-item>
            <el-form-item label="更新内容">
              <el-input
                v-model="form.content"
                type="textarea"
                :rows="10"
                placeholder="描述本次更新的内容...&#10;- 新增：xxx功能&#10;- 修复：xxx问题&#10;- 优化：xxx体验"
                resize="none"
              />
            </el-form-item>
            <div class="form-actions">
              <el-button type="primary" :loading="loadingAdd" @click="add" class="submit-btn">
                <svg viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
                  <path d="M19 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h11l5 5v11a2 2 0 0 1-2 2z" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
                  <polyline points="17 21 17 13 7 13 7 21" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
                  <polyline points="7 3 7 8 15 8" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
                </svg>
                提交版本
              </el-button>
              <el-button @click="clearForm" class="clear-btn">清空</el-button>
            </div>
          </el-form>
        </div>
      </div>

      <!-- Versions List Panel -->
      <div class="panel list-panel">
        <div class="panel-header">
          <div class="panel-title">
            <svg viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
              <circle cx="12" cy="12" r="10" stroke="currentColor" stroke-width="2"/>
              <polyline points="12 6 12 12 16 14" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
            </svg>
            <span>版本历史</span>
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
          <div v-loading="loadingList" class="versions-timeline">
            <div v-for="(item, index) in rows" :key="item.version" class="timeline-item">
              <div class="timeline-marker">
                <div :class="['marker-dot', index === 0 ? 'latest' : '']"></div>
                <div v-if="index < rows.length - 1" class="marker-line"></div>
              </div>
              <div class="timeline-content">
                <div class="version-header">
                  <span class="version-badge">v{{ item.version }}</span>
                  <span class="version-date">{{ formatTime(item.created_at) }}</span>
                  <span v-if="index === 0" class="latest-tag">最新</span>
                </div>
                <div class="version-content">{{ item.content }}</div>
              </div>
            </div>
            <div v-if="rows.length === 0 && !loadingList" class="empty-state">
              <svg viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
                <path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
                <polyline points="14 2 14 8 20 8" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
              </svg>
              <span>暂无版本记录</span>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
/**
 * Versions Management View
 * Manage app version history and changelogs
 */
import { onMounted, reactive, ref } from 'vue'
import { ElMessage } from 'element-plus'
import { http } from '../utils/http'

type Row = {
  version: string
  content: string
  created_at: string
}

// Form state
const form = reactive({
  version: '',
  content: ''
})
const loadingAdd = ref(false)

// List state
const rows = ref<Row[]>([])
const loadingList = ref(false)

/**
 * Add new version
 */
async function add() {
  if (!form.version.trim() || !form.content.trim()) {
    return ElMessage.warning('请填写版本号和更新内容')
  }
  
  loadingAdd.value = true
  try {
    await http.post('/admin/versions', {
      version: form.version.trim(),
      content: form.content.trim()
    })
    ElMessage.success('版本发布成功')
    clearForm()
    await load()
  } finally {
    loadingAdd.value = false
  }
}

/**
 * Load versions list
 */
async function load() {
  loadingList.value = true
  try {
    rows.value = await http.get<Row[]>('/client/versions?limit=50')
  } finally {
    loadingList.value = false
  }
}

/**
 * Clear form
 */
function clearForm() {
  form.version = ''
  form.content = ''
}

/**
 * Format datetime
 */
function formatTime(dateStr: string): string {
  const date = new Date(dateStr)
  return date.toLocaleDateString('zh-CN', {
    year: 'numeric',
    month: '2-digit',
    day: '2-digit'
  })
}

onMounted(load)
</script>

<style scoped>
.versions-view {
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

/* Version Form */
.version-form :deep(.el-form-item) {
  margin-bottom: 20px;
}

.version-form :deep(.el-form-item__label) {
  font-weight: 500;
  color: var(--text-secondary);
  padding-bottom: 8px;
}

.version-form :deep(.el-textarea__inner) {
  font-family: inherit;
  line-height: 1.6;
}

.form-actions {
  display: flex;
  gap: 12px;
  margin-top: 8px;
}

.submit-btn {
  display: flex;
  align-items: center;
  gap: 8px;
}

.submit-btn svg {
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

/* Timeline Styles */
.versions-timeline {
  max-height: 520px;
  overflow-y: auto;
}

.timeline-item {
  display: flex;
  gap: 16px;
  position: relative;
}

.timeline-marker {
  display: flex;
  flex-direction: column;
  align-items: center;
  flex-shrink: 0;
}

.marker-dot {
  width: 12px;
  height: 12px;
  background: var(--bg-hover);
  border: 2px solid var(--border-default);
  border-radius: 50%;
  z-index: 1;
}

.marker-dot.latest {
  background: var(--primary);
  border-color: var(--primary);
  box-shadow: 0 0 12px var(--primary-glow);
}

.marker-line {
  width: 2px;
  flex: 1;
  background: var(--border-subtle);
  margin: 4px 0;
}

.timeline-content {
  flex: 1;
  padding-bottom: 24px;
}

.version-header {
  display: flex;
  align-items: center;
  gap: 12px;
  margin-bottom: 12px;
}

.version-badge {
  display: inline-block;
  padding: 4px 12px;
  background: rgba(32, 165, 58, 0.15);
  color: var(--primary);
  border-radius: 6px;
  font-size: 14px;
  font-weight: 700;
  font-family: 'SF Mono', 'Consolas', monospace;
}

.version-date {
  font-size: 13px;
  color: var(--text-muted);
}

.latest-tag {
  display: inline-block;
  padding: 2px 8px;
  background: var(--primary);
  color: white;
  border-radius: 4px;
  font-size: 11px;
  font-weight: 600;
  text-transform: uppercase;
}

.version-content {
  color: var(--text-secondary);
  line-height: 1.7;
  white-space: pre-wrap;
  font-size: 14px;
  background: var(--bg-elevated);
  padding: 16px;
  border-radius: var(--radius-sm);
  border: 1px solid var(--border-subtle);
}

/* Empty State */
.empty-state {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: 60px 20px;
  color: var(--text-muted);
  gap: 12px;
}

.empty-state svg {
  width: 48px;
  height: 48px;
  opacity: 0.5;
}
</style>
