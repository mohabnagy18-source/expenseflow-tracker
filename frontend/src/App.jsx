import { useCallback, useEffect, useMemo, useState } from 'react'
import { api, MONTHS } from './api.js'

const WEEKDAYS = ['Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat', 'Sun']
const todayLocal = () => {
  const d = new Date()
  return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`
}

export default function App() {
  const [user, setUser] = useState(() => JSON.parse(localStorage.getItem('ef_user') || 'null'))
  const login = (u) => { localStorage.setItem('ef_user', JSON.stringify(u)); setUser(u) }
  const logout = () => { localStorage.removeItem('ef_user'); setUser(null) }
  if (!user) return <Auth onDone={login} />
  return <Dashboard user={user} logout={logout} />
}

function Auth({ onDone }) {
  const [mode, setMode] = useState('login')
  const isLogin = mode === 'login'
  const [form, setForm] = useState({ fullName: '', email: '', password: '', currency: 'USD' })
  const [err, setErr] = useState('')
  const [busy, setBusy] = useState(false)
  const submit = async (e) => {
    e.preventDefault(); setBusy(true); setErr('')
    try {
      const u = await api(isLogin ? '/auth/login' : '/auth/register', { method: 'POST', body: isLogin ? { email: form.email, password: form.password } : form })
      onDone(u)
    } catch (e2) { setErr(e2.message) } finally { setBusy(false) }
  }
  return (
    <div className="auth-page">
      <div className="auth-hero">
        <h1><span className="logo">~</span> ExpenseFlow</h1>
        <p>Log what you spend in seconds, and see clear weekly and monthly summaries.</p>
        <ul><li>One-tap logging with your own items</li><li>Weekly and monthly summaries</li><li>Day-to-day spending comparison</li></ul>
      </div>
      <form className="card auth" onSubmit={submit}>
        <h2>{isLogin ? 'Welcome back' : 'Create your account'}</h2>
        <p className="muted">{isLogin ? 'Demo account: demo@expenseflow.com / Demo123!' : 'Start tracking in under a minute.'}</p>
        {err && <div className="alert alert-err">{err}</div>}
        {!isLogin && <label>Full name<input required value={form.fullName} onChange={(e) => setForm({ ...form, fullName: e.target.value })} placeholder="Jane Doe" /></label>}
        <label>Email<input required type="email" value={form.email} onChange={(e) => setForm({ ...form, email: e.target.value })} placeholder="you@example.com" /></label>
        <label>Password<input required type="password" minLength="6" value={form.password} onChange={(e) => setForm({ ...form, password: e.target.value })} placeholder="********" /></label>
        {!isLogin && <label>Currency<select value={form.currency} onChange={(e) => setForm({ ...form, currency: e.target.value })}>
          {['USD','EUR','EGP','GBP','SAR','AED'].map((c) => <option key={c}>{c}</option>)}</select></label>}
        <button className="btn btn-primary btn-block" disabled={busy}>{busy ? 'Please wait...' : isLogin ? 'Log in' : 'Sign up'}</button>
        <button type="button" className="link center" onClick={() => setMode(isLogin ? 'register' : 'login')}>{isLogin ? 'No account yet? Sign up' : 'Already registered? Log in'}</button>
      </form>
    </div>
  )
}

function Dashboard({ user, logout }) {
  const now = new Date()
  const [ym, setYm] = useState({ y: now.getFullYear(), m: now.getMonth() + 1 })
  const [weekAnchor, setWeekAnchor] = useState(todayLocal())
  const [report, setReport] = useState(null)
  const [week, setWeek] = useState(null)
  const [expenses, setExpenses] = useState(null)
  const [categories, setCategories] = useState([])
  const [quickItems, setQuickItems] = useState([])
  const [err, setErr] = useState('')
  const [notice, setNotice] = useState('')
  const [tab, setTab] = useState('expenses')
  const [form, setForm] = useState({ categoryId: '', amount: '', date: todayLocal(), note: '' })
  const [catForm, setCatForm] = useState({ name: '', icon: '', color: '#4f6ef7' })
  const [itemForm, setItemForm] = useState({ name: '', categoryId: '', amount: '' })
  const money = useCallback((n) => `${user.currency} ${Number(n).toFixed(2)}`, [user.currency])

  const load = useCallback(() => {
    api(`/reports/monthly?year=${ym.y}&month=${ym.m}`, { token: user.token }).then(setReport).catch((e) => setErr(e.message))
    api(`/reports/weekly?date=${weekAnchor}`, { token: user.token }).then(setWeek).catch(() => {})
    api(`/expenses?year=${ym.y}&month=${ym.m}`, { token: user.token }).then(setExpenses).catch(() => {})
    api('/categories', { token: user.token }).then((c) => {
      setCategories(c)
      setForm((f) => f.categoryId ? f : { ...f, categoryId: c[0]?.id || '' })
      setItemForm((f) => f.categoryId ? f : { ...f, categoryId: c[0]?.id || '' })
    }).catch(() => {})
    api('/quick-items', { token: user.token }).then(setQuickItems).catch(() => {})
  }, [ym, weekAnchor, user.token])
  useEffect(() => { load() }, [load])

  const flash = (msg) => { setNotice(msg); setTimeout(() => setNotice(''), 2500) }
  const shift = (d) => setYm(({ y, m }) => { const t = new Date(y, m - 1 + d, 1); return { y: t.getFullYear(), m: t.getMonth() + 1 } })
  const shiftWeek = (d) => setWeekAnchor((a) => { const t = new Date(`${a}T12:00:00`); t.setDate(t.getDate() + d * 7); return t.toISOString().slice(0, 10) })

  const addExpense = async (e) => {
    e.preventDefault()
    try {
      await api('/expenses', { method: 'POST', token: user.token, body: { categoryId: form.categoryId, amount: +form.amount, date: form.date, note: form.note || null } })
      setForm({ ...form, amount: '', note: '' }); load()
    } catch (e2) { setErr(e2.message) }
  }
  const delExpense = async (id) => { await api(`/expenses/${id}`, { method: 'DELETE', token: user.token }).catch(() => {}); load() }
  const logQuick = async (item) => {
    try {
      await api(`/quick-items/${item.id}/log`, { method: 'POST', token: user.token, body: { date: todayLocal() } })
      flash(`Logged: ${item.name} (${money(item.amount)})`); load()
    } catch (e2) { setErr(e2.message) }
  }
  const addCategory = async (e) => {
    e.preventDefault()
    try { await api('/categories', { method: 'POST', token: user.token, body: catForm }); setCatForm({ name: '', icon: '', color: '#4f6ef7' }); load() } catch (e2) { setErr(e2.message) }
  }
  const delCategory = async (id) => { await api(`/categories/${id}`, { method: 'DELETE', token: user.token }).catch(() => {}); load() }
  const addItem = async (e) => {
    e.preventDefault()
    try {
      await api('/quick-items', { method: 'POST', token: user.token, body: { name: itemForm.name, categoryId: itemForm.categoryId, amount: +itemForm.amount, note: null } })
      setItemForm({ ...itemForm, name: '', amount: '' }); load()
    } catch (e2) { setErr(e2.message) }
  }
  const delItem = async (id) => { await api(`/quick-items/${id}`, { method: 'DELETE', token: user.token }).catch(() => {}); load() }

  const maxDay = useMemo(() => Math.max(1, ...(report?.byDay || []).map((d) => d.total)), [report])
  const maxWeekDay = useMemo(() => Math.max(1, ...(week?.byDay || []).map((d) => d.total)), [week])
  const changeUp = (report?.changePercent ?? 0) > 0
  const weekChangeUp = (week?.changePercent ?? 0) > 0
  const dayChangeUp = (week?.dayChangePercent ?? 0) > 0
  return (
    <div>
      <nav className="nav">
        <div className="brand"><span className="logo">~</span> ExpenseFlow</div>
        <div className="nav-links">
          <div className="user-chip"><span className="avatar">{user.fullName[0]}</span><span className="hide-sm">{user.fullName}</span>
          <button className="link" onClick={logout}>Log out</button></div>
        </div>
      </nav>
      <main className="container">
        {err && <div className="alert alert-err">{err}</div>}
        {notice && <div className="alert alert-ok">{notice}</div>}
        <div className="tabs">
          <button className={tab === 'expenses' ? 'tab tab-active' : 'tab'} onClick={() => setTab('expenses')}>Expenses</button>
          <button className={tab === 'week' ? 'tab tab-active' : 'tab'} onClick={() => setTab('week')}>Week</button>
          <button className={tab === 'month' ? 'tab tab-active' : 'tab'} onClick={() => setTab('month')}>Month</button>
          <button className={tab === 'cats' ? 'tab tab-active' : 'tab'} onClick={() => setTab('cats')}>Categories &amp; items</button>
        </div>

        {tab === 'expenses' && (<>
          <div className="card quick-card">
            <h3>Quick log</h3>
            <p className="muted">One tap logs it for today. Add your own items under Categories &amp; items.</p>
            {quickItems.length === 0 ? <p className="muted">No items yet - create your list of usual expenses first, then log with one tap.</p> : (
              <div className="chips">
                {quickItems.map((q) => (
                  <button key={q.id} className="chip" onClick={() => logQuick(q)}>
                    <span className="chip-dot" style={{ background: q.categoryColor }} />
                    {q.name} <span className="chip-amt">{money(q.amount)}</span>
                  </button>))}
              </div>)}
          </div>
          <form className="card form-row" onSubmit={addExpense}>
            <select value={form.categoryId} onChange={(e) => setForm({ ...form, categoryId: e.target.value })}>
              {categories.map((c) => <option key={c.id} value={c.id}>{c.icon} {c.name}</option>)}
            </select>
            <input required type="number" min="0.01" step="0.01" placeholder="Amount" value={form.amount} onChange={(e) => setForm({ ...form, amount: e.target.value })} />
            <input required type="date" value={form.date} onChange={(e) => setForm({ ...form, date: e.target.value })} />
            <input placeholder="What was it? (optional)" value={form.note} onChange={(e) => setForm({ ...form, note: e.target.value })} />
            <button className="btn btn-primary">Add</button>
          </form>
          <div className="month-bar">
            <button className="btn btn-ghost btn-sm" onClick={() => shift(-1)}>&larr;</button>
            <h2>{MONTHS[ym.m - 1]} {ym.y}</h2>
            <button className="btn btn-ghost btn-sm" onClick={() => shift(1)}>&rarr;</button>
          </div>
          <div className="list">
            {(expenses || []).length === 0 && <div className="card muted">No expenses this month.</div>}
            {(expenses || []).map((x) => (
              <div key={x.id} className="card row">
                <div className="row-main-flex">
                  <span className="cat-icon" style={{ background: x.categoryColor }}>{(x.categoryIcon || x.categoryName[0]).slice(0, 2)}</span>
                  <div className="row-main"><strong>{x.categoryName}</strong><span className="muted">{x.date}{x.note ? ` - ${x.note}` : ''}</span></div>
                </div>
                <div className="row-side"><strong>{money(x.amount)}</strong>
                  <button className="btn btn-ghost btn-sm" onClick={() => delExpense(x.id)}>Delete</button></div>
              </div>))}
          </div>
        </>)}

        {tab === 'week' && (<>
          <div className="month-bar">
            <button className="btn btn-ghost btn-sm" onClick={() => shiftWeek(-1)}>&larr;</button>
            <h2>{week ? `${week.weekStart} - ${week.weekEnd}` : 'This week'}</h2>
            <button className="btn btn-ghost btn-sm" onClick={() => shiftWeek(1)}>&rarr;</button>
          </div>
          {!week ? <div className="card">Loading week...</div> : (<>
            <div className="stats">
              <div className="card stat"><span className="stat-num">{money(week.total)}</span><span className="muted">Spent this week</span></div>
              <div className="card stat"><span className={`stat-num ${weekChangeUp ? 'neg' : 'pos'}`}>{weekChangeUp ? '+' : ''}{week.changePercent}%</span><span className="muted">vs {money(week.previousWeekTotal)} last week</span></div>
              <div className="card stat"><span className="stat-num">{money(week.todayTotal)}</span><span className="muted">Spent today</span></div>
              <div className="card stat"><span className={`stat-num ${dayChangeUp ? 'neg' : 'pos'}`}>{week.yesterdayTotal === 0 && week.todayTotal === 0 ? '0%' : `${dayChangeUp ? '+' : ''}${week.dayChangePercent}%`}</span><span className="muted">vs yesterday ({money(week.yesterdayTotal)}){!dayChangeUp && week.todayTotal > 0 ? ' - more frugal' : ''}</span></div>
            </div>
            <div className="grid">
              <div className="card">
                <h3>Spending by day</h3>
                <div className="chart chart-week">
                  {week.byDay.map((d, i) => (
                    <div key={d.date} className="bar-col" title={`${d.date}: ${money(d.total)}`}>
                      <div className={`bar ${d.date === todayLocal() ? 'bar-today' : ''}`} style={{ height: `${(d.total / maxWeekDay) * 100}%` }} />
                      <span className="bar-label">{WEEKDAYS[i]}</span>
                    </div>))}
                </div>
              </div>
              <div className="card">
                <h3>By category this week</h3>
                {week.byCategory.length === 0 && <p className="muted">Nothing spent this week.</p>}
                {week.byCategory.map((c) => (
                  <div key={c.categoryId} className="cat-row">
                    <span className="cat-icon" style={{ background: c.color }}>{(c.icon || c.name[0]).slice(0, 2)}</span>
                    <div className="cat-main">
                      <div className="cat-head"><strong>{c.name}</strong><span>{money(c.total)} ({c.percent}%)</span></div>
                      <div className="track"><div className="fill" style={{ width: `${c.percent}%`, background: c.color }} /></div>
                    </div>
                  </div>))}
              </div>
            </div>
          </>)}
        </>)}

        {tab === 'month' && (<>
          <div className="month-bar">
            <button className="btn btn-ghost btn-sm" onClick={() => shift(-1)}>&larr;</button>
            <h2>{MONTHS[ym.m - 1]} {ym.y}</h2>
            <button className="btn btn-ghost btn-sm" onClick={() => shift(1)}>&rarr;</button>
          </div>
          {!report ? <div className="card">Loading report...</div> : (<>
            <div className="stats">
              <div className="card stat"><span className="stat-num">{money(report.total)}</span><span className="muted">Spent this month</span></div>
              <div className="card stat"><span className="stat-num">{report.expenseCount}</span><span className="muted">Expenses</span></div>
              <div className="card stat"><span className="stat-num">{money(report.dailyAverage)}</span><span className="muted">Daily average</span></div>
              <div className="card stat"><span className={`stat-num ${changeUp ? 'neg' : 'pos'}`}>{changeUp ? '+' : ''}{report.changePercent}%</span><span className="muted">vs {money(report.previousMonthTotal)} last month</span></div>
            </div>
            <div className="grid">
              <div className="card">
                <h3>Spending by day</h3>
                <div className="chart">
                  {report.byDay.map((d) => (
                    <div key={d.date} className="bar-col" title={`${d.date}: ${money(d.total)}`}>
                      <div className="bar" style={{ height: `${(d.total / maxDay) * 100}%` }} />
                      <span className="bar-label">{d.date.slice(8)}</span>
                    </div>))}
                </div>
              </div>
              <div className="card">
                <h3>By category</h3>
                {report.byCategory.length === 0 && <p className="muted">Nothing spent yet this month.</p>}
                {report.byCategory.map((c) => (
                  <div key={c.categoryId} className="cat-row">
                    <span className="cat-icon" style={{ background: c.color }}>{(c.icon || c.name[0]).slice(0, 2)}</span>
                    <div className="cat-main">
                      <div className="cat-head"><strong>{c.name}</strong><span>{money(c.total)} ({c.percent}%)</span></div>
                      <div className="track"><div className="fill" style={{ width: `${c.percent}%`, background: c.color }} /></div>
                    </div>
                  </div>))}
              </div>
            </div>
          </>)}
        </>)}

        {tab === 'cats' && (<>
          <div className="card quick-card">
            <h3>My quick items</h3>
            <p className="muted">Your usual expenses with their usual amounts - log them for today with one tap.</p>
            <form className="form-row form-items" onSubmit={addItem}>
              <input required placeholder="Item name (e.g. Morning coffee)" value={itemForm.name} onChange={(e) => setItemForm({ ...itemForm, name: e.target.value })} />
              <select value={itemForm.categoryId} onChange={(e) => setItemForm({ ...itemForm, categoryId: e.target.value })}>
                {categories.map((c) => <option key={c.id} value={c.id}>{c.icon} {c.name}</option>)}
              </select>
              <input required type="number" min="0.01" step="0.01" placeholder="Usual amount" value={itemForm.amount} onChange={(e) => setItemForm({ ...itemForm, amount: e.target.value })} />
              <button className="btn btn-primary">Add item</button>
            </form>
            <div className="list">{quickItems.map((q) => (
              <div key={q.id} className="card row">
                <div className="row-main-flex">
                  <span className="cat-icon" style={{ background: q.categoryColor }}>{(q.categoryIcon || q.categoryName[0]).slice(0, 2)}</span>
                  <div className="row-main"><strong>{q.name}</strong><span className="muted">{q.categoryName} - {money(q.amount)}</span></div>
                </div>
                <div className="row-side">
                  <button className="btn btn-primary btn-sm" onClick={() => logQuick(q)}>Log today</button>
                  <button className="btn btn-ghost btn-sm" onClick={() => delItem(q.id)}>Delete</button>
                </div>
              </div>))}
            </div>
          </div>
          <form className="card form-row form-cats" onSubmit={addCategory}>
            <input required placeholder="New category name" value={catForm.name} onChange={(e) => setCatForm({ ...catForm, name: e.target.value })} />
            <input placeholder="Icon (e.g. G)" maxLength="2" value={catForm.icon} onChange={(e) => setCatForm({ ...catForm, icon: e.target.value })} />
            <input type="color" value={catForm.color} onChange={(e) => setCatForm({ ...catForm, color: e.target.value })} />
            <button className="btn btn-primary">Add category</button>
          </form>
          <div className="list">{categories.map((c) => (
            <div key={c.id} className="card row">
              <div className="row-main-flex">
                <span className="cat-icon" style={{ background: c.color }}>{(c.icon || c.name[0]).slice(0, 2)}</span>
                <strong>{c.name}</strong>{c.isDefault && <span className="badge badge-blue">default</span>}
              </div>
              {!c.isDefault && <button className="btn btn-ghost btn-sm" onClick={() => delCategory(c.id)}>Delete</button>}
            </div>))}
          </div>
        </>)}
      </main>
      <footer className="footer">ExpenseFlow - quick daily logging with weekly and monthly summaries</footer>
    </div>
  )
}
