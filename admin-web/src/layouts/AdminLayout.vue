<template>
  <div class="admin-layout" :class="{ 'sidebar-collapsed': isCollapsed }">
    <!-- Sidebar -->
    <aside class="sidebar">
      <div class="sidebar-header">
        <div class="logo">
          <div class="logo-icon">
            <svg viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
              <path d="M12 2L2 7L12 12L22 7L12 2Z" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
              <path d="M2 17L12 22L22 17" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
              <path d="M2 12L12 17L22 12" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
            </svg>
          </div>
          <div class="logo-text">
            <span class="logo-title">控制台</span>
            <span class="logo-domain">bocail.com</span>
          </div>
        </div>
        <!-- Collapse Button in Header -->
        <button class="collapse-btn" @click="toggleCollapse" :title="isCollapsed ? '展开' : '收起'">
          <svg viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg" :class="{ rotated: isCollapsed }">
            <polyline points="15 18 9 12 15 6" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
          </svg>
        </button>
      </div>

      <nav class="sidebar-nav">
        <div class="nav-section">
          <span class="nav-section-title">内容管理</span>
          <router-link to="/cards" class="nav-item" :class="{ active: isActive('/cards') }" :title="isCollapsed ? '充值卡' : ''">
            <svg class="nav-icon" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
              <rect x="3" y="5" width="18" height="14" rx="2" stroke="currentColor" stroke-width="2"/>
              <path d="M3 10H21" stroke="currentColor" stroke-width="2"/>
            </svg>
            <span class="nav-text">充值卡</span>
          </router-link>
          <router-link to="/announcements" class="nav-item" :class="{ active: isActive('/announcements') }" :title="isCollapsed ? '公告' : ''">
            <svg class="nav-icon" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
              <path d="M18 8A6 6 0 1 0 6 8c0 7-3 9-3 9h18s-3-2-3-9" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
              <path d="M13.73 21a2 2 0 0 1-3.46 0" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
            </svg>
            <span class="nav-text">公告</span>
          </router-link>
          <router-link to="/versions" class="nav-item" :class="{ active: isActive('/versions') }" :title="isCollapsed ? '更新日志' : ''">
            <svg class="nav-icon" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
              <path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
              <polyline points="14 2 14 8 20 8" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
              <line x1="16" y1="13" x2="8" y2="13" stroke="currentColor" stroke-width="2" stroke-linecap="round"/>
              <line x1="16" y1="17" x2="8" y2="17" stroke="currentColor" stroke-width="2" stroke-linecap="round"/>
            </svg>
            <span class="nav-text">更新日志</span>
          </router-link>
        </div>

        <div class="nav-section">
          <span class="nav-section-title">系统配置</span>
          <router-link to="/settings" class="nav-item" :class="{ active: isActive('/settings') }" :title="isCollapsed ? '桌面端配置' : ''">
            <svg class="nav-icon" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
              <circle cx="12" cy="12" r="3" stroke="currentColor" stroke-width="2"/>
              <path d="M19.4 15a1.65 1.65 0 0 0 .33 1.82l.06.06a2 2 0 0 1 0 2.83 2 2 0 0 1-2.83 0l-.06-.06a1.65 1.65 0 0 0-1.82-.33 1.65 1.65 0 0 0-1 1.51V21a2 2 0 0 1-2 2 2 2 0 0 1-2-2v-.09A1.65 1.65 0 0 0 9 19.4a1.65 1.65 0 0 0-1.82.33l-.06.06a2 2 0 0 1-2.83 0 2 2 0 0 1 0-2.83l.06-.06a1.65 1.65 0 0 0 .33-1.82 1.65 1.65 0 0 0-1.51-1H3a2 2 0 0 1-2-2 2 2 0 0 1 2-2h.09A1.65 1.65 0 0 0 4.6 9a1.65 1.65 0 0 0-.33-1.82l-.06-.06a2 2 0 0 1 0-2.83 2 2 0 0 1 2.83 0l.06.06a1.65 1.65 0 0 0 1.82.33H9a1.65 1.65 0 0 0 1-1.51V3a2 2 0 0 1 2-2 2 2 0 0 1 2 2v.09a1.65 1.65 0 0 0 1 1.51 1.65 1.65 0 0 0 1.82-.33l.06-.06a2 2 0 0 1 2.83 0 2 2 0 0 1 0 2.83l-.06.06a1.65 1.65 0 0 0-.33 1.82V9a1.65 1.65 0 0 0 1.51 1H21a2 2 0 0 1 2 2 2 2 0 0 1-2 2h-.09a1.65 1.65 0 0 0-1.51 1z" stroke="currentColor" stroke-width="2"/>
            </svg>
            <span class="nav-text">桌面端配置</span>
          </router-link>
          <router-link to="/agents" class="nav-item" :class="{ active: isActive('/agents') }" :title="isCollapsed ? 'Agent' : ''">
            <svg class="nav-icon" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
              <rect x="4" y="4" width="16" height="16" rx="2" stroke="currentColor" stroke-width="2"/>
              <rect x="9" y="9" width="6" height="6" stroke="currentColor" stroke-width="2"/>
              <line x1="9" y1="1" x2="9" y2="4" stroke="currentColor" stroke-width="2"/>
              <line x1="15" y1="1" x2="15" y2="4" stroke="currentColor" stroke-width="2"/>
              <line x1="9" y1="20" x2="9" y2="23" stroke="currentColor" stroke-width="2"/>
              <line x1="15" y1="20" x2="15" y2="23" stroke="currentColor" stroke-width="2"/>
              <line x1="20" y1="9" x2="23" y2="9" stroke="currentColor" stroke-width="2"/>
              <line x1="20" y1="14" x2="23" y2="14" stroke="currentColor" stroke-width="2"/>
              <line x1="1" y1="9" x2="4" y2="9" stroke="currentColor" stroke-width="2"/>
              <line x1="1" y1="14" x2="4" y2="14" stroke="currentColor" stroke-width="2"/>
            </svg>
            <span class="nav-text">Agent</span>
          </router-link>
        </div>

        <div class="nav-section">
          <span class="nav-section-title">用户管理</span>
          <router-link to="/users" class="nav-item" :class="{ active: isActive('/users') }" :title="isCollapsed ? '用户' : ''">
            <svg class="nav-icon" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
              <path d="M17 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
              <circle cx="9" cy="7" r="4" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
              <path d="M23 21v-2a4 4 0 0 0-3-3.87" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
              <path d="M16 3.13a4 4 0 0 1 0 7.75" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
            </svg>
            <span class="nav-text">用户</span>
          </router-link>
        </div>
      </nav>

      <div class="sidebar-footer">
        <div class="user-info">
          <div class="user-avatar">
            {{ auth.me?.username?.charAt(0)?.toUpperCase() || 'A' }}
          </div>
          <div class="user-details">
            <span class="user-name">{{ auth.me?.username || 'Admin' }}</span>
            <span class="user-role">管理员</span>
          </div>
        </div>
        <button class="logout-btn" @click="logout" title="退出登录">
          <svg viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
            <path d="M9 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h4" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
            <polyline points="16 17 21 12 16 7" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
            <line x1="21" y1="12" x2="9" y2="12" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
          </svg>
        </button>
      </div>
    </aside>

    <!-- Main Content -->
    <main class="main-content">
      <header class="content-header">
        <div class="header-left">
          <h1 class="page-title">{{ pageTitle }}</h1>
        </div>
        <div class="header-right">
          <div class="status-indicator">
            <span class="status-dot"></span>
            <span class="status-text">在线</span>
          </div>
        </div>
      </header>

      <div class="content-body">
        <router-view />
      </div>
    </main>
  </div>
