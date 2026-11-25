const BASE = '/api/expense'
export async function api(path, { method = 'GET', body, token } = {}) {
  const res = await fetch(BASE + path, {
    method,
    headers: { 'Content-Type': 'application/json', ...(token ? { Authorization: `Bearer ${token}` } : {}) },
    body: body ? JSON.stringify(body) : undefined,
  })
  if (res.status === 204) return null
  const data = await res.json().catch(() => ({}))
  if (!res.ok) throw new Error(data.message || data.title || `Request failed (${res.status})`)
  return data
}
export const MONTHS = ['January','February','March','April','May','June','July','August','September','October','November','December']
