import { useEffect, useMemo, useState } from 'react'
import './App.css'

interface Project {
  id: number
  name: string
  description: string
  startDate: string
  endDate: string
  status: string
  priority: string
  completionPercentage: number
  owner: string
  createdAt: string
  updatedAt: string
  task: unknown[]
  meeting: unknown[]
}

interface ProjectSearchResponse {
  success: boolean
  statusCode: number
  message: string
  data: {
    items: Project[]
    totalCount: number
    pageIndex: number
    pageSize: number
    totalPages: number
    hasPreviousPage: boolean
    hasNextPage: boolean
  }
  timestamp: string
}

const dateTimeFormatter = new Intl.DateTimeFormat('zh-TW', {
  year: 'numeric',
  month: '2-digit',
  day: '2-digit',
  hour: '2-digit',
  minute: '2-digit',
})

const formatDate = (isoString: string) => {
  try {
    return dateTimeFormatter.format(new Date(isoString))
  } catch {
    return 'N/A'
  }
}

function App() {
  const [projects, setProjects] = useState<Project[]>([])
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    const controller = new AbortController()

    const loadProjects = async () => {
      try {
        setIsLoading(true)
        setError(null)

        const response = await fetch('http://20.29.58.245/api/Project/search', {
          method: 'POST',
          headers: {
            accept: 'text/plain',
            'Content-Type': 'application/json',
          },
          body: JSON.stringify({
            pageIndex: 1,
            pageSize: 5,
          }),
          signal: controller.signal,
        })

        if (!response.ok) {
          throw new Error(`Request failed with status ${response.status}`)
        }

        const payload: ProjectSearchResponse = await response.json()

        if (!payload?.data?.items) {
          throw new Error('Unexpected response shape from API')
        }

        setProjects(payload.data.items)
      } catch (err) {
        if (err instanceof DOMException && err.name === 'AbortError') {
          return
        }

        setError(err instanceof Error ? err.message : 'Unable to load projects')
      } finally {
        setIsLoading(false)
      }
    }

    loadProjects()

    return () => controller.abort()
  }, [])

  const tableRows = useMemo(() => {
    return projects.map((project) => ({
      ...project,
      startDateFormatted: project.startDate ? formatDate(project.startDate) : 'N/A',
      endDateFormatted: project.endDate ? formatDate(project.endDate) : 'N/A',
      lastUpdated: project.updatedAt ? formatDate(project.updatedAt) : 'N/A',
    }))
  }, [projects])

  return (
    <div className="app">
      <header className="app__header">
        <h1 className="app__title">專案列表</h1>
        <p className="app__subtitle">
          以下為呼叫 <code>POST /api/Project/search</code> 取得的前 5 筆專案資料。
        </p>
      </header>

      {isLoading && <p className="app__status">資料載入中...</p>}

      {error && !isLoading && (
        <p className="app__status app__status--error">{error}</p>
      )}

      {!isLoading && !error && (
        <div className="app__table-wrapper">
          <table className="app__table">
            <thead>
              <tr>
                <th>ID</th>
                <th>名稱</th>
                <th>狀態</th>
                <th>優先度</th>
                <th>開始日期</th>
                <th>結束日期</th>
                <th>更新時間</th>
              </tr>
            </thead>
            <tbody>
              {tableRows.map((project) => (
                <tr key={project.id}>
                  <td>{project.id}</td>
                  <td>
                    <span className="app__project-name">{project.name}</span>
                    {project.description && (
                      <p className="app__project-description">{project.description}</p>
                    )}
                  </td>
                  <td>{project.status}</td>
                  <td>{project.priority}</td>
                  <td>{project.startDateFormatted}</td>
                  <td>{project.endDateFormatted}</td>
                  <td>{project.lastUpdated}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  )
}

export default App