</template>

<script setup lang="ts">
/**
 * Admin Layout Component
 * Modern dark theme with collapsible sidebar navigation
 */
import { computed, ref, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useAuthStore } from '../stores/auth'

const route = useRoute()
const router = useRouter()
const auth = useAuthStore()

// Sidebar collapsed state (persisted in localStorage)
const isCollapsed = ref(false)

// Load collapsed state from localStorage on mount
onMounted(() => {
  const saved = localStorage.getItem('sidebar-collapsed')
  if (saved !== null) {
    isCollapsed.value = saved === 'true'
  }
})

// Toggle sidebar collapsed state
function toggleCollapse() {
  isCollapsed.value = !isCollapsed.value
  localStorage.setItem('sidebar-collapsed', String(isCollapsed.value))
}

// Check if current route matches
function isActive(path: string): boolean {
  return route.path === path
}

// Dynamic page title based on route
const pageTitle = computed(() => {
  const titles: Record<string, string> = {
    '/cards': '充值卡管理',
    '/announcements': '公告管理',
    '/versions': '版本更新日志',
    '/settings': '桌面端配置',
    '/agents': 'Agent 管理',
    '/users': '用户管理'
  }
  return titles[route.path] || '控制台'
})

// Handle logout
function logout() {
  auth.logout()
  router.push('/login')
}
</script>

