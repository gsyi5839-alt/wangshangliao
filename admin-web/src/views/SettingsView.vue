<template>
  <div class="settings-view">
    <div class="panel settings-panel">
      <div class="panel-header">
        <div class="panel-title">
          <svg viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
            <circle cx="12" cy="12" r="3" stroke="currentColor" stroke-width="2"/>
            <path d="M19.4 15a1.65 1.65 0 0 0 .33 1.82l.06.06a2 2 0 0 1 0 2.83 2 2 0 0 1-2.83 0l-.06-.06a1.65 1.65 0 0 0-1.82-.33 1.65 1.65 0 0 0-1 1.51V21a2 2 0 0 1-2 2 2 2 0 0 1-2-2v-.09A1.65 1.65 0 0 0 9 19.4a1.65 1.65 0 0 0-1.82.33l-.06.06a2 2 0 0 1-2.83 0 2 2 0 0 1 0-2.83l.06-.06a1.65 1.65 0 0 0 .33-1.82 1.65 1.65 0 0 0-1.51-1H3a2 2 0 0 1-2-2 2 2 0 0 1 2-2h.09A1.65 1.65 0 0 0 4.6 9a1.65 1.65 0 0 0-.33-1.82l-.06-.06a2 2 0 0 1 0-2.83 2 2 0 0 1 2.83 0l.06.06a1.65 1.65 0 0 0 1.82.33H9a1.65 1.65 0 0 0 1-1.51V3a2 2 0 0 1 2-2 2 2 0 0 1 2 2v.09a1.65 1.65 0 0 0 1 1.51 1.65 1.65 0 0 0 1.82-.33l.06-.06a2 2 0 0 1 2.83 0 2 2 0 0 1 0 2.83l-.06.06a1.65 1.65 0 0 0-.33 1.82V9a1.65 1.65 0 0 0 1.51 1H21a2 2 0 0 1 2 2 2 2 0 0 1-2 2h-.09a1.65 1.65 0 0 0-1.51 1z" stroke="currentColor" stroke-width="2"/>
          </svg>
          <span>桌面端配置</span>
        </div>
        <div class="header-actions">
          <el-button @click="load" :loading="loadingLoad" class="refresh-btn">
            <svg viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
              <polyline points="23 4 23 10 17 10" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
              <path d="M20.49 15a9 9 0 1 1-2.12-9.36L23 10" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
            </svg>
            刷新
          </el-button>
        </div>
      </div>
      <div class="panel-body">
        <el-form :model="form" label-position="top" class="settings-form">
          <!-- Download Section -->
          <div class="form-section">
            <div class="section-header">
              <svg viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
                <path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
                <polyline points="7 10 12 15 17 10" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
                <line x1="12" y1="15" x2="12" y2="3" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
              </svg>
              <span>下载配置</span>
            </div>
            <div class="section-content">
              <el-form-item label="框架下载地址">
                <el-input v-model="form.framework_download_url" placeholder="https://example.com/framework.zip">
                  <template #prefix>
                    <svg viewBox="0 0 24 24" fill="none" class="input-icon" xmlns="http://www.w3.org/2000/svg">
                      <path d="M10 13a5 5 0 0 0 7.54.54l3-3a5 5 0 0 0-7.07-7.07l-1.72 1.71" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
                      <path d="M14 11a5 5 0 0 0-7.54-.54l-3 3a5 5 0 0 0 7.07 7.07l1.71-1.71" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
                    </svg>
                  </template>
                </el-input>
                <span class="form-hint">桌面端框架文件下载链接</span>
              </el-form-item>
              <el-form-item label="官方机器包下载">
                <el-input v-model="form.machine_pack_download_url" placeholder="https://example.com/machine.zip">
                  <template #prefix>
                    <svg viewBox="0 0 24 24" fill="none" class="input-icon" xmlns="http://www.w3.org/2000/svg">
                      <path d="M10 13a5 5 0 0 0 7.54.54l3-3a5 5 0 0 0-7.07-7.07l-1.72 1.71" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
                      <path d="M14 11a5 5 0 0 0-7.54-.54l-3 3a5 5 0 0 0 7.07 7.07l1.71-1.71" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
                    </svg>
                  </template>
                </el-input>
                <span class="form-hint">机器人包文件下载链接</span>
              </el-form-item>
            </div>
          </div>

          <!-- API Section -->
          <div class="form-section">
            <div class="section-header">
              <svg viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
                <path d="M22 12h-4l-3 9L9 3l-3 9H2" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
              </svg>
              <span>开奖 API 配置</span>
            </div>
            <div class="section-content">
              <el-form-item label="开奖 API 地址">
                <el-input v-model="form.lottery_api_url" placeholder="https://bcapi.cn/token/{token}/code/jnd28/rows/1.json">
                  <template #prefix>
                    <svg viewBox="0 0 24 24" fill="none" class="input-icon" xmlns="http://www.w3.org/2000/svg">
                      <circle cx="12" cy="12" r="10" stroke="currentColor" stroke-width="2"/>
                      <line x1="2" y1="12" x2="22" y2="12" stroke="currentColor" stroke-width="2"/>
                      <path d="M12 2a15.3 15.3 0 0 1 4 10 15.3 15.3 0 0 1-4 10 15.3 15.3 0 0 1-4-10 15.3 15.3 0 0 1 4-10z" stroke="currentColor" stroke-width="2"/>
                    </svg>
                  </template>
                </el-input>
                <span class="form-hint">使用 {token} 作为令牌占位符</span>
              </el-form-item>
              <el-form-item label="开奖 API Token">
                <el-input v-model="form.lottery_api_token" type="password" show-password placeholder="输入 API Token">
                  <template #prefix>
                    <svg viewBox="0 0 24 24" fill="none" class="input-icon" xmlns="http://www.w3.org/2000/svg">
                      <rect x="3" y="11" width="18" height="11" rx="2" ry="2" stroke="currentColor" stroke-width="2"/>
                      <path d="M7 11V7a5 5 0 0 1 10 0v4" stroke="currentColor" stroke-width="2"/>
                    </svg>
                  </template>
                </el-input>
                <span class="form-hint">bcapi.cn 的访问令牌</span>
              </el-form-item>
            </div>
          </div>

          <!-- Actions -->
          <div class="form-actions">
            <el-button type="primary" @click="save" :loading="loadingSave" class="save-btn">
              <svg viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
                <path d="M19 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h11l5 5v11a2 2 0 0 1-2 2z" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
                <polyline points="17 21 17 13 7 13 7 21" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
                <polyline points="7 3 7 8 15 8" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
              </svg>
              保存配置
            </el-button>
          </div>
        </el-form>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
