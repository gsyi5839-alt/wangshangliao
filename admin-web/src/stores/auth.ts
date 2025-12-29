import { defineStore } from 'pinia'
import { http } from '../utils/http'

type AdminMe = { id: string; username: string }

export const useAuthStore = defineStore('auth', {
  state: () => ({
    token: localStorage.getItem('admin_token') || '',
    me: null as AdminMe | null,
    meLoaded: false
  }),
  actions: {
    setToken(token: string) {
      this.token = token
      localStorage.setItem('admin_token', token)
    },
    logout() {
      this.token = ''
      this.me = null
      this.meLoaded = false
      localStorage.removeItem('admin_token')
    },
    async login(username: string, password: string) {
      const resp = await http.post<{ token: string }>('/auth/login', { username, password }, false)
      this.setToken(resp.token)
      await this.fetchMe()
    },
    async fetchMe() {
      const me = await http.get<AdminMe>('/admins/me')
      this.me = me
      this.meLoaded = true
    }
  }
})