<style scoped>
.admin-layout {
  display: flex;
  min-height: 100vh;
  background: var(--bg-darkest);
}

/* Sidebar Styles */
.sidebar {
  width: 260px;
  background: var(--bg-card);
  border-right: 1px solid var(--border-subtle);
  display: flex;
  flex-direction: column;
  position: fixed;
  height: 100vh;
  z-index: 100;
  transition: width 0.3s cubic-bezier(0.4, 0, 0.2, 1);
}

/* Collapsed Sidebar */
.sidebar-collapsed .sidebar {
  width: 72px;
}

.sidebar-collapsed .logo-text,
.sidebar-collapsed .nav-section-title,
.sidebar-collapsed .nav-text,
.sidebar-collapsed .user-details {
  opacity: 0;
  width: 0;
  overflow: hidden;
  white-space: nowrap;
}

.sidebar-collapsed .sidebar-header {
  padding: 16px;
  justify-content: center;
}

.sidebar-collapsed .logo {
  justify-content: center;
}

.sidebar-collapsed .nav-item {
  justify-content: center;
  padding: 14px;
}

.sidebar-collapsed .user-info {
  justify-content: center;
}

.sidebar-collapsed .main-content {
  margin-left: 72px;
}

.sidebar-collapsed .collapse-btn {
  position: absolute;
  right: 50%;
  transform: translateX(50%);
  margin-left: 0;
}

.sidebar-collapsed .sidebar-footer {
  flex-direction: column;
  gap: 12px;
  padding: 16px 12px;
}

.sidebar-header {
  padding: 20px;
  border-bottom: 1px solid var(--border-subtle);
  display: flex;
  align-items: center;
  justify-content: space-between;
  transition: padding 0.3s ease;
}

.logo {
  display: flex;
  align-items: center;
  gap: 12px;
  transition: all 0.3s ease;
}

.logo-icon {
  width: 42px;
  height: 42px;
  min-width: 42px;
  background: linear-gradient(135deg, var(--primary) 0%, var(--primary-dark) 100%);
  border-radius: var(--radius-md);
  display: flex;
  align-items: center;
  justify-content: center;
  color: white;
  box-shadow: 0 4px 12px var(--primary-glow);
}

.logo-icon svg {
  width: 24px;
  height: 24px;
}

.logo-text {
  display: flex;
  flex-direction: column;
  transition: opacity 0.2s ease, width 0.3s ease;
}

.logo-title {
  font-size: 18px;
  font-weight: 700;
  color: var(--text-primary);
  letter-spacing: -0.5px;
}

.logo-domain {
  font-size: 12px;
  color: var(--text-muted);
  font-weight: 500;
}

/* Collapse Toggle Button */
.collapse-btn {
  width: 32px;
  height: 32px;
  min-width: 32px;
  border: 1px solid var(--border-default);
  background: var(--bg-elevated);
  border-radius: var(--radius-sm);
  cursor: pointer;
  display: flex;
  align-items: center;
  justify-content: center;
  color: var(--text-muted);
  transition: all var(--transition-fast);
  margin-left: auto;
}

.collapse-btn:hover {
  background: var(--bg-hover);
  color: var(--primary);
  border-color: var(--primary);
}

.collapse-btn svg {
  width: 18px;
  height: 18px;
  transition: transform 0.3s ease;
}

.collapse-btn svg.rotated {
  transform: rotate(180deg);
}

/* Navigation */
.sidebar-nav {
  flex: 1;
  padding: 16px 12px;
  overflow-y: auto;
}

.nav-section {
  margin-bottom: 24px;
}

.nav-section-title {
  display: block;
  font-size: 11px;
  font-weight: 600;
  color: var(--text-muted);
  text-transform: uppercase;
  letter-spacing: 1px;
  padding: 0 12px;
  margin-bottom: 8px;
  transition: opacity 0.2s ease, width 0.3s ease;
}

.nav-item {
  display: flex;
  align-items: center;
  gap: 12px;
  padding: 12px 14px;
  border-radius: var(--radius-sm);
  color: var(--text-secondary);
  text-decoration: none;
  font-size: 14px;
  font-weight: 500;
  transition: all var(--transition-fast);
  margin-bottom: 4px;
}

