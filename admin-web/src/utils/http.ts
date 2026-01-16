import axios, { AxiosError } from 'axios'
import { ElMessage } from 'element-plus'

function getToken() {
  return localStorage.getItem('admin_token') || ''
}

const SKIP_AUTH_HEADER = 'X-Skip-Auth'

export const api = axios.create({
  baseURL: '/api',
  timeout: 15000
})

api.interceptors.request.use((config) => {
  const headers: any = (config.headers ??= {})
  const skipAuth =
    headers[SKIP_AUTH_HEADER] === '1' ||
    headers[SKIP_AUTH_HEADER.toLowerCase()] === '1' ||
    headers.skipAuth === true

  if (skipAuth) {
    // Ensure auth header is NOT attached for this request.
    try {
      delete headers.Authorization
      delete headers.authorization
      delete headers[SKIP_AUTH_HEADER]
      delete headers[SKIP_AUTH_HEADER.toLowerCase()]
      delete headers.skipAuth
    } catch {
      // noop
    }
    headers.Authorization = undefined
    headers.authorization = undefined
  } else {
    const token = getToken()
    if (token) headers.Authorization = `Bearer ${token}`
  }
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
    if (!withAuth) headers[SKIP_AUTH_HEADER] = '1'
    const { data } = await api.post(path, body ?? {}, { headers })
    return data.data as T
  },
  async del<T>(path: string): Promise<T> {
    const { data } = await api.delete(path)
    return data.data as T
  }
}


