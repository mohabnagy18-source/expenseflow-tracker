# ExpenseFlow - Personal Expense Tracker

Track spending, organize it with your own categories, and get a clear monthly report with charts: daily spending, category breakdown, and month-over-month comparison.

**Live app:** https://expenseflow-mn.vercel.app
**API:** http://clinicease-api.runasp.net/api/expense (hosted together with the ClinicEase API on the shared free hosting plan)

Demo account: `demo@expenseflow.com` / `Demo123!` (comes with seeded expenses)

## Features
- Email + password accounts with JWT auth, per-user currency
- Expenses CRUD with category, date and notes
- Custom categories with icon and color (default set seeded on sign-up)
- Monthly report: total, expense count, daily average, percent change vs previous month, daily bar chart, category share bars
- Month navigation and filters
- Responsive UI for mobile and desktop

## Tech stack
- **Backend:** ASP.NET Core 8, Clean Architecture (Domain / Application / Infrastructure), EF Core 8 + SQL Server, JWT authentication
- **Frontend:** React 18 + Vite, custom CSS, dependency-free charts
- **Hosting:** API + SQL Server on MonsterASP.NET (module hosted inside the ClinicEase API site), frontend on Vercel (Vercel rewrites proxy `/api/*` to the API)

## Repository layout
- `backend/` - ExpenseFlow.Domain / ExpenseFlow.Application / ExpenseFlow.Infrastructure class libraries. The deployed API host (controllers, DI wiring, Program.cs) lives in the clinicease-booking repo under `backend/src/ClinicEase.Api`, which references these libraries; clone both repos side by side to build the combined solution.
- `frontend/` - React + Vite app (`vercel.json` contains the `/api` rewrite to the live API)

## Run locally
```bash
cd frontend && npm install && npm run dev   # UI on http://localhost:5173
```
For the API, see the clinicease-booking repo (combined host). EF Core migrations apply automatically at startup and seed a demo account with sample expenses.
