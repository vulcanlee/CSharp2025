import { useState } from 'react'
import reactLogo from './assets/react.svg'
import viteLogo from '/vite.svg'
import './App.css'

interface WeatherForecast {
  date: string
  temperatureC: number
  summary: string
  temperatureF?: number
}

function App() {
  const [count, setCount] = useState(0)

    const [location, setLocation] = useState<string>('Kaohsiung')
    const [startDate, setStartDate] = useState<string>('2025-09-01') // yyyy-MM-dd（<input type="date">）
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [forecasts, setForecasts] = useState<WeatherForecast[]>([])

  // 請依實際後端埠號調整，以下取自 WebApiDemo.http
  const apiBase = 'https://localhost:7074'

  const fetchForecasts = async () => {
    setLoading(true)
    setError(null)
    setForecasts([])

    try {
      if (!location.trim()) {
        throw new Error('請輸入地點')
      }
      if (!startDate) {
        throw new Error('請選擇日期')
      }

      const resp = await fetch(`${apiBase}/weatherforecast/GetWeatherForecastWithHeaders`, {
        method: 'GET',
        headers: {
          // 後端 Controller 使用 [FromHeader(Name="Location")] 與 [FromHeader(Name="StartDate")]
          'Location': location.trim(),
          // DateOnly 綁定建議用 ISO 格式 yyyy-MM-dd，與 <input type="date"> 一致
          'StartDate': startDate,
          'Accept': 'text/plain'
        }
      })

      if (!resp.ok) {
        throw new Error(`請求失敗：${resp.status} ${resp.statusText}`)
      }

      // 伺服器把預報結果放在自訂回應標頭 X-Weather-Forecasts
      const headerVal = resp.headers.get('X-Weather-Forecasts')
      if (!headerVal) {
        throw new Error('找不到回應標頭 X-Weather-Forecasts。請確認伺服器已設定 CORS Expose-Headers。')
      }

      const raw = JSON.parse(headerVal) as any[]

      // 後端使用 System.Text.Json 預設為 PascalCase，這裡做正規化
      const normalized: WeatherForecast[] = raw.map((x: any) => ({
        date: x.Date ?? x.date,
        temperatureC: x.TemperatureC ?? x.temperatureC,
        summary: x.Summary ?? x.summary,
        temperatureF: x.TemperatureF ?? x.temperatureF
      }))

      setForecasts(normalized)
    } catch (e: any) {
      setError(e?.message ?? String(e))
    } finally {
      setLoading(false)
    }
  }

  return (
    <>
      <div>
        <a href="https://vite.dev" target="_blank">
          <img src={viteLogo} className="logo" alt="Vite logo" />
        </a>
        <a href="https://react.dev" target="_blank">
          <img src={reactLogo} className="logo react" alt="React logo" />
        </a>
      </div>

      <h1>Vite + React</h1>

      <div className="card" style={{ display: 'grid', gap: 12 }}>
        <div>
          <button onClick={() => setCount((count) => count + 1)}>
            count is {count}
          </button>
        </div>

        <hr />

        <h2>天氣查詢（Header 傳遞）</h2>

        <label style={{ display: 'grid', gap: 6 }}>
          <span>地點</span>
          <input
            type="text"
            placeholder="例如：Kaohsiung"
            value={location}
            onChange={(e) => setLocation(e.target.value)}
          />
        </label>

        <label style={{ display: 'grid', gap: 6 }}>
          <span>開始日期</span>
          <input
            type="date"
            value={startDate}
            onChange={(e) => setStartDate(e.target.value)}
          />
        </label>

        <div>
          <button onClick={fetchForecasts} disabled={loading}>
            {loading ? '查詢中…' : '送出並取得預報'}
          </button>
        </div>

        {error && (
          <div style={{ color: 'crimson' }}>
            {error}
          </div>
        )}

        {forecasts.length > 0 && (
          <div>
            <h3>預報結果</h3>
            <ul>
              {forecasts.map((f) => (
                <li key={`${f.date}-${f.summary}`}>
                  <strong>{f.date}</strong> — {f.summary}，{f.temperatureC} °C{typeof f.temperatureF === 'number' ? `（${f.temperatureF} °F）` : ''}
                </li>
              ))}
            </ul>
          </div>
        )}
      </div>

      <p className="read-the-docs">
        Click on the Vite and React logos to learn more
      </p>
    </>
  )
}

export default App