.nav-item:hover {
  background: var(--bg-hover);
  color: var(--text-primary);
}

.nav-item.active {
  background: var(--primary);
  color: white;
  box-shadow: 0 4px 12px var(--primary-glow);
}

.nav-icon {
  width: 20px;
  height: 20px;
  min-width: 20px;
  flex-shrink: 0;
}

.nav-text {
  transition: opacity 0.2s ease, width 0.3s ease;
  white-space: nowrap;
}

/* Sidebar Footer */
.sidebar-footer {
  padding: 16px 20px;
  border-top: 1px solid var(--border-subtle);
  display: flex;
  align-items: center;
  justify-content: space-between;
  transition: all 0.3s ease;
}

.user-info {
  display: flex;
  align-items: center;
  gap: 12px;
  transition: all 0.3s ease;
}

.user-avatar {
  width: 36px;
  height: 36px;
  min-width: 36px;
  background: linear-gradient(135deg, var(--primary) 0%, var(--primary-dark) 100%);
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
  font-weight: 700;
  font-size: 14px;
  color: white;
}

.user-details {
  display: flex;
  flex-direction: column;
  transition: opacity 0.2s ease, width 0.3s ease;
}

.user-name {
  font-size: 14px;
  font-weight: 600;
  color: var(--text-primary);
}

.user-role {
  font-size: 11px;
  color: var(--text-muted);
}

.logout-btn {
  width: 36px;
  height: 36px;
  min-width: 36px;
  border: none;
  background: var(--bg-hover);
  border-radius: var(--radius-sm);
  cursor: pointer;
  display: flex;
  align-items: center;
  justify-content: center;
  color: var(--text-muted);
  transition: all var(--transition-fast);
}

.logout-btn:hover {
  background: var(--danger);
  color: white;
}

.logout-btn svg {
  width: 18px;
  height: 18px;
}

/* Main Content Area */
.main-content {
  flex: 1;
  margin-left: 260px;
  display: flex;
  flex-direction: column;
  min-height: 100vh;
  transition: margin-left 0.3s cubic-bezier(0.4, 0, 0.2, 1);
}

.content-header {
  height: 72px;
  padding: 0 32px;
  display: flex;
  align-items: center;
  justify-content: space-between;
  background: var(--bg-card);
  border-bottom: 1px solid var(--border-subtle);
  position: sticky;
  top: 0;
  z-index: 50;
}

.header-left {
  display: flex;
  align-items: center;
  gap: 16px;
}

.page-title {
  font-size: 20px;
  font-weight: 700;
  color: var(--text-primary);
  margin: 0;
  letter-spacing: -0.5px;
}

.header-right {
  display: flex;
  align-items: center;
  gap: 16px;
}

.status-indicator {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 8px 14px;
  background: rgba(32, 165, 58, 0.1);
  border-radius: 20px;
  border: 1px solid rgba(32, 165, 58, 0.3);
}

.status-dot {
  width: 8px;
  height: 8px;
  background: var(--primary);
  border-radius: 50%;
  animation: pulse 2s infinite;
}

@keyframes pulse {
  0% {
    box-shadow: 0 0 0 0 rgba(32, 165, 58, 0.6);
  }
  70% {
    box-shadow: 0 0 0 8px rgba(32, 165, 58, 0);
  }
  100% {
    box-shadow: 0 0 0 0 rgba(32, 165, 58, 0);
  }
}

.status-text {
  font-size: 13px;
  font-weight: 500;
  color: var(--primary);
}

.content-body {
  flex: 1;
  padding: 32px;
  background: var(--bg-dark);
}

/* Responsive Design - Mobile */
@media (max-width: 768px) {
  .sidebar {
    width: 72px;
  }
  
  .sidebar-header {
    padding: 16px;
    justify-content: center;
  }
  
  .logo {
    justify-content: center;
  }
  
  .logo-text,
  .nav-section-title,
  .nav-text,
  .user-details {
    opacity: 0;
    width: 0;
    overflow: hidden;
  }
  
  .nav-item {
    justify-content: center;
    padding: 14px;
  }
  
  .main-content {
    margin-left: 72px;
  }
  
  .user-info {
    justify-content: center;
  }
  
  .sidebar-footer {
    flex-direction: column;
    gap: 12px;
    padding: 16px 12px;
  }
  
  .collapse-btn {
    display: none;
  }
}
</style>
