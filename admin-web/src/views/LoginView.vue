<template>
  <div class="login-page">
    <!-- Animated Background -->
    <div class="background">
      <div class="grid-overlay"></div>
      <div class="glow glow-1"></div>
      <div class="glow glow-2"></div>
      <div class="floating-shapes">
        <div class="shape shape-1"></div>
        <div class="shape shape-2"></div>
        <div class="shape shape-3"></div>
      </div>
    </div>

    <!-- Login Card -->
    <div class="login-container">
      <div class="login-card">
        <!-- Logo Section -->
        <div class="login-header">
          <div class="logo-wrapper">
            <div class="logo-icon">
              <svg viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
                <path d="M12 2L2 7L12 12L22 7L12 2Z" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
                <path d="M2 17L12 22L22 17" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
                <path d="M2 12L12 17L22 12" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
              </svg>
            </div>
          </div>
          <h1 class="login-title">管理控制台</h1>
          <p class="login-subtitle">bocail.com 后台管理系统</p>
        </div>

        <!-- Login Form -->
        <el-form ref="formRef" :model="form" :rules="rules" class="login-form" @submit.prevent="submit">
          <el-form-item prop="username">
            <div class="input-wrapper">
              <svg class="input-icon" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
                <path d="M20 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
                <circle cx="12" cy="7" r="4" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
              </svg>
              <el-input
                v-model="form.username"
                placeholder="账号"
                size="large"
                autocomplete="username"
              />
            </div>
          </el-form-item>

          <el-form-item prop="password">
            <div class="input-wrapper">
              <svg class="input-icon" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
                <rect x="3" y="11" width="18" height="11" rx="2" ry="2" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
                <path d="M7 11V7a5 5 0 0 1 10 0v4" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
              </svg>
              <el-input
                v-model="form.password"
                type="password"
                placeholder="密码"
                size="large"
                autocomplete="current-password"
                show-password
                @keyup.enter="submit"
              />
            </div>
          </el-form-item>

          <el-button
            type="primary"
            size="large"
            class="submit-btn"
            :loading="loading"
            @click="submit"
          >
            <span v-if="!loading">登录系统</span>
            <span v-else>验证中...</span>
          </el-button>
        </el-form>

        <!-- Footer -->
        <div class="login-footer">
          <span class="security-badge">
            <svg viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
              <path d="M12 22s8-4 8-10V5l-8-3-8 3v7c0 6 8 10 8 10z" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
            </svg>
            安全加密连接
          </span>
        </div>
      </div>

      <!-- Version Tag -->
      <div class="version-tag">v1.0.0</div>
    </div>
  </div>
</template>

<script setup lang="ts">
/**
 * Login View Component
 * Modern login page with animated background
 */
import { reactive, ref } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '../stores/auth'
import { ElMessage } from 'element-plus'
import type { FormInstance, FormRules } from 'element-plus'

const router = useRouter()
const auth = useAuthStore()

const formRef = ref<FormInstance>()
const loading = ref(false)

const form = reactive({
  username: '',
  password: ''
})

const rules: FormRules = {
  username: [
    { required: true, message: '请输入账号', trigger: 'blur' }
  ],
  password: [
    { required: true, message: '请输入密码', trigger: 'blur' }
  ]
}

async function submit() {
  if (!formRef.value) return
  
  try {
    await formRef.value.validate()
  } catch {
    return
  }

  loading.value = true
  try {
    await auth.login(form.username.trim(), form.password)
    ElMessage.success('登录成功')
    await router.push('/cards')
  } catch (err: unknown) {
    const message = err instanceof Error ? err.message : '登录失败'
    ElMessage.error(message)
  } finally {
    loading.value = false
  }
}
</script>

<style scoped>
.login-page {
  min-height: 100vh;
  display: flex;
  align-items: center;
  justify-content: center;
  position: relative;
  overflow: hidden;
  background: var(--bg-darkest);
}

/* Animated Background */
.background {
  position: fixed;
  inset: 0;
  z-index: 0;
  overflow: hidden;
}

.grid-overlay {
  position: absolute;
  inset: 0;
  background-image: 
    linear-gradient(rgba(32, 165, 58, 0.03) 1px, transparent 1px),
    linear-gradient(90deg, rgba(32, 165, 58, 0.03) 1px, transparent 1px);
  background-size: 60px 60px;
  animation: gridMove 20s linear infinite;
}

@keyframes gridMove {
  0% { transform: translate(0, 0); }
  100% { transform: translate(60px, 60px); }
}

.glow {
  position: absolute;
  border-radius: 50%;
  filter: blur(100px);
  opacity: 0.4;
  animation: glowPulse 8s ease-in-out infinite;
}

.glow-1 {
  width: 600px;
  height: 600px;
  background: radial-gradient(circle, var(--primary) 0%, transparent 70%);
  top: -200px;
  right: -100px;
  animation-delay: 0s;
}

.glow-2 {
  width: 500px;
  height: 500px;
  background: radial-gradient(circle, rgba(32, 165, 58, 0.5) 0%, transparent 70%);
  bottom: -150px;
  left: -100px;
  animation-delay: 4s;
}

@keyframes glowPulse {
  0%, 100% { opacity: 0.3; transform: scale(1); }
  50% { opacity: 0.5; transform: scale(1.1); }
}

