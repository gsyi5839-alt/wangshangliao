import { createRouter, createWebHashHistory } from 'vue-router'
import { useAuthStore } from '../stores/auth'

import LoginView from '../views/LoginView.vue'
import AdminLayout from '../layouts/AdminLayout.vue'
import CardsView from '../views/CardsView.vue'
import AnnouncementsView from '../views/AnnouncementsView.vue'
import VersionsView from '../views/VersionsView.vue'
import SettingsView from '../views/SettingsView.vue'
import AgentsView from '../views/AgentsView.vue'
import UsersView from '../views/UsersView.vue'

export const router = createRouter({
  history: createWebHashHistory('/admin/'),
  routes: [
    { path: '/login', component: LoginView },
    {
      path: '/',
      component: AdminLayout,
      children: [
        { path: '', redirect: '/cards' },
        { path: 'cards', component: CardsView },
        { path: 'announcements', component: AnnouncementsView },
        { path: 'versions', component: VersionsView },
        { path: 'settings', component: SettingsView },
        { path: 'agents', component: AgentsView },
        { path: 'users', component: UsersView }
      ]
    }
  ]
})

router.beforeEach(async (to) => {
  const auth = useAuthStore()
  if (to.path === '/login') return true
  if (!auth.token) return '/login'
  if (!auth.meLoaded) {
    try {
      await auth.fetchMe()
    } catch {
      auth.logout()
      return '/login'
    }
  }
  return true
})