/**
 * Settings Management View
 * Configure desktop client settings
 */
import { onMounted, reactive, ref } from 'vue'
import { ElMessage } from 'element-plus'
import { http } from '../utils/http'

type Settings = {
  framework_download_url: string | null
  machine_pack_download_url: string | null
  lottery_api_url: string | null
  lottery_api_token: string | null
}

// Form state
const form = reactive<Settings>({
  framework_download_url: '',
  machine_pack_download_url: '',
  lottery_api_url: '',
  lottery_api_token: ''
})

const loadingLoad = ref(false)
const loadingSave = ref(false)

/**
 * Load settings from server
 */
async function load() {
  loadingLoad.value = true
  try {
    const data = await http.get<Settings>('/admin/settings')
    form.framework_download_url = data.framework_download_url || ''
    form.machine_pack_download_url = data.machine_pack_download_url || ''
    form.lottery_api_url = data.lottery_api_url || ''
    form.lottery_api_token = data.lottery_api_token || ''
  } finally {
    loadingLoad.value = false
  }
}

/**
 * Save settings to server
 */
async function save() {
  loadingSave.value = true
  try {
    const pairs: Array<[keyof Settings, string]> = [
      ['framework_download_url', form.framework_download_url || ''],
      ['machine_pack_download_url', form.machine_pack_download_url || ''],
      ['lottery_api_url', form.lottery_api_url || ''],
      ['lottery_api_token', form.lottery_api_token || '']
    ]
    for (const [key, value] of pairs) {
      await http.post('/admin/settings', { key, value })
    }
    ElMessage.success('配置保存成功')
  } finally {
    loadingSave.value = false
  }
}

onMounted(load)
</script>

<style scoped>
.settings-view {
  max-width: 800px;
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
  padding: 24px;
}

/* Form Sections */
.form-section {
  margin-bottom: 32px;
}

.section-header {
  display: flex;
  align-items: center;
  gap: 10px;
  font-size: 15px;
  font-weight: 600;
  color: var(--text-primary);
  margin-bottom: 20px;
  padding-bottom: 12px;
  border-bottom: 1px solid var(--border-subtle);
}

.section-header svg {
  width: 18px;
  height: 18px;
  color: var(--primary);
}

.section-content {
  display: flex;
  flex-direction: column;
  gap: 20px;
}

/* Form Styles */
.settings-form :deep(.el-form-item) {
  margin-bottom: 0;
}

.settings-form :deep(.el-form-item__label) {
  font-weight: 500;
  color: var(--text-secondary);
  padding-bottom: 8px;
}

.input-icon {
  width: 16px;
  height: 16px;
  color: var(--text-muted);
}

.form-hint {
  display: block;
  font-size: 12px;
  color: var(--text-muted);
  margin-top: 6px;
}

/* Actions */
.form-actions {
  margin-top: 32px;
  padding-top: 24px;
  border-top: 1px solid var(--border-subtle);
}

.save-btn,
.refresh-btn {
  display: flex;
  align-items: center;
  gap: 8px;
}

.save-btn svg,
.refresh-btn svg {
  width: 16px;
  height: 16px;
}
</style>