.floating-shapes {
  position: absolute;
  inset: 0;
  overflow: hidden;
}

.shape {
  position: absolute;
  border: 1px solid rgba(32, 165, 58, 0.2);
  border-radius: var(--radius-md);
  animation: float 20s ease-in-out infinite;
}

.shape-1 {
  width: 80px;
  height: 80px;
  top: 15%;
  left: 10%;
  animation-delay: 0s;
}

.shape-2 {
  width: 120px;
  height: 120px;
  top: 60%;
  right: 15%;
  animation-delay: 5s;
}

.shape-3 {
  width: 60px;
  height: 60px;
  bottom: 20%;
  left: 25%;
  animation-delay: 10s;
}

@keyframes float {
  0%, 100% { transform: translateY(0) rotate(0deg); }
  25% { transform: translateY(-20px) rotate(5deg); }
  50% { transform: translateY(0) rotate(0deg); }
  75% { transform: translateY(20px) rotate(-5deg); }
}

/* Login Container */
.login-container {
  position: relative;
  z-index: 10;
  display: flex;
  flex-direction: column;
  align-items: center;
}

.login-card {
  width: 420px;
  max-width: 92vw;
  background: rgba(22, 22, 22, 0.8);
  backdrop-filter: blur(20px);
  border: 1px solid var(--border-subtle);
  border-radius: var(--radius-xl);
  padding: 48px 40px;
  box-shadow: var(--shadow-lg);
  animation: cardEnter 0.6s ease-out;
}

@keyframes cardEnter {
  from {
    opacity: 0;
    transform: translateY(30px) scale(0.95);
  }
  to {
    opacity: 1;
    transform: translateY(0) scale(1);
  }
}

/* Login Header */
.login-header {
  text-align: center;
  margin-bottom: 40px;
}

.logo-wrapper {
  display: flex;
  justify-content: center;
  margin-bottom: 24px;
}

.logo-icon {
  width: 72px;
  height: 72px;
  background: linear-gradient(135deg, var(--primary) 0%, var(--primary-dark) 100%);
  border-radius: var(--radius-lg);
  display: flex;
  align-items: center;
  justify-content: center;
  color: white;
  box-shadow: 0 8px 32px var(--primary-glow);
  animation: logoFloat 4s ease-in-out infinite;
}

@keyframes logoFloat {
  0%, 100% { transform: translateY(0); }
  50% { transform: translateY(-8px); }
}

.logo-icon svg {
  width: 40px;
  height: 40px;
}

.login-title {
  font-size: 28px;
  font-weight: 800;
  color: var(--text-primary);
  margin: 0 0 8px 0;
  letter-spacing: -1px;
}

.login-subtitle {
  font-size: 14px;
  color: var(--text-muted);
  margin: 0;
}

/* Login Form */
.login-form {
  display: flex;
  flex-direction: column;
  gap: 20px;
}

.input-wrapper {
  position: relative;
  width: 100%;
}

.input-icon {
  position: absolute;
  left: 16px;
  top: 50%;
  transform: translateY(-50%);
  width: 20px;
  height: 20px;
  color: var(--text-muted);
  z-index: 10;
  pointer-events: none;
  transition: color var(--transition-fast);
}

.input-wrapper:focus-within .input-icon {
  color: var(--primary);
}

.login-form :deep(.el-input__wrapper) {
  padding-left: 48px;
  height: 52px;
  background: var(--bg-input);
  border-radius: var(--radius-md);
  box-shadow: 0 0 0 1px var(--border-default) inset;
  transition: all var(--transition-fast);
}

.login-form :deep(.el-input__wrapper:hover) {
  box-shadow: 0 0 0 1px var(--text-muted) inset;
}

.login-form :deep(.el-input__wrapper.is-focus) {
  box-shadow: 0 0 0 2px var(--primary) inset;
  background: var(--bg-elevated);
}

.login-form :deep(.el-input__inner) {
  font-size: 15px;
  font-weight: 500;
}

.login-form :deep(.el-form-item) {
  margin-bottom: 0;
}

.login-form :deep(.el-form-item__error) {
  padding-top: 6px;
}

.submit-btn {
  width: 100%;
  height: 52px;
  font-size: 16px;
  font-weight: 600;
  border-radius: var(--radius-md);
  background: linear-gradient(135deg, var(--primary) 0%, var(--primary-dark) 100%);
  border: none;
  box-shadow: 0 4px 16px var(--primary-glow);
  transition: all var(--transition-fast);
  margin-top: 8px;
}

.submit-btn:hover {
  transform: translateY(-2px);
  box-shadow: 0 8px 24px var(--primary-glow);
}

.submit-btn:active {
  transform: translateY(0);
}

/* Login Footer */
.login-footer {
  margin-top: 32px;
  display: flex;
  justify-content: center;
}

.security-badge {
  display: flex;
  align-items: center;
  gap: 6px;
  font-size: 12px;
  color: var(--text-muted);
}

.security-badge svg {
  width: 14px;
  height: 14px;
}

/* Version Tag */
.version-tag {
  margin-top: 24px;
  font-size: 12px;
  color: var(--text-disabled);
  font-weight: 500;
}
</style>
