import axios, { AxiosError } from 'axios'
import { ElMessage } from 'element-plus'

function getToken() {
  return localStorage.getItem('admin_token') || ''
}

export const api = axios.create({
  baseURL: '/api',
  timeout: 15000
})

api.interceptors.request.use((config) => {
  const token = getToken()
  if (token) config.headers.Authorization = `Bearer ${token}`
  return config
})

api.interceptors.response.use(
  (resp) => resp,
  (err: AxiosError<any>) => {
    const msg =
      (err.response?.data && (err.response.data.error?.message || err.response.data.message)) ||
      err.message ||
      'Request failed'
    ElMessage.error(msg)
    return Promise.reject(err)
  }
)

export const http = {
  async get<T>(path: string): Promise<T> {
    const { data } = await api.get(path)
    return data.data as T
  },
  async post<T>(path: string, body?: any, withAuth = true): Promise<T> {
    const headers: any = {}
    if (!withAuth) delete headers.Authorization
    const { data } = await api.post(path, body ?? {}, { headers })
    return data.data as T
  },
  async del<T>(path: string): Promise<T> {
    const { data } = await api.delete(path)
    return data.data as T
  }
}


