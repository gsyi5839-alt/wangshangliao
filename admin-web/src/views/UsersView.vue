<template>
  <div class="users-view">
    <div class="panel users-panel">
      <div class="panel-header">
        <div class="panel-title">
          <svg viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
            <path d="M17 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
            <circle cx="9" cy="7" r="4" stroke="currentColor" stroke-width="2"/>
            <path d="M23 21v-2a4 4 0 0 0-3-3.87" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
            <path d="M16 3.13a4 4 0 0 1 0 7.75" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
          </svg>
          <span>用户管理</span>
          <span class="user-count">{{ rows.length }} 位用户</span>
        </div>
        <el-button @click="load" :loading="loading" class="refresh-btn">
          <svg viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
            <polyline points="23 4 23 10 17 10" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
            <path d="M20.49 15a9 9 0 1 1-2.12-9.36L23 10" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
          </svg>
          刷新
        </el-button>
      </div>
      <div class="panel-body">
        <el-table :data="rows" max-height="600" v-loading="loading" class="users-table">
          <el-table-column prop="id" label="ID" width="70" align="center" />
          <el-table-column prop="username" label="用户名" width="160">
            <template #default="{ row }">
              <div class="user-cell">
                <div class="user-avatar">{{ row.username.charAt(0).toUpperCase() }}</div>
                <span class="username">{{ row.username }}</span>
              </div>
            </template>
          </el-table-column>
          <el-table-column prop="expire_at" label="到期时间" width="180">
            <template #default="{ row }">
              <div v-if="row.expire_at" :class="['expire-badge', isExpired(row.expire_at) ? 'expired' : 'active']">
                <svg viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
                  <circle cx="12" cy="12" r="10" stroke="currentColor" stroke-width="2"/>
                  <polyline points="12 6 12 12 16 14" stroke="currentColor" stroke-width="2" stroke-linecap="round"/>
                </svg>
                <span>{{ formatExpire(row.expire_at) }}</span>
              </div>
              <span v-else class="no-expire">未设置</span>
            </template>
          </el-table-column>
          <el-table-column prop="bound_info" label="绑定信息" min-width="180">
            <template #default="{ row }">
              <span v-if="row.bound_info" class="bound-info">{{ row.bound_info }}</span>
              <span v-else class="no-bound">-</span>
            </template>
          </el-table-column>
          <el-table-column prop="promoter_username" label="推广员" width="120">
            <template #default="{ row }">
              <span v-if="row.promoter_username" class="promoter-tag">{{ row.promoter_username }}</span>
              <span v-else class="no-promoter">-</span>
            </template>
          </el-table-column>
          <el-table-column label="续费操作" width="220" align="center">
            <template #default="{ row }">
              <div class="extend-actions">
                <el-input-number v-model="extendDays[row.id]" :min="1" :max="3650" size="small" class="days-input" />
                <el-button type="primary" size="small" @click="extend(row.id)" class="extend-btn">
                  <svg viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
                    <line x1="12" y1="5" x2="12" y2="19" stroke="currentColor" stroke-width="2" stroke-linecap="round"/>
                    <line x1="5" y1="12" x2="19" y2="12" stroke="currentColor" stroke-width="2" stroke-linecap="round"/>
                  </svg>
                  续费
                </el-button>
              </div>
            </template>
          </el-table-column>
        </el-table>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
/**
 * Users Management View
 * Manage client users and their subscriptions
 */
import { onMounted, reactive, ref } from 'vue'
import { ElMessage } from 'element-plus'
import { http } from '../utils/http'

type UserRow = {
  id: number
  username: string
  bound_info: string | null
  promoter_username: string | null
  expire_at: string | null
}

// State
const rows = ref<UserRow[]>([])
const loading = ref(false)
const extendDays = reactive<Record<number, number>>({})

/**
 * Load users list
 */
async function load() {
  loading.value = true
  try {
    rows.value = await http.get<UserRow[]>('/admin/users?limit=500')
    for (const r of rows.value) {
      if (!extendDays[r.id]) extendDays[r.id] = 30
    }
  } finally {
    loading.value = false
  }
}

/**
 * Extend user subscription
 */
async function extend(id: number) {
  const days = extendDays[id] || 30
  await http.post(`/admin/users/${id}/extend`, { days })
  ElMessage.success(`已成功续费 ${days} 天`)
  await load()
}

/**
 * Check if date is expired
 */
function isExpired(dateStr: string): boolean {
  return new Date(dateStr) < new Date()
}

/**
 * Format expire date
 */
function formatExpire(dateStr: string): string {
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
.users-view {
  display: flex;
  flex-direction: column;
  gap: 24px;
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

.user-count {
  font-size: 13px;
  font-weight: 500;
  color: var(--text-muted);
  background: var(--bg-input);
  padding: 4px 10px;
  border-radius: 10px;
  margin-left: 8px;
}

.panel-body {
  padding: 20px;
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

/* User Cell */
.user-cell {
  display: flex;
  align-items: center;
  gap: 10px;
}

.user-avatar {
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

.username {
  font-weight: 500;
  color: var(--text-primary);
}

/* Expire Badge */
.expire-badge {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  padding: 4px 10px;
  border-radius: 8px;
  font-size: 13px;
  font-weight: 500;
}

.expire-badge svg {
  width: 14px;
  height: 14px;
}

.expire-badge.active {
  background: rgba(32, 165, 58, 0.15);
  color: var(--primary);
}

.expire-badge.expired {
  background: rgba(229, 57, 53, 0.15);
  color: var(--danger);
}

.no-expire {
  color: var(--text-disabled);
}

/* Other Fields */
.bound-info {
  color: var(--text-secondary);
  font-size: 13px;
}

.no-bound {
  color: var(--text-disabled);
}

.promoter-tag {
  display: inline-block;
  padding: 2px 8px;
  background: rgba(38, 166, 154, 0.15);
  color: var(--info);
  border-radius: 6px;
  font-size: 12px;
  font-weight: 500;
}

.no-promoter {
  color: var(--text-disabled);
}

/* Extend Actions */
.extend-actions {
  display: flex;
  align-items: center;
  gap: 8px;
}

.days-input {
  width: 100px;
}

.extend-btn {
  display: flex;
  align-items: center;
  gap: 4px;
}

.extend-btn svg {
  width: 14px;
  height: 14px;
}
</style>
